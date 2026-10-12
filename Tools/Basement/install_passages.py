"""Install the validated player-size correction, preserving unrelated scene data."""
import sys
sys.dont_write_bytecode = True
import re
from pathlib import Path
from install_basement import blocks, region

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'


def field(block, rebuilt, name):
    return block.replace(region(block, name), region(rebuilt, name))


def merge_prefab(original, generated):
    old, new = blocks(original), blocks(generated)
    result = original[:original.index('--- !u!')]
    changed = []
    for object_id, (kind, block) in old.items():
        assert object_id in new, f'Original prefab object removed: {object_id}'
        rebuilt = new[object_id][1]
        value = block
        if kind == 4:
            for name in ('m_LocalPosition', 'm_LocalScale', 'm_Children'):
                value = field(value, rebuilt, name)
        elif kind == 1:
            value = field(value, rebuilt, 'm_IsActive')
        if value != block:
            changed.append(object_id)
        result += value
    result += ''.join(re.sub(r'[ \t]+(?=\n)', '', block)
                      for object_id, (_, block) in new.items() if object_id not in old)
    print(f'Prefab: {len(changed)} geometry/hierarchy blocks adjusted; {len(new)-len(old)} new blocks.')
    return result


def merge_scene(original, generated):
    old, new = blocks(original), blocks(generated)
    result = original
    instance_id = 969722462
    item_pattern = r'    - target: [^\n]*\n      propertyPath: [^\n]*\n      value:[^\n]*\n      objectReference:[^\n]*\n'
    def relevant(item):
        path = re.search(r'propertyPath: ([^\n]*)', item)[1].strip("'")
        return path.startswith(('m_LocalPosition.', 'm_LocalScale.')) or path == 'm_IsActive'
    for target_instance in (instance_id, 258975767, 1896838222):
        original_instance = old[target_instance][1]
        rebuilt = new[target_instance][1]
        original_items = re.findall(item_pattern, original_instance)
        new_items = re.findall(item_pattern, rebuilt)
        retained = [i for i in original_items if not relevant(i)]
        updated = [i for i in new_items if relevant(i)]
        start = original_instance.index('    m_Modifications:\n') + len('    m_Modifications:\n')
        end = original_instance.index('    m_RemovedComponents:')
        corrected = original_instance[:start] + ''.join(retained + updated) + original_instance[end:]
        if target_instance == instance_id:
            # The old scene had deleted the exit header; the gate now follows this header.
            corrected = corrected.replace('    - {fileID: 7195092520332256153, guid: 29573391a8065934c96646bc09b52bcc, type: 3}\n', '')
        corrected = re.sub(r'[ \t]+(?=\n)', '', corrected)
        result = result.replace(original_instance, corrected)
    # Disable two obsolete wall fragments and resize the scene's added entrance wall.
    for object_id in (1587741511, 196126984, 233188830):
        kind, block = old[object_id]
        rebuilt = new[object_id][1]
        value = block
        if kind == 1:
            value = field(value, rebuilt, 'm_IsActive')
        elif kind == 4:
            for name in ('m_LocalPosition', 'm_LocalScale'):
                value = field(value, rebuilt, name)
        result = result.replace(block, value)
    result_blocks = blocks(result)
    changed = {i for i in old if old[i] != result_blocks[i]}
    assert changed <= {instance_id, 1587741511, 196126984, 233188830, 258975767, 1896838222}, changed
    assert result_blocks.keys() == old.keys(), 'Unrelated scene objects added/removed'
    print('Scene: only basement instance, its wall fragments and its 2 key-door leaves adjusted.')
    return result


def main():
    prefab_path = Path('Assets/prefabs/Scenario/Basement.prefab')
    scene_path = Path('Assets/Scenes/SampleScene.unity')
    before_prefab = (ROOT / '.utmp/Basement-passage-before.prefab').read_text(encoding='utf-8-sig')
    before_scene = (ROOT / '.utmp/Basement-passage-before.unity').read_text(encoding='utf-8-sig')
    prefab = merge_prefab(before_prefab, (PREVIEW / prefab_path).read_text(encoding='utf-8-sig'))
    scene = merge_scene(before_scene, (PREVIEW / 'Assets/Art/Basement/PassageValidation.unity').read_text(encoding='utf-8-sig'))
    destination = ROOT if '--install' in sys.argv else PREVIEW
    if destination == ROOT:
        assert (ROOT / prefab_path).read_text(encoding='utf-8-sig') == before_prefab, 'Prefab changed during validation'
        assert (ROOT / scene_path).read_text(encoding='utf-8-sig') == before_scene, 'Scene changed during validation'
    (destination / prefab_path).write_text(prefab, encoding='utf-8', newline='\n')
    (destination / scene_path).write_text(scene, encoding='utf-8', newline='\n')
    print('Installed passage correction to', destination)


if __name__ == '__main__':
    main()
