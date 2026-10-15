"""Verify the optimized prefab against the byte backup produced by Unity.

Run from the repository root: py Tools/Basement/Optimization/verify_basement.py
Optional screenshot comparison requires Pillow. This does not modify assets.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
import sys
sys.path.insert(0, str(ROOT / 'Tools/ProjectOrganization'))
from resolve_paths import resolve
OUTPUT = ROOT / '.utmp/BasementOptimization'
PREFAB = resolve(ROOT / 'Assets/prefabs/Scenario/Basement.prefab')


def blocks(text):
    return {m[2]: (m[1], m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def mesh_reference(block):
    return re.search(r'(?m)^  m_Mesh:.*guid: ([a-f0-9]{32})', block)[1]


def main():
    before = blocks((OUTPUT / 'Basement.prefab.before').read_text(encoding='utf-8-sig'))
    after = blocks(PREFAB.read_text(encoding='utf-8-sig'))
    assert before.keys() == after.keys(), 'Prefab fileIDs changed'
    cached_index = OUTPUT / 'guid-index.json'
    if cached_index.exists():
        index = {guid: resolve(ROOT / path) for guid, path in json.loads(cached_index.read_text(encoding='utf-8-sig')).items()}
    else:
        index = {}
        for meta in (ROOT / 'Assets').rglob('*.meta'):
            match = re.search(r'(?m)^guid: ([a-f0-9]{32})', meta.read_text(encoding='utf-8-sig'))
            if match:
                index[match[1]] = Path(str(meta)[:-5])
    changed = 0
    for fid, (kind, old) in before.items():
        new = after[fid][1]
        if old == new:
            continue
        assert kind == after[fid][0] == '33', f'Unexpected component change: {fid}'
        assert re.sub(r'(?m)^  m_Mesh:.*$', '', old) == re.sub(r'(?m)^  m_Mesh:.*$', '', new)
        a, b = index[mesh_reference(old)], index[mesh_reference(new)]
        normalize = lambda p: re.sub(r'(?m)^  m_Name:[^\r\n]*', '', p.read_text(encoding='utf-8-sig'))
        assert normalize(a) == normalize(b), f'Mesh data differ: {a}, {b}'
        changed += 1
    print(f'PASS: {len(before)} fileIDs preserved; {changed} equivalent MeshFilter references changed.')
    try:
        from PIL import Image, ImageChops, ImageStat
    except ImportError:
        print('Screenshot comparison skipped: install Pillow to enable it.')
        return
    results = {}
    for source in sorted((OUTPUT / 'before').glob('*.png')):
        destination = OUTPUT / 'after' / source.name
        diff = ImageChops.difference(Image.open(source), Image.open(destination))
        red, green, blue = diff.split()
        maximum = ImageChops.lighter(ImageChops.lighter(red, green), blue)
        pixels = diff.width * diff.height - maximum.histogram()[0]
        results[source.name] = {'changed_pixels': pixels, 'bbox': diff.getbbox(),
                                'mean_absolute_error_0_255': ImageStat.Stat(diff).mean}
    for name in ('arena', 'boss'):
        for renderer in (0, 1):
            a = Image.open(OUTPUT / f'after/{name}-renderer{renderer}.png')
            b = Image.open(OUTPUT / f'after/{name}-fallback-renderer{renderer}.png')
            assert ImageChops.difference(a, b).getbbox() is None, f'Shader A/B differs: {name}/{renderer}'
    print('PASS: four shader A/B captures are pixel-identical on the tested desktop GPU.')
    print(json.dumps(results, indent=2))


if __name__ == '__main__':
    main()
