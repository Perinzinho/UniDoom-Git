"""Stage or install the verified Cylinder revision while retaining existing objects and other rooms."""
import argparse
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/Cylinder-decay-before'
ART = Path('Assets/Art/Cylinder')
PREFAB = Path('Assets/prefabs/Scenario/Cylinder.prefab')


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in
            re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def merge(original, generated):
    old, new = blocks(original), blocks(generated)
    assert set(old).issubset(new), 'Existing objects removed'
    allowed = {4: ['m_Children'], 23: ['m_Materials', 'm_Enabled'], 102: ['m_Text'], 108: ['m_Intensity']}
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
    print(f'Preserved {len(old)} existing blocks, updated {changed}, added {len(new) - len(old)}')
    return result.encode('utf-8')


def validate(path, build=False):
    log = path.read_text(encoding='utf-8', errors='replace')
    markers = ['CYLINDER_DECAY_VERIFIED', 'CYLINDER_DECAY_MAP_OK', 'CYLINDER_DOORS_OK',
               'CYLINDER_PLAYER_ROUTES_OK', 'CYLINDER_CANTEEN_SUPPORT_OK', 'CYLINDER_ROOF_COVERAGE_OK',
               'CYLINDER_DECAY_PREFAB_DETAILS_OK', 'CYLINDER_DECAY_MAP_DETAILS_OK', 'Exiting batchmode successfully']
    if build:
        markers.append('CYLINDER_DECAY_BUILD_OK')
    for marker in markers:
        assert marker in log, f'Missing Unity validation: {marker}'
    assert 'Exception:' not in log and 'error CS' not in log and 'Render Graph Execution error' not in log


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    validate(ROOT / '.utmp/Cylinder-decay-build.log', build=True)
    if args.install:
        validate(ROOT / '.utmp/Cylinder-decay-verify.log')
    for name, relative in [('Cylinder.prefab', PREFAB), ('SampleScene.unity', Path('Assets/Scenes/SampleScene.unity')),
                           ('Player.prefab', Path('Assets/prefabs/Player/Player.prefab')),
                           ('Basement.prefab', Path('Assets/prefabs/Scenario/Basement.prefab')),
                           ('ClassRoom.prefab', Path('Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab'))]:
        assert (ROOT / relative).read_bytes() == (BEFORE / name).read_bytes(), f'Concurrent edit: {relative}'
        if name != 'Cylinder.prefab':
            assert (PREVIEW / relative).read_bytes() == (BEFORE / name).read_bytes(), f'Unrelated generated edit: {relative}'
    for snapshot in (BEFORE / 'Cylinder').rglob('*'):
        if snapshot.is_file():
            assert (ROOT / ART / snapshot.relative_to(BEFORE / 'Cylinder')).read_bytes() == snapshot.read_bytes(), f'Concurrent art edit: {snapshot.name}'
    prefab = merge((BEFORE / 'Cylinder.prefab').read_text(encoding='utf-8-sig'), (PREVIEW / PREFAB).read_text(encoding='utf-8-sig'))
    pending = []
    used_meshes = set(re.findall(r'guid: (\w+)', prefab.decode('utf-8')))
    for scene in (PREVIEW / ART).glob('*.unity'):
        used_meshes.update(re.findall(r'guid: (\w+)', scene.read_text(encoding='utf-8-sig')))
    for source in (PREVIEW / ART).rglob('*'):
        if not source.is_file():
            continue
        relative = source.relative_to(PREVIEW / ART)
        target = ROOT / ART / relative
        existing = (BEFORE / 'Cylinder' / relative).exists()
        if not existing and not relative.name.startswith(('Decay', 'SealedDoorPanel', 'SealedDoorSlab')):
            continue
        if not existing and relative.parts[0] == 'Meshes':
            meta = source if source.suffix == '.meta' else Path(str(source) + '.meta')
            guid = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)[1]
            if guid not in used_meshes:
                continue
        if not target.exists() or target.read_bytes() != source.read_bytes():
            assert not target.exists() or existing, f'Unexpected concurrent asset: {target}'
            pending.append((source, target))
    print(f'{len(pending)} art files ready')
    destination = ROOT if args.install else PREVIEW
    if args.install:
        for source, target in pending:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
    (destination / PREFAB).write_bytes(prefab)
    if args.install:
        for source in (PREVIEW / 'Preview').glob('Cylinder-*.png'):
            shutil.copy2(source, ROOT / 'Tools/Cylinder' / source.name)
        shutil.copy2(PREVIEW / 'Preview/Cylinder-decay-validation.txt', ROOT / 'Tools/Cylinder/decay-validation.txt')
    print('Installed verified revision' if args.install else 'Staged preserved prefab; run CylinderDecayVerifier.Verify before --install')


if __name__ == '__main__':
    main()
