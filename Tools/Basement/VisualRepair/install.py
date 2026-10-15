"""Stage/install only reviewed visual fields; preserve earlier optimization work."""
from pathlib import Path
import hashlib
import json
import re
import shutil
import sys

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/BasementVisualRepair'
PREVIEW = ROOT / '.utmp/BasementPreview'
ART = 'Assets/Art/Basement/VisualRepair'
PREFABS = ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab', 'Assets/Art/Basement/BasementFinalSanctum.prefab']
SCENE = 'Assets/Scenes/SampleScene.unity'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def fields(text):
    return {m[1]: m[0] for m in re.finditer(r'^  (\w+):[^\n]*(?:\n(?!  \w+:)[^\n]*)*', text, re.M)}


def merge_prefab(path):
    original = (WORK / 'before' / path).read_text(encoding='utf-8-sig')
    generated = (PREVIEW / path).read_text(encoding='utf-8-sig')
    old, new = blocks(original), blocks(generated)
    assert old.keys() <= new.keys(), 'Original fileID removed'
    output = original[:original.index('--- !u!')]
    changes = []
    for fid, (kind, text) in old.items():
        assert new[fid][0] == kind
        a, b = fields(text), fields(new[fid][1])
        allowed = {4: {'m_Children'}, 23: {'m_Enabled'}, 33: {'m_Mesh'}, 102: {'m_Text'}}.get(kind, set())
        for key in allowed:
            if a.get(key) == b.get(key):
                continue
            assert key in a and key in b
            if key == 'm_Children':
                before = re.findall(r'fileID: (-?\d+)', a[key])
                after = re.findall(r'fileID: (-?\d+)', b[key])
                assert after[:len(before)] == before
                assert all(child not in old for child in after[len(before):])
            elif key == 'm_Enabled':
                assert a[key].strip() == 'm_Enabled: 1' and b[key].strip() == 'm_Enabled: 0'
            elif key == 'm_Text':
                assert not re.sub(r'(?m)^  m_Text:\s*', '', b[key]).strip().strip('"')
            text = text.replace(a[key], b[key], 1)
            changes.append([fid, kind, key])
        output += text
    new_ids = set(new) - set(old)
    for fid in new_ids:
        assert new[fid][0] in {1, 4, 23, 33}, 'Added physics/script/light component'
    output += ''.join(text for fid, (_, text) in new.items() if fid in new_ids)
    staged = blocks(output)
    assert old.keys() <= staged.keys()
    for fid, (kind, text) in old.items():
        if kind in {54, 64, 65, 108, 114, 136, 143, 195, 205, 215, 1001}:
            assert staged[fid][1] == text, 'Original gameplay/physics/light/LOD changed'
    return output, new_ids, {'path': path, 'existing_fileIDs': len(old), 'new_visual_blocks': len(new_ids), 'changes': changes}


def merge_scene(new_prefab_ids):
    original = (WORK / 'before' / SCENE).read_text(encoding='utf-8-sig')
    generated = (PREVIEW / SCENE).read_text(encoding='utf-8-sig')
    old, new = blocks(original), blocks(generated)
    guid = re.search(r'^guid: (\w+)', (ROOT / (PREFABS[0] + '.meta')).read_text(), re.M)[1]
    ids = [fid for fid, (kind, text) in old.items() if kind == 1001 and f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}' in text]
    assert len(ids) == 1
    fid = ids[0]
    pattern = r'(?ms)^    m_Modifications:\n.*?(?=^    m_RemovedComponents:)'
    a, b = re.search(pattern, old[fid][1]), re.search(pattern, new[fid][1])
    assert a and b
    entries = re.findall(r'(?ms)^    - target:.*?(?=^    - target:|\Z)', b[0])
    approved = []
    for entry in entries:
        match = re.search(r'target: {fileID: (-?\d+), guid: (\w+)', entry)
        if match and match[1] in new_prefab_ids and match[2] == guid:
            assert re.search(r'propertyPath: (m_LocalScale\.[xyz]|m_Mesh)\n', entry), entry
            approved.append(entry)
    assert len(approved) >= 10, 'Scene scale/mesh overrides missing'
    block = old[fid][1].replace(a[0], a[0] + ''.join(approved), 1)
    result = original.replace(old[fid][1], block, 1)
    changes = [key for key, value in old.items() if blocks(result)[key] != value]
    assert changes == [fid]
    return result, {'scene_instance': fid, 'new_visual_overrides': len(approved), 'original_scene_blocks_preserved': len(old)}


def require_log(name, markers):
    text = (WORK / name).read_text(encoding='utf-8', errors='replace')
    for marker in markers:
        assert marker in text, name + ': missing ' + marker
    for error in ['error CS', 'Shader error in', 'Exception:', 'Render Graph Execution error']:
        assert error not in text, name + ': ' + error


def main():
    if '--install' not in sys.argv:
        require_log('refine.log', ['BASEMENT_VISUAL_REPAIR_BUILT', 'BASEMENT_VISUAL_REPAIR_VERIFIED', 'Exiting batchmode successfully'])
        report = []
        main_ids = None
        for path in PREFABS:
            text, new_ids, entry = merge_prefab(path)
            if main_ids is None:
                main_ids = new_ids
            (PREVIEW / path).write_text(text, encoding='utf-8', newline='\n')
            staged = WORK / 'staged' / path
            staged.parent.mkdir(parents=True, exist_ok=True)
            staged.write_text(text, encoding='utf-8', newline='\n')
            report.append(entry)
        text, entry = merge_scene(main_ids)
        (PREVIEW / SCENE).write_text(text, encoding='utf-8', newline='\n')
        staged = WORK / 'staged' / SCENE
        staged.parent.mkdir(parents=True, exist_ok=True)
        staged.write_text(text, encoding='utf-8', newline='\n')
        report.append(entry)
        (WORK / 'staging-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        print('Staged minimal prefab fields and scene overrides for new visual objects only.')
        return
    require_log('verify.log', ['BASEMENT_VISUAL_REPAIR_VERIFIED', 'BASEMENT_FINAL_MAP_PLAYER_OK', 'BASEMENT_FINAL_PREFAB_PLAYER_OK', 'BASEMENT_ENCOUNTER_TEST_OK', 'Exiting batchmode successfully'])
    require_log('play.log', ['BASEMENT_SURFACE_PLAYMODE_OK', 'Shutdown.'])
    baseline = json.loads((WORK / 'baseline.json').read_text())
    for path in PREFABS + [SCENE]:
        assert sha(ROOT / path) == baseline[path], 'Concurrent edit: ' + path
        assert sha(PREVIEW / path) == sha(WORK / 'staged' / path), 'Validated asset changed: ' + path
    assert not (ROOT / ART).exists(), 'Keep existing output; do not overwrite it.'
    shutil.copytree(PREVIEW / ART, ROOT / ART)
    shutil.copy2(PREVIEW / (ART + '.meta'), ROOT / (ART + '.meta'))
    for path in PREFABS + [SCENE]:
        (ROOT / path).write_bytes((WORK / 'staged' / path).read_bytes())
    docs = ROOT / 'Tools/Basement/VisualRepair'
    for prefix in ['BeforeRepair', 'VerifiedRepair']:
        for source in (PREVIEW / 'Preview/BasementSurface').glob('VisualRepair-' + prefix + '-*.png'):
            shutil.copy2(source, docs / source.name.replace('VisualRepair-', ''))
    for source, name in [('Preview/VisualRepair/validation.txt', 'validation.txt'),
                         ('Preview/VisualRepair/diagnosis.txt', 'diagnosis.txt'),
                         ('Preview/BasementSurface/runtime-validation.txt', 'runtime-validation.txt')]:
        shutil.copy2(PREVIEW / source, docs / name)
    shutil.copy2(WORK / 'staging-report.json', docs / 'staging-report.json')
    print('Installed reviewed visual assets, three prefabs and scene overrides; previous optimization fields preserved.')


if __name__ == '__main__':
    main()
