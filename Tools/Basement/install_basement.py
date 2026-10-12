"""Install generated basement assets without reserializing the existing room.

Run with --install after BasementBuilder.Build succeeds in .utmp/BasementPreview.
Without --install, write the preserved-block prefab to the isolated project only.
"""
import argparse
import re
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
PREFAB = Path('Assets/prefabs/Scenario/Basement.prefab')


def blocks(text):
    return {int(match[2]): (int(match[1]), match[0]) for match in
            re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def region(text, name):
    found = re.search(rf'  {name}:[^\n]*(?:\n  -[^\n]*)*', text)
    if not found:
        raise ValueError(f'Missing field {name}')
    return found[0]


def merge(original, generated):
    old = blocks(original)
    new = blocks(generated)
    if 'BasementArenaDetails' in original:
        raise ValueError('Basement already installed')
    output = original[:original.index('--- !u!')]
    for file_id, (kind, block) in old.items():
        if file_id not in new or new[file_id][0] != kind:
            raise ValueError(f'Original object lost: {file_id}')
        rebuilt = new[file_id][1]
        if kind == 4:
            before = region(block, 'm_Children')
            after = region(rebuilt, 'm_Children')
            old_children = re.findall(r'fileID: (-?\d+)', before)
            new_children = re.findall(r'fileID: (-?\d+)', after)
            if new_children[:len(old_children)] != old_children:
                raise ValueError(f'Existing hierarchy changed: {file_id}')
            block = block.replace(before, after)
        elif kind == 23:
            block = block.replace(region(block, 'm_Materials'), region(rebuilt, 'm_Materials'))
        # Only mesh references are copied; original ProBuilder face/vertex data stays intact.
        if kind in (33, 64, 114):
            match = re.search(r'  m_Mesh: [^\n]*', block)
            replacement = re.search(r'  m_Mesh: [^\n]*', rebuilt)
            if match and replacement:
                block = block.replace(match[0], replacement[0])
        output += block
    output += ''.join(re.sub(r'[ \t]+(?=\n)', '', block)
                      for file_id, (_, block) in new.items() if file_id not in old)
    # Verify original transforms, colliders and PB geometry after whitelisting only additions.
    result = blocks(output)
    for file_id, (kind, block) in old.items():
        after = result[file_id][1]
        if kind == 4:
            after = after.replace(region(after, 'm_Children'), region(block, 'm_Children'))
        elif kind == 23:
            after = after.replace(region(after, 'm_Materials'), region(block, 'm_Materials'))
        if kind in (33, 64, 114):
            after = re.sub(r'  m_Mesh: [^\n]*', '  m_Mesh: VERIFIED', after)
            block = re.sub(r'  m_Mesh: [^\n]*', '  m_Mesh: VERIFIED', block)
        if after != block:
            raise ValueError(f'Unapproved modification to original block: {file_id}')
    print(f'Preserved {len(old)} original objects; added {len(new)-len(old)} blocks')
    return output


def patch_scene(text):
    """Replace the old arena-exit door; finish only the two other basement doors."""
    material_meta = (PREVIEW / 'Assets/Art/Basement/Materials/Steel.mat.meta').read_text()
    steel = re.search(r'guid: (\w+)', material_meta)[1]
    prefab_guid = 'a6d30893aa935334a8d77242fdbc3c2b'
    for instance_id, name in [(258975767, 'DoorPrefab (4)'),
                              (952704914, 'DoorPrefab (5)'),
                              (1896838222, 'DoorPrefab (6)')]:
        match = re.search(rf'--- !u!1001 &{instance_id}\n.*?(?=--- !u!|\Z)', text, re.S)
        if not match or name not in match[0] or prefab_guid not in match[0]:
            raise ValueError(f'Basement door instance changed: {name}')
        block = match[0]
        additions = ''
        if instance_id == 952704914:
            additions += (f'    - target: {{fileID: 3304891777103809538, guid: {prefab_guid}, type: 3}}\n'
                          '      propertyPath: m_IsActive\n      value: 0\n'
                          '      objectReference: {fileID: 0}\n')
        else:
            block = block.replace('guid: df759e98d250feb49a4ebcd4008e4a30', f'guid: {steel}')
            additions += (f'    - target: {{fileID: -5098522852723664778, guid: {prefab_guid}, type: 3}}\n'
                          "      propertyPath: 'm_Materials.Array.data[0]'\n      value:\n"
                          f'      objectReference: {{fileID: 2100000, guid: {steel}, type: 2}}\n')
            if instance_id == 1896838222:
                for key in ('requiresKey01', 'requiresKey02', 'requiresKey03'):
                    additions += (f'    - target: {{fileID: 6318899405212657097, guid: {prefab_guid}, type: 3}}\n'
                                  f'      propertyPath: {key}\n      value: 0\n'
                                  '      objectReference: {fileID: 0}\n')
        block = block.replace('    m_RemovedComponents:', additions + '    m_RemovedComponents:')
        text = text[:match.start()] + block + text[match.end():]
    return text


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    original = (ROOT / '.utmp/Basement-original.prefab').read_text(encoding='utf-8-sig')
    generated_path = PREVIEW / PREFAB
    result = merge(original, generated_path.read_text(encoding='utf-8-sig'))
    destination = ROOT if args.install else PREVIEW
    scene_path = Path('Assets/Scenes/SampleScene.unity')
    scene = patch_scene((ROOT / scene_path).read_text(encoding='utf-8-sig'))
    if args.install:
        if (ROOT / PREFAB).read_text(encoding='utf-8-sig') != original:
            raise ValueError('Basement source changed since generation; refusing to overwrite')
        art = Path('Assets/Art/Basement')
        shutil.copytree(PREVIEW / art, ROOT / art, dirs_exist_ok=True)
        shutil.copy2(PREVIEW / (str(art)+'.meta'), ROOT / (str(art)+'.meta'))
        for preview in (PREVIEW / 'Preview').glob('Basement-*.png'):
            shutil.copy2(preview, ROOT / 'Tools/Basement' / preview.name)
    (destination / PREFAB).write_text(result, encoding='utf-8', newline='\n')
    (destination / scene_path).write_text(scene, encoding='utf-8', newline='\n')
    print('Installed to', destination)


if __name__ == '__main__':
    main()
