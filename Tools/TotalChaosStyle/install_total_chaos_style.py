"""Stage/install the authored Total Chaos inspired revision after isolated Unity checks."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/TotalChaos-before'
ART = Path('Assets/Art/TotalChaosInspired')
TARGETS = [Path(p) for p in (
    'Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab',
    'Assets/prefabs/Scenario/Basement.prefab',
    'Assets/Art/Basement/BasementFinalArena.prefab',
    'Assets/Art/Basement/BasementFinalSanctum.prefab',
    'Assets/prefabs/Scenario/Cylinder.prefab')]


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def merge(original, generated):
    old, new = blocks(original), blocks(generated)
    assert set(old) <= set(new), 'Original object/component IDs removed'
    allowed = {4: ['m_Children'], 23: ['m_Enabled', 'm_Materials'], 33: ['m_Mesh']}
    result = original[:original.index('--- !u!')]
    changed = 0
    for fid, (kind, block) in old.items():
        assert kind == new[fid][0]
        initial = block
        for field in allowed.get(kind, []):
            pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
            a, b = re.search(pattern, block, re.M | re.S), re.search(pattern, new[fid][1], re.M | re.S)
            if a and b:
                block = block[:a.start()] + b[0] + block[a.end():]
        changed += block != initial
        result += block
    result += ''.join(block for fid, (_, block) in new.items() if fid not in old)
    print(f'  {len(old)} original blocks retained; {changed} updated; {len(new)-len(old)} added')
    return result.encode('utf-8')


def log(name, markers):
    text = (ROOT / '.utmp' / name).read_text(encoding='utf-8', errors='replace')
    for marker in markers + ['Exiting batchmode successfully']:
        assert marker in text, f'{name}: missing {marker}'
    for error in ['Exception:', 'error CS', 'Render Graph Execution error']:
        assert error not in text, f'{name}: {error}'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    log('TotalChaos-build.log', ['TOTAL_CHAOS_STYLE_BUILD_OK', 'TOTAL_CHAOS_STYLE_VISUALS_VERIFIED', 'TOTAL_CHAOS_STANDARD_DOORS_OK'])
    if args.install:
        log('TotalChaos-verify.log', ['TOTAL_CHAOS_STYLE_VISUALS_VERIFIED', 'TOTAL_CHAOS_STANDARD_DOORS_OK'])
        log('TotalChaos-classroom.log', ['CLASSROOM_ACTUAL_PLAYER_ROUTE_OK', 'CLASSROOM_DOOR_MOTION_VERIFIED', 'CLASSROOM_MAP_INHERITANCE_OK'])
        log('TotalChaos-basement.log', ['BASEMENT_FINAL_VERIFIED', 'BASEMENT_FINAL_MAP_PLAYER_OK', 'BASEMENT_FINAL_NEIGHBOURS_OK'])
        log('TotalChaos-cylinder.log', ['CYLINDER_DECAY_VERIFIED', 'CYLINDER_PLAYER_ROUTES_OK', 'CYLINDER_DOORS_OK', 'CYLINDER_ROOF_COVERAGE_OK'])
    manifest = json.loads((BEFORE / 'manifest.json').read_text(encoding='utf-8'))
    for name, expected in manifest.items():
        assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, f'Concurrent main-project edit: {name}'
    for name in ('Assets/Scenes/SampleScene.unity', 'Assets/prefabs/Player/Player.prefab', 'Assets/prefabs/Scenario/ClassRoom.prefab'):
        assert (PREVIEW/name).read_bytes() == (BEFORE/name).read_bytes(), f'Unrelated generation edit: {name}'
    prefab_data = {}
    for target in TARGETS:
        print(target)
        prefab_data[target] = merge((BEFORE/target).read_text(encoding='utf-8-sig'), (PREVIEW/target).read_text(encoding='utf-8-sig'))
    if not args.install:
        for target, data in prefab_data.items():
            (PREVIEW/target).write_bytes(data)
        print('Staged. Run visual + classroom + basement + cylinder checks before --install.')
        return
    for target, data in prefab_data.items():
        assert data == (PREVIEW/target).read_bytes(), f'Staged prefab changed: {target}'
    # Install only the authored assets referenced by the final prefabs, including material textures/shaders.
    used = set()
    for data in prefab_data.values():
        used.update(re.findall(r'guid: (\w+)', data.decode('utf-8')))
    sources = {}
    for meta in (PREVIEW/ART).rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match and Path(str(meta)[:-5]).is_file():
            sources[match[1]] = Path(str(meta)[:-5])
    queue = list(used)
    while queue:
        guid = queue.pop()
        if guid not in sources or sources[guid].suffix != '.mat':
            continue
        for ref in re.findall(r'guid: (\w+)', sources[guid].read_text(encoding='utf-8-sig')):
            if ref not in used:
                used.add(ref)
                queue.append(ref)
    count = 0
    for guid, source in sources.items():
        if guid not in used:
            continue
        target = ROOT / source.relative_to(PREVIEW)
        assert not target.exists(), f'Unexpected new asset already exists: {target}'
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        shutil.copy2(Path(str(source)+'.meta'), Path(str(target)+'.meta'))
        count += 1
    for directory in [PREVIEW/ART] + list((PREVIEW/ART).rglob('*')):
        if directory.is_dir() and Path(str(directory)+'.meta').exists():
            destination = ROOT / directory.relative_to(PREVIEW)
            destination.mkdir(parents=True, exist_ok=True)
            shutil.copy2(Path(str(directory)+'.meta'), Path(str(destination)+'.meta'))
    for target, data in prefab_data.items():
        (ROOT/target).write_bytes(data)
    for image in (PREVIEW/'Preview').glob('TotalChaos-*.png'):
        shutil.copy2(image, ROOT/'Tools/TotalChaosStyle'/image.name)
    for name in ('TotalChaos-style-validation.txt', 'TotalChaos-geometry.txt'):
        shutil.copy2(PREVIEW/'Preview'/name, ROOT/'Tools/TotalChaosStyle'/name)
    print(f'Installed {count} authored assets plus preserved prefabs and previews.')


if __name__ == '__main__':
    main()
