"""Merge the validated entrance scene into concurrent, disjoint user edits."""
import importlib.util
import json
import re
import shutil
from pathlib import Path

OUT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('campus_install',OUT/'install.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
baseline=m.blocks((OUT/'Before'/m.SCENE).read_text(encoding='utf-8'))
source=OUT/'EntranceOnlyScene.unity'
if not source.exists():shutil.copy2(m.WORK/m.SCENE,source)
ours=m.blocks(source.read_text(encoding='utf-8'))
current_path=m.ROOT/m.SCENE;current_text=current_path.read_text(encoding='utf-8');current=m.blocks(current_text)
backup=OUT/'ConcurrentBeforeInstall'/m.SCENE
backup.parent.mkdir(parents=True,exist_ok=True)
if backup.exists():assert m.sha(backup)==m.sha(current_path),'Scene changed again; preserve a new backup before merging'
else:shutil.copy2(current_path,backup)
our_changes={key for key in baseline.keys()&ours.keys() if baseline[key]!=ours[key]}
user_changes={key for key in baseline.keys()&current.keys() if baseline[key]!=current[key]}
conflicts=our_changes&user_changes
for key in conflicts:
    # Our only edit in the overlapping hallway block removes an obsolete, null TMP
    # component placeholder. Keep the user's complete block, including that entry.
    assert key=='590149246',('Review overlapping scene edits',key)
    old=baseline[key][1];new=ours[key][1]
    cleaned=re.sub(r'    m_AddedComponents:\n    - targetCorrespondingSourceObject: \{fileID: 3549532400739203675, guid: 5a557056d2e00194b849f81567ee2771, type: 3\}\n      insertIndex: -1\n      addedObject: \{fileID: 0\}\n','    m_AddedComponents: []\n',old)
    assert new==cleaned,'Unexpected entrance edit in overlapping block'
    our_changes.remove(key)
assert not ((baseline.keys()-current.keys())&our_changes),'User removed an object modified by entrance revision'
def replacement(match):
    key=match[2]
    return ours[key][1] if key in our_changes else match[0]
merged=re.sub(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',replacement,current_text,flags=re.S)
after=m.blocks(merged)
assert current.keys()<=after.keys()
for key in current.keys()-our_changes:assert after[key]==current[key],('Concurrent block not preserved',key)
(m.WORK/m.SCENE).write_text(merged,encoding='utf-8',newline='\n')
result=dict(expected_project_scene_sha256=m.sha(current_path),concurrent_changed_blocks=sorted(user_changes),concurrent_removed_blocks=sorted(baseline.keys()-current.keys()),entrance_applied_blocks=sorted(our_changes),overlapping_block_preserved='590149246',all_concurrent_scene_blocks_preserved=True)
(OUT/'scene-merge.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
