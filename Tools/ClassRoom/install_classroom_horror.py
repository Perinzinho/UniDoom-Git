"""Install the classroom revision only after successful isolated Unity validation."""
import argparse
import hashlib
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/ClassRoom-horror-before'
ART = Path('Assets/Art/ClassRoom')


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in
            re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def preserved_prefab(original, generated):
    """Retain existing YAML and IDs; update only furniture, lighting and lettering."""
    old, new = blocks(original), blocks(generated)
    if not set(old).issubset(new):
        raise ValueError('Existing classroom object IDs were removed')
    allowed = {
        4: ['m_LocalPosition', 'm_LocalScale', 'm_LocalRotation',
            'm_LocalEulerAnglesHint', 'm_Children'],
        23: ['m_Materials'],
        102: ['m_Text', 'm_CharacterSize'],
        108: ['m_Color', 'm_Intensity', 'm_Range'],
    }
    result = original[:original.index('--- !u!')]
    changed = 0
    for fid, (kind, block) in old.items():
        assert new[fid][0] == kind
        initial = block
        for field in allowed.get(kind, []):
            pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
            a = re.search(pattern, block, re.M | re.S)
            b = re.search(pattern, new[fid][1], re.M | re.S)
            if a and b:
                block = block[:a.start()] + b[0] + block[a.end():]
        if block != initial:
            changed += 1
        result += block
    result += ''.join(block for fid, (_, block) in new.items() if fid not in old)
    print(f'Preserved {len(old)} existing objects; updated {changed}; added {len(new) - len(old)}')
    return result.encode('utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    parser.add_argument('--stage', action='store_true', help='Stage preserved prefab YAML for final Unity verification')
    args = parser.parse_args()
    log = (ROOT / '.utmp/ClassRoom-horror-build.log').read_text(encoding='utf-8', errors='replace')
    for marker in ('CLASSROOM_HORROR_BUILD_OK', 'CLASSROOM_DOOR_MOTION_VERIFIED',
                   'CLASSROOM_ACTUAL_PLAYER_ROUTE_OK', 'CLASSROOM_MAP_INHERITANCE_OK',
                   'Exiting batchmode successfully'):
        assert marker in log, f'Missing Unity validation: {marker}'
    assert 'Exception:' not in log and 'error CS' not in log
    if args.install:
        final = (ROOT / '.utmp/ClassRoom-horror-final.log').read_text(encoding='utf-8', errors='replace')
        for marker in ('CLASSROOM_DOOR_MOTION_VERIFIED', 'CLASSROOM_ACTUAL_PLAYER_ROUTE_OK',
                       'CLASSROOM_MAP_INHERITANCE_OK', 'Exiting batchmode successfully'):
            assert marker in final, f'Missing validation of staged prefab: {marker}'
        assert 'Exception:' not in final and 'error CS' not in final
    # The nested interior is sufficient; map, functional door and player stay byte-identical.
    for name, path in (
        ('ClassRoom.prefab', 'Assets/prefabs/Scenario/ClassRoom.prefab'),
        ('Player.prefab', 'Assets/prefabs/Player/Player.prefab'),
        ('SampleScene.unity', 'Assets/Scenes/SampleScene.unity'),
    ):
        assert digest(BEFORE / name) == digest(ROOT / path), f'Concurrent edit: {path}'
        assert digest(BEFORE / name) == digest(PREVIEW / path), f'Unexpected generated edit: {path}'
    old_art = BEFORE / 'ClassRoom'
    for snapshot in old_art.rglob('*'):
        if snapshot.is_file():
            target = ROOT / ART / snapshot.relative_to(old_art)
            assert target.exists() and digest(snapshot) == digest(target), f'Concurrent edit: {target}'
    pending = {}
    for source in (PREVIEW / ART).rglob('*'):
        if not source.is_file():
            continue
        relative = source.relative_to(PREVIEW / ART)
        target = ROOT / ART / relative
        if not (old_art / relative).exists():
            if relative.parts[0] == 'Meshes' and not relative.name.startswith('Horror_'):
                continue
            if relative.name.startswith('AshenSkin.'):
                continue
        data = source.read_bytes()
        if relative.as_posix() == 'AbandonedClassRoomInterior.prefab' or relative.suffix == '.prefab':
            snapshot = old_art / relative
            if snapshot.exists():
                data = preserved_prefab(snapshot.read_text(encoding='utf-8'), data.decode('utf-8'))
        if not target.exists() or data != target.read_bytes():
            if target.exists() and not (old_art / relative).exists():
                raise ValueError(f'Untracked concurrent asset: {target}')
            pending[target] = data
    print(f'{len(pending)} classroom files ready to install')
    for target, data in pending.items():
        print(target.relative_to(ROOT).as_posix())
        if args.stage and target.suffix == '.prefab':
            (PREVIEW / target.relative_to(ROOT)).write_bytes(data)
        if args.install:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    for source in (PREVIEW / 'Preview').glob('ClassRoom-*.png'):
        if args.install:
            shutil.copy2(source, ROOT / 'Tools/ClassRoom' / source.name)
    if args.install:
        shutil.copy2(PREVIEW / 'Preview/validation.txt', ROOT / 'Tools/ClassRoom/validation.txt')
    print('Installed' if args.install else 'Staged' if args.stage else 'Dry run; use --install to apply the validated revision')


if __name__ == '__main__':
    main()
