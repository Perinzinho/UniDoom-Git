"""Preserve prefab IDs and gameplay blocks while staging/installing verified lighting."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/ClassRoom-lighting-before'
INTERIOR = Path('Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab')
RUNTIME = Path('Assets/TotalChaos/Runtime/TCRegionAtmosphere.cs')
ART = Path('Assets/Art/ClassRoom/Lighting')
MATERIALS = Path('Assets/Art/SkillAtmosphere/Materials/ClassRoom')


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def stage():
    original = (BEFORE / INTERIOR).read_text(encoding='utf-8-sig')
    generated = (PREVIEW / INTERIOR).read_text(encoding='utf-8-sig')
    old, new = blocks(original), blocks(generated)
    assert set(old) <= set(new), 'Existing component ID removed'
    lights = {re.search(r'm_GameObject: \{fileID: (\d+)', b)[1]
              for k, b in old.values() if k == 108}
    light_transforms = {fid for fid, (k, b) in old.items() if k == 4 and
                        re.search(r'm_GameObject: \{fileID: (\d+)', b)[1] in lights}
    scripts = {'e9bd2423b59bca04ea3b4080e5132864',  # region
               '172515602e62fb746b5d573b38a5fe58',  # Volume
               '207a0c2d979225248a336fcc408c7512',  # flicker
               '474bcb49853aa07438625e644c072ee6'}  # URP light data
    result = original[:original.index('--- !u!')]
    changed = 0
    for fid, (kind, block) in old.items():
        initial = block
        if kind in (108, 215):
            block = new[fid][1]
        elif kind == 114 and re.search(r'm_Script:.*guid: (\w+)', block)[1] in scripts:
            block = new[fid][1]
        else:
            fields = ['m_LocalRotation', 'm_LocalEulerAnglesHint'] if fid in light_transforms else []
            if kind == 1 and fid in lights:
                fields += ['m_Component']
            for field in fields:
                pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
                a = re.search(pattern, block, re.M | re.S)
                b = re.search(pattern, new[fid][1], re.M | re.S)
                assert a and b
                block = block[:a.start()] + b[0] + block[a.end():]
        changed += block != initial
        result += block
    result += ''.join(block for fid, (_, block) in new.items() if fid not in old)
    final = blocks(result)
    for fid, (kind, block) in old.items():
        if kind in (33, 23, 65, 64, 54, 136):
            assert final[fid][1] == block, 'Mesh/renderer/collision/gameplay changed: ' + fid
        if kind == 4:
            for field in ['m_LocalPosition', 'm_LocalScale', 'm_Children', 'm_Father']:
                pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
                assert re.search(pattern, final[fid][1], re.M | re.S)[0] == re.search(pattern, block, re.M | re.S)[0]
    (PREVIEW / INTERIOR).write_bytes(result.encode('utf-8'))
    print(f'Staged: {len(old)} IDs retained, {changed} lighting blocks updated, {len(new)-len(old)} support blocks added.')


def log(name, markers):
    text = (ROOT / '.utmp' / name).read_text(encoding='utf-8', errors='replace')
    for marker in markers:
        assert marker in text, f'{name}: missing {marker}'
    for error in ['Exception:', 'error CS', 'Render Graph Execution error']:
        assert error not in text, f'{name}: {error}'


def install():
    log('ClassRoom-lighting-final.log', ['CLASSROOM_LIGHTING_PREFAB_OK', 'Exiting batchmode successfully'])
    log('ClassRoom-lighting-play.log', ['CLASSROOM_LIGHTING_PLAYMODE_OK'])
    log('ClassRoom-lighting-gameplay.log', ['CLASSROOM_ACTUAL_PLAYER_ROUTE_OK', 'CLASSROOM_DOOR_MOTION_VERIFIED', 'CLASSROOM_MAP_INHERITANCE_OK', 'Exiting batchmode successfully'])
    manifest = json.loads((BEFORE / 'manifest.json').read_text())
    # The runtime adapter is the only file implemented directly before the installation.
    for name, expected in manifest.items():
        if name == str(RUNTIME).replace('\\', '/'):
            assert (ROOT / name).read_bytes() == (PREVIEW / name).read_bytes(), 'Runtime changed during validation'
        else:
            assert digest(ROOT / name) == expected, 'Concurrent change: ' + name
    changes = [INTERIOR]
    changes += [Path('Assets/Art/ClassRoom/Materials/Plaster.mat'), Path('Assets/Art/ClassRoom/Materials/Floor.mat')]
    changes += [p.relative_to(PREVIEW) for p in (PREVIEW / MATERIALS).glob('*.mat')
                if p.read_bytes() != (ROOT / p.relative_to(PREVIEW)).read_bytes()]
    changes += [p.relative_to(PREVIEW) for p in (PREVIEW / ART).rglob('*') if p.is_file()]
    changes += [Path(str(p.relative_to(PREVIEW)) + '.meta') for p in [PREVIEW / ART] + list((PREVIEW / ART).rglob('*'))
                if p.is_dir() and Path(str(p) + '.meta').exists()]
    for path in changes:
        target = ROOT / path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(PREVIEW / path, target)
    captures = PREVIEW / 'Preview/ClassRoomLighting'
    for p in captures.glob('*'):
        if p.name.startswith(('Before-', 'Final-')) or p.name == 'runtime-validation.txt':
            shutil.copy2(p, ROOT / 'Tools/ClassRoom/Lighting' / p.name)
    metrics = {}
    for line in (captures / 'capture-metrics.txt').read_text().splitlines():
        if line.startswith(('Before ', 'Final ')):
            metrics[line.split(':')[0]] = line
    (ROOT / 'Tools/ClassRoom/Lighting/capture-metrics.txt').write_text('\n'.join(metrics.values())+'\n')
    # Keep the documented reproducible runtime source synchronized.
    shutil.copy2(ROOT / RUNTIME, ROOT / 'Tools/TotalChaosSkill/Unity/Runtime/TCRegionAtmosphere.cs')
    print(f'Installed {len(changes)} files; scene, player, geometry, collision and other maps unchanged.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    install() if args.install else stage()
