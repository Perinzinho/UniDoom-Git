"""Read-only checks against the pre-task backup; run from the repository root."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
import sys
sys.path.insert(0, str(ROOT / 'Tools/ProjectOrganization'))
from resolve_paths import resolve
OUTPUT = ROOT / '.utmp/ClassRoomOptimization'


def read(path):
    return path.read_text(encoding='utf-8-sig')


def blocks(text):
    return {m[2]: (m[1], m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def guid(path):
    return re.search(r'(?m)^guid: ([a-f0-9]{32})', read(Path(str(path) + '.meta')))[1]


def texture_bindings(text):
    return {m[1]: m[2] for m in re.finditer(
        r'    - (\w+):\n        m_Texture: (\{[^\n]+\})', text)}


def main():
    inventory = json.loads(read(OUTPUT / 'static-inventory.json'))
    index = {g: resolve(ROOT / p) for g, p in json.loads(read(OUTPUT / 'guid-index.json')).items()}
    changed_meshes = 0
    cleared_texts = 0
    total_ids = 0
    for path in inventory['prefabs']:
        before = blocks(read(OUTPUT / 'source-before' / path))
        after = blocks(read(resolve(ROOT / path)))
        assert before.keys() == after.keys(), f'Prefab fileIDs changed: {path}'
        text_objects = {re.search(r'm_GameObject: \{fileID: (\d+)\}', b)[1]
                        for kind, b in after.values() if kind == '102'}
        total_ids += len(before)
        for fid, (kind, old) in before.items():
            new = after[fid][1]
            if old == new:
                continue
            assert kind == after[fid][0]
            if kind == '102':
                pattern = r'(?ms)^  m_Text:.*?(?=^  m_OffsetZ:)'
                assert re.search(pattern, new)[0].split(':', 1)[1].strip() in ('', '""', "''")
                assert re.sub(pattern, '', old) == re.sub(pattern, '', new)
                cleared_texts += 1
                continue
            if kind == '23' and re.search(r'm_GameObject: \{fileID: (\d+)\}', new)[1] in text_objects:
                assert '  m_Enabled: 0\n' in new
                assert re.sub(r'(?m)^  m_Enabled:.*$', '', old) == re.sub(r'(?m)^  m_Enabled:.*$', '', new)
                continue
            assert kind == after[fid][0] == '33', f'Unexpected component edit: {path}/{fid}'
            assert re.sub(r'(?m)^  m_Mesh:.*$', '', old) == re.sub(r'(?m)^  m_Mesh:.*$', '', new)
            a, b = [index[re.search(r'm_Mesh:.*guid: ([a-f0-9]{32})', t)[1]] for t in (old, new)]
            normalize = lambda p: re.sub(r'(?m)^  m_Name:[^\r\n]*', '', read(p))
            assert normalize(a) == normalize(b), f'Meshes differ: {a}, {b}'
            changed_meshes += 1
    repairs = json.loads(read(ROOT / 'Tools/ClassRoom/Optimization/texture-bindings.json'))['bindings']
    for entry in repairs:
        binding = texture_bindings(read(ROOT / entry['material']))[entry['property']]
        assert guid(ROOT / entry['texture']) in binding, entry
    changed_materials = 0
    for path in inventory['materials']:
        old = read(OUTPUT / 'source-before' / path)
        new = read(resolve(ROOT / path))
        if old == new:
            continue
        # All colors, controls, render states, UV transforms and subassets survive.
        def normalize(text):
            text = re.sub(r'(?m)^        m_Texture:.*$', '', text)
            return re.sub(r'  m_ValidKeywords:.*?(?=  m_LightmapFlags:)', '', text, flags=re.S)
        assert normalize(old) == normalize(new), f'Unexpected material edit: {path}'
        for binding in texture_bindings(new).values():
            match = re.search(r'guid: ([a-f0-9]{32})', binding)
            if match:
                assert match[1] in index and index[match[1]].is_file(), f'Broken texture: {path}/{binding}'
        changed_materials += 1
    importer_count = 0
    for backup in (OUTPUT / 'source-before').rglob('*.meta'):
        path = backup.relative_to(OUTPUT / 'source-before')
        old, new = read(backup), read(resolve(ROOT / path))
        normalize = lambda t: re.sub(r'(?m)^(\s*(?:textureType|sRGBTexture): )\d+', r'\1', t)
        assert normalize(old) == normalize(new), f'Unexpected importer edit: {path}'
        assert re.search(r'\bsRGBTexture: (\d+)', new)[1] == '0', f'Nonlinear data texture: {path}'
        if path.name.endswith(('nor_gl.png.meta', '_Normal.png.meta')):
            assert re.search(r'\btextureType: (\d+)', new)[1] == '1', f'Invalid normal import: {path}'
        importer_count += 1
    print(f'PASS: {total_ids} prefab fileIDs preserved; {changed_meshes} equivalent mesh references; '
          f'{len(repairs)} repaired texture slots; {changed_materials} changed materials; {importer_count} repaired imports; '
          f'{cleared_texts} texts cleared.')

    from PIL import Image, ImageChops, ImageStat, ImageDraw
    results = {}
    for source in sorted((OUTPUT / 'repaired').glob('*.png')):
        a = Image.open(source).convert('RGB')
        b = Image.open(OUTPUT / 'after' / source.name).convert('RGB')
        fallback = Image.open(OUTPUT / 'after/fallback' / source.name).convert('RGB')
        diff = ImageChops.difference(a, b)
        maximum = ImageChops.lighter(ImageChops.lighter(*diff.split()[:2]), diff.split()[2])
        results[source.name] = {'optimization_changed_pixels': a.width * a.height - maximum.histogram()[0],
                                'optimization_mean_error_0_255': ImageStat.Stat(diff).mean,
                                'shader_ab_identical': ImageChops.difference(b, fallback).getbbox() is None}
    assert results, 'No captures found'
    assert all(v['shader_ab_identical'] for v in results.values()), 'Shader A/B differs; inspect screenshot-results.json'
    (OUTPUT / 'screenshot-results.json').write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
    print(f'PASS: {len(results)} shader A/B captures are pixel-identical on the tested desktop GPU.')
    print(json.dumps(results, indent=2))
    sheet = Image.new('RGB', (1280, 1520), '#141414')
    draw = ImageDraw.Draw(sheet)
    for row, view in enumerate(('aisle', 'front', 'door', 'flashlight')):
        for col, label in enumerate(('before', 'after')):
            screenshot = Image.open(OUTPUT / label / f'{view}-renderer0.png').convert('RGB')
            sheet.paste(screenshot.resize((640, 360)), (col * 640, row * 380 + 20))
            draw.text((col * 640 + 8, row * 380 + 4), f'{label} - {view}', fill='white')
    sheet.save(ROOT / 'Tools/ClassRoom/Optimization/comparison.png')


if __name__ == '__main__':
    main()
