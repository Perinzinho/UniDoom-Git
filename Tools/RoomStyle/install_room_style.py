"""Install the verified room revision, retaining geometry data and unrelated scene edits."""
import argparse
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/RoomStyle-before'
BASEMENT = Path('Assets/prefabs/Scenario/Basement.prefab')
CLASSROOM = Path('Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab')
SCENE = Path('Assets/Scenes/SampleScene.unity')
BASEMENT_GUID = '29573391a8065934c96646bc09b52bcc'


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in
            re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def field(text, name):
    m = re.search(rf'^  {re.escape(name)}:[^\n]*', text, re.M)
    if not m:
        raise ValueError(f'Missing {name}')
    return m[0]


def merge_prefab(before, generated, classroom=False):
    old, new = blocks(before), blocks(generated)
    assert set(old) == set(new), 'Lighting/resize must retain every object ID'
    output = before[:before.index('--- !u!')]
    affected = set()
    for fid, (kind, block) in old.items():
        assert kind == new[fid][0]
        fields = ['m_Color', 'm_Intensity'] if kind == 108 else []
        if not classroom and kind == 4:
            fields = ['m_LocalPosition', 'm_LocalScale']
        if not classroom and kind == 114 and 'arenaSize:' in block:
            fields = ['arenaCenter', 'arenaSize']
        for name in fields:
            a, b = field(block, name), field(new[fid][1], name)
            if a != b:
                block = block.replace(a, b)
                affected.add(fid)
        output += block
    print(f'Preserved {len(old)} objects; updated {len(affected)} objects')
    return output, affected


def scene_entries(block):
    return list(re.finditer(r'^    - target: [^\n]*\n.*?(?=^    - target:|^    m_RemovedComponents:)',
                            block, re.S | re.M))


def entry_key(entry):
    target = re.search(r'target: \{fileID: (-?\d+), guid: (\w+)', entry)
    prop = re.search(r'propertyPath: (.*)', entry)
    assert target and prop
    return target[1], target[2], prop[1]


def merge_scene(before, generated, affected):
    old, new = blocks(before), blocks(generated)
    result = before
    for fid, (kind, block) in old.items():
        if kind != 1001 or f'guid: {BASEMENT_GUID}' not in block:
            continue
        rebuilt = new[fid][1]
        def allowed(key):
            return key[1] == BASEMENT_GUID and key[0] in affected and (
                key[2].startswith(('m_LocalPosition.', 'm_LocalScale.', 'm_Color.'))
                or key[2] in ('m_Intensity', 'arenaSize.x', 'arenaSize.y', 'arenaSize.z',
                              'arenaCenter.x', 'arenaCenter.y', 'arenaCenter.z'))
        replacements = {entry_key(m[0]): m[0] for m in scene_entries(rebuilt) if allowed(entry_key(m[0]))}
        for m in reversed(scene_entries(block)):
            key = entry_key(m[0])
            if allowed(key):
                block = block[:m.start()] + replacements.pop(key, '') + block[m.end():]
        if replacements:
            block = block.replace('    m_RemovedComponents:', ''.join(replacements.values())+'    m_RemovedComponents:')
        result = result.replace(old[fid][1], block)
    # Only basement prefab overrides may change. User edits in other blocks stay byte-identical.
    result_blocks = blocks(result)
    for fid, (kind, block) in old.items():
        if kind != 1001 or f'guid: {BASEMENT_GUID}' not in block:
            assert result_blocks[fid][1] == block
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    log = (ROOT / '.utmp/RoomStyle-build.log').read_text(encoding='utf-8-sig')
    assert all(s in log for s in ['ROOM_STYLE_BUILD_OK', 'ROOM_STYLE_VERIFIED',
        'PASSAGE_MAP_TRAVERSAL_OK', 'PASSAGE_PREFAB_TRAVERSAL_OK',
        'BASEMENT_ENCOUNTER_TEST_OK', 'CLASSROOM_VERIFIED']), 'Unity checks must pass'
    old_b = (BEFORE / 'Basement.prefab').read_text(encoding='utf-8-sig')
    old_c = (BEFORE / 'AbandonedClassRoomInterior.prefab').read_text(encoding='utf-8-sig')
    old_s = (BEFORE / 'SampleScene.unity').read_text(encoding='utf-8-sig')
    b, affected = merge_prefab(old_b, (PREVIEW / BASEMENT).read_text(encoding='utf-8-sig'))
    c, _ = merge_prefab(old_c, (PREVIEW / CLASSROOM).read_text(encoding='utf-8-sig'), True)
    s = merge_scene(old_s, (PREVIEW / SCENE).read_text(encoding='utf-8-sig'), affected)
    destination = ROOT if args.install else PREVIEW
    if args.install:
        for path, expected in [(BASEMENT, old_b), (CLASSROOM, old_c), (SCENE, old_s)]:
            assert (ROOT / path).read_text(encoding='utf-8-sig') == expected, f'Concurrent change: {path}'
        for area in ['Basement', 'ClassRoom']:
            for p in (ROOT / f'Assets/Art/{area}/Materials').glob('*.mat'):
                assert p.read_bytes() == (BEFORE / f'{area}Materials' / p.name).read_bytes(), f'Concurrent material edit: {p}'
        for p in (ROOT / 'Assets/Art/ClassRoom/Textures').glob('*.meta'):
            assert p.read_bytes() == (BEFORE / 'ClassRoomTextureMeta' / p.name).read_bytes(), f'Concurrent texture edit: {p}'
        for area in ['Basement', 'ClassRoom']:
            art = Path(f'Assets/Art/{area}')
            for p in (PREVIEW / art / 'Materials').glob('*.mat'):
                shutil.copy2(p, ROOT / art / 'Materials' / p.name)
        for p in (PREVIEW / 'Assets/Art/ClassRoom/Textures').glob('*.meta'):
            shutil.copy2(p, ROOT / 'Assets/Art/ClassRoom/Textures' / p.name)
        for scene in ['Assets/Art/Basement/BasementPreview.unity', 'Assets/Art/ClassRoom/ClassRoomPreview.unity']:
            clean = re.sub(r'[ \t]+(?=\r?$)', '', (PREVIEW / scene).read_text(encoding='utf-8-sig'), flags=re.M)
            (ROOT / scene).write_text(clean, encoding='utf-8', newline='\n')
        for p in (PREVIEW / 'Preview').glob('Basement-*.png'):
            shutil.copy2(p, ROOT / 'Tools/Basement' / p.name)
        (ROOT / 'Tools/ClassRoom').mkdir(exist_ok=True)
        for p in (PREVIEW / 'Preview').glob('ClassRoom-*.png'):
            shutil.copy2(p, ROOT / 'Tools/ClassRoom' / p.name)
    for path, text in [(BASEMENT, b), (CLASSROOM, c), (SCENE, s)]:
        (destination / path).write_text(text, encoding='utf-8', newline='\n')
    print('Installed preserved revision to', destination)


if __name__ == '__main__':
    main()
