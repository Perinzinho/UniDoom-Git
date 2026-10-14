"""Stage only material references; install after isolated Unity validation."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/ClassRoom-PBR/before'
TARGETS = ['Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab', 'Assets/prefabs/Scenario/ClassRoom.prefab']
ART = 'Assets/Art/ClassRoom/Surface'


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def stage():
    for path in TARGETS:
        original = (BEFORE/path).read_text(encoding='utf-8-sig')
        generated = (PREVIEW/path).read_text(encoding='utf-8-sig')
        old, new = blocks(original), blocks(generated)
        assert set(old) == set(new), f'Object IDs changed: {path}'
        result = original[:original.index('--- !u!')]
        changed = 0
        for fid, (kind, block) in old.items():
            if kind in (23, 215):
                field = 'm_Materials' if kind == 23 else 'm_CustomBakedTexture'
                pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
                a, b = re.search(pattern, block, re.M|re.S), re.search(pattern, new[fid][1], re.M|re.S)
                if a and b:
                    changed += a[0] != b[0]
                    block = block[:a.start()] + b[0] + block[a.end():]
            result += block
        (PREVIEW/path).write_bytes(result.encode('utf-8'))
        print(path, 'material lists changed:', changed, 'IDs retained:', len(old))


def require_log(name, markers):
    text = (ROOT/'.utmp'/name).read_text(encoding='utf-8', errors='replace')
    for marker in markers:
        assert marker in text, f'{name}: missing {marker}'
    for error in ['error CS', 'Exception:', 'Shader error in', 'Render Graph Execution error']:
        assert error not in text, f'{name}: {error}'


def install():
    require_log('ClassRoom-PBR-final.log', ['CLASSROOM_PBR_REVERSAL_OK', 'CLASSROOM_PBR_RENDER_OK', 'CLASSROOM_PBR_SRP_BATCHER_OK',
        'CLASSROOM_PBR_GPU_CONTROLS_OK', 'CLASSROOM_ACTUAL_PLAYER_ROUTE_OK', 'CLASSROOM_DOOR_MOTION_VERIFIED', 'CLASSROOM_MAP_INHERITANCE_OK',
        'CLASSROOM_PBR_ALL_CHECKS_OK', 'Exiting batchmode successfully'])
    require_log('ClassRoom-PBR-play.log', ['CLASSROOM_LIGHTING_PLAYMODE_OK'])
    manifest = json.loads((BEFORE/'manifest.json').read_text())
    for name, expected in manifest.items():
        assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest() == expected, 'Concurrent change: ' + name
    for path in TARGETS:
        old = blocks((BEFORE/path).read_text(encoding='utf-8-sig'))
        new = blocks((PREVIEW/path).read_text(encoding='utf-8-sig'))
        assert old.keys() == new.keys()
        for fid, (kind, block) in old.items():
            if kind in (23, 215):
                field = 'm_Materials' if kind == 23 else 'm_CustomBakedTexture'
                pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
                assert re.sub(pattern, '', block, flags=re.M|re.S) == re.sub(pattern, '', new[fid][1], flags=re.M|re.S)
            else:
                assert block == new[fid][1], f'Non-material block changed: {path}:{fid}'
        shutil.copy2(PREVIEW/path, ROOT/path)
    for p in (PREVIEW/ART).rglob('*'):
        if p.is_file():
            target = ROOT/p.relative_to(PREVIEW); target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(p, target)
    for p in [PREVIEW/ART] + list((PREVIEW/ART).rglob('*')):
        if p.is_dir() and Path(str(p)+'.meta').exists():
            shutil.copy2(Path(str(p)+'.meta'), Path(str(ROOT/p.relative_to(PREVIEW))+'.meta'))
    for path in ['Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs', 'Assets/TotalChaos/Shaders/TotalChaosInspiredSurface.shader',
                 'Assets/TotalChaos/Shaders/TotalChaosSurfaceInput.hlsl', 'Assets/TotalChaos/Shaders/LICENSE-Unity.md', 'Assets/TotalChaos/Shaders']:
        meta = PREVIEW/(path+'.meta')
        if meta.exists(): shutil.copy2(meta, ROOT/(path+'.meta'))
    captures = PREVIEW/'Preview/ClassRoomLighting'
    for p in captures.glob('PBR*.png'): shutil.copy2(p, ROOT/'Tools/ClassRoom/Surface'/p.name)
    shutil.copy2(captures/'runtime-validation.txt', ROOT/'Tools/ClassRoom/Surface/runtime-validation.txt')
    values = {}
    for line in (captures/'capture-metrics.txt').read_text().splitlines():
        if line.startswith(('PBRBefore ', 'PBRFinal ')): values[line.split(':')[0]] = line
    (ROOT/'Tools/ClassRoom/Surface/capture-metrics.txt').write_text('\n'.join(values.values())+'\n')
    print('Installed PBR materials. Original material assets, lighting, geometry and gameplay retained.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    install() if args.install else stage()
