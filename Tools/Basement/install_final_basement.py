"""Stage or install the final basement revision, preserving unrelated scene data."""
import argparse
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/Basement-final-before'
ART = Path('Assets/Art/Basement')
PREFAB = Path('Assets/prefabs/Scenario/Basement.prefab')
SCENE = Path('Assets/Scenes/SampleScene.unity')


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def replace_field(block, generated, name):
    pattern = rf'^  {re.escape(name)}:.*?(?=^  [A-Za-z_]|\Z)'
    a, b = re.search(pattern, block, re.M | re.S), re.search(pattern, generated, re.M | re.S)
    return block[:a.start()] + b[0] + block[a.end():] if a and b else block


def merge_prefab(original, generated):
    old, new = blocks(original), blocks(generated)
    allowed = {
        1: ['m_IsActive', 'm_Component'],
        4: ['m_LocalPosition', 'm_LocalRotation', 'm_LocalScale', 'm_LocalEulerAnglesHint', 'm_Children'],
        23: ['m_Enabled', 'm_Materials'], 33: ['m_Mesh'],
        64: ['m_Enabled', 'm_Mesh'], 65: ['m_Enabled', 'm_IsTrigger', 'm_Center', 'm_Size'],
        114: ['arenaCenter', 'arenaSize'],
    }
    removed = set(old) - set(new)
    for fid in removed:
        kind, block = old[fid]
        assert kind == 114 and ('m_Faces:' in block or 'm_Vertices:' in block or 'm_Positions:' in block), f'Unexpected component removed: {fid}'
    result = original[:original.index('--- !u!')]
    changed = 0
    for fid, (kind, block) in old.items():
        if fid in removed:
            continue
        assert kind == new[fid][0]
        initial = block
        for name in allowed.get(kind, []):
            block = replace_field(block, new[fid][1], name)
        changed += block != initial
        result += block
    result += ''.join(block for fid, (_, block) in new.items() if fid not in old)
    print(f'Prefab: {changed} updated blocks, {len(new) - len(old) + len(removed)} added, {len(removed)} structural ProBuilder components removed')
    return result


def merge_scene(original, generated):
    old, new = blocks(original), blocks(generated)
    result = original
    # Only basement and its two previously designated key-door instances may change.
    allowed = {'969722462', '258975767', '1896838222'}
    for fid in allowed:
        assert old[fid][0] == new[fid][0] == 1001
        result = result.replace(old[fid][1], new[fid][1])
    # The obsolete scene-added entrance wall otherwise duplicates the restored original wall.
    for fid, (kind, block) in old.items():
        if fid in allowed or fid not in new:
            continue
        if kind == 1 and fid == '233188829':
            result = result.replace(block, replace_field(block, new[fid][1], 'm_IsActive'))
    for fid, (kind, block) in new.items():
        if fid not in old:
            assert kind in (4, 1, 33, 23, 65, 114, 1001), f'Unexpected new scene data: {fid}'
            result += block
    output = blocks(result)
    assert set(old).issubset(output)
    changed = {fid for fid in old if old[fid] != output[fid]}
    assert changed <= allowed | {'233188829'}, changed
    print('Scene: basement, its 2 key doors and obsolete entrance-wall activity only')
    return result


def validate_log(path, build=False):
    log = path.read_text(encoding='utf-8', errors='replace')
    markers = ['BASEMENT_FINAL_VERIFIED', 'BASEMENT_FINAL_PREFAB_PLAYER_OK',
               'BASEMENT_FINAL_MAP_PLAYER_OK', 'BASEMENT_FINAL_DOORS_OK',
               'BASEMENT_ENCOUNTER_TEST_OK', 'Exiting batchmode successfully']
    if build:
        markers.append('BASEMENT_FINAL_BUILD_OK')
    else:
        markers.append('BASEMENT_FINAL_NEIGHBOURS_OK')
    for marker in markers:
        assert marker in log, f'Missing Unity validation: {marker}'
    assert 'Exception:' not in log and 'error CS' not in log


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    validate_log(ROOT / '.utmp/Basement-final-build.log', build=True)
    if args.install:
        validate_log(ROOT / '.utmp/Basement-final-verify.log')
    for name, path in [('Basement.prefab', PREFAB), ('SampleScene.unity', SCENE),
                       ('Player.prefab', Path('Assets/prefabs/Player/Player.prefab')),
                       ('ClassRoom.prefab', Path('Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab'))]:
        assert (ROOT / path).read_bytes() == (BEFORE / name).read_bytes(), f'Concurrent edit: {path}'
    for snapshot in (BEFORE / 'Basement').rglob('*'):
        if snapshot.is_file():
            target = ROOT / ART / snapshot.relative_to(BEFORE / 'Basement')
            assert target.read_bytes() == snapshot.read_bytes(), f'Concurrent art edit: {target}'
    prefab = merge_prefab((BEFORE / 'Basement.prefab').read_text(encoding='utf-8-sig'), (PREVIEW / PREFAB).read_text(encoding='utf-8-sig'))
    scene = merge_scene((BEFORE / 'SampleScene.unity').read_text(encoding='utf-8-sig'), (PREVIEW / SCENE).read_text(encoding='utf-8-sig'))
    destination = ROOT if args.install else PREVIEW
    pending = []
    for source in (PREVIEW / ART).rglob('*'):
        if not source.is_file():
            continue
        relative = source.relative_to(PREVIEW / ART)
        # Ignore old temporary generator output not used by this revision.
        existing = (BEFORE / 'Basement' / relative).exists()
        if not existing and not relative.name.startswith(('Final', 'BasementFinal')):
            continue
        target = ROOT / ART / relative
        if not target.exists() or target.read_bytes() != source.read_bytes():
            assert not target.exists() or existing, f'Concurrent asset: {target}'
            pending.append((source, destination / ART / relative))
    print(f'{len(pending)} art files ready')
    if args.install:
        for source, target in pending:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
    (destination / PREFAB).write_text(prefab, encoding='utf-8', newline='\n')
    (destination / SCENE).write_text(scene, encoding='utf-8', newline='\n')
    if args.install:
        for source in (PREVIEW / 'Preview').glob('Basement-*.png'):
            shutil.copy2(source, ROOT / 'Tools/Basement' / source.name)
        shutil.copy2(PREVIEW / 'Preview/Basement-final-validation.txt', ROOT / 'Tools/Basement/final-validation.txt')
    print('Installed verified revision' if args.install else 'Staged preserved YAML; run BasementFinalVerifier.Verify before --install')


if __name__ == '__main__':
    main()
