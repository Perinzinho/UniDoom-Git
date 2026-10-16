"""Install validated persistent campus assets, refusing concurrent edits."""
import concurrent.futures
import hashlib
import json
import os
import re
import shutil
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/EntranceRebuild'
PREFABS=[f'Assets/_Project/Scenes/{r}/Prefabs/{r}.prefab' for r in ['EntranceGarden','EntranceHallway']]
SCENE='Assets/_Project/Scenes/Main/SampleScene.unity'
ANCHOR='Assets/_Project/Global/Scripts/EntranceRebuild/CampusEntranceAnchor.cs'
ATMOSPHERE='Assets/_Project/Global/Scripts/TotalChaos/TCRegionAtmosphere.cs'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def blocks(text):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',text,re.S)}
def key(block):
    return tuple(re.search(r'^  '+field+r':.*$',block,re.M)[0] for field in ['m_CorrespondingSourceObject','m_PrefabInstance'])

def preserve_ids():
    for rel in PREFABS:
        original=blocks((OUT/'Before'/rel).read_text(encoding='utf-8'))
        path=WORK/rel;text=path.read_text(encoding='utf-8');current=blocks(text)
        for fid in original.keys()-current.keys():
            kind,old=original[fid]
            assert kind==4 and 'stripped' in old.splitlines()[0],('Removed original object',rel,fid)
            matches=[i for i,(k,b) in current.items() if k==4 and key(b)==key(old)]
            assert len(matches)==1,(rel,fid,matches)
            new=matches[0];text=text.replace(f'--- !u!4 &{new} stripped',f'--- !u!4 &{fid} stripped')
            text=re.sub(rf'fileID: {new}(?=[,}}])',f'fileID: {fid}',text)
        assert original.keys()<=blocks(text).keys()
        path.write_text(text,encoding='utf-8',newline='\n')
    # TMP creates hidden, nonserialized MeshFilters when a prefab instance enables.
    # Unity can save these as added-component entries with a null addedObject.
    # Remove only newly introduced null entries; keep every preexisting entry.
    original=(OUT/'Before'/SCENE).read_text(encoding='utf-8')
    pattern=r'    - targetCorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+), type: 3\}\n      insertIndex: -1\n      addedObject: \{fileID: 0\}\n'
    existing={(match[1],match[2]) for match in re.finditer(pattern,original)}
    path=WORK/SCENE;text=path.read_text(encoding='utf-8');removed=[]
    def remove_transient(match):
        if (match[1],match[2]) in existing:return match[0]
        removed.append(match[1]);return ''
    text=re.sub(pattern,remove_transient,text)
    # Unity YAML requires an explicit empty array after removing the only entry.
    text=re.sub(r'(    m_AddedComponents:)\n(?=  m_SourcePrefab:)',r'\1 []\n',text)
    path.write_text(text,encoding='utf-8',newline='\n')
    (OUT/'serialization-repair.json').write_text(json.dumps(dict(restored_original_fileids=True,removed_new_transient_null_component_entries=removed),indent=2))
    print('All original prefab fileIDs preserved.')

def install():
    baseline=json.loads((OUT/'before.json').read_text())
    expected=dict(baseline)
    if (OUT/'scene-merge.json').exists():expected[SCENE]=json.loads((OUT/'scene-merge.json').read_text())['expected_project_scene_sha256']
    assert 'CAMPUS_VALIDATION_PASS' in (WORK/'CampusEvidence/validation.txt').read_text()
    assert 'CAMPUS_PLAY_PASS' in (WORK/'CampusEvidence/play-validation.txt').read_text()
    assert 'CAMPUS_FINAL_PASS' in (WORK/'CampusEvidence/final-validation.txt').read_text()
    log=(WORK/'campus-integrity.log').read_text(errors='replace')
    assert 'CAMPUS_FINAL_PASS' in log and 'error CS' not in log and 'Shader error' not in log
    paths={WORK/p for p in PREFABS+[SCENE]}
    paths.update(p for p in (WORK/'Assets/_Project').rglob('*') if p.is_file() and ('/CampusReference/' in p.as_posix() or p.name in ['CampusReference.meta','CampusReference.asset','CampusReference.asset.meta']))
    required=set()
    for room in ['EntranceGarden','EntranceHallway']:
        required.update((WORK/f'CampusEvidence/{room}-dependencies.txt').read_text().splitlines())
    intermediates={p for p in paths if '/Meshes/CampusReference/' in p.as_posix() and p.suffix=='.asset' and p.relative_to(WORK).as_posix() not in required}
    paths.difference_update(intermediates|{Path(str(p)+'.meta') for p in intermediates})
    (OUT/'generation-intermediates.json').write_text(json.dumps([p.relative_to(WORK).as_posix() for p in sorted(intermediates)],indent=2))
    paths.update([WORK/ANCHOR,WORK/(ANCHOR+'.meta')])
    assert sha(ROOT/ATMOSPHERE)==sha(WORK/ATMOSPHERE),'Atmosphere implementation differs from validated copy'
    for p in list(paths):
        for parent in p.parents:
            if parent==WORK/'Assets':break
            meta=Path(str(parent)+'.meta')
            if meta.is_file() and not (ROOT/meta.relative_to(WORK)).exists():paths.add(meta)
    # Verify every overwritten original before touching any project file.
    for p in paths:
        rel=p.relative_to(WORK).as_posix();dest=ROOT/rel
        if rel in baseline:
            assert sha(dest)==expected[rel],('Concurrent project edit',rel)
            if rel not in PREFABS+[SCENE]:assert sha(p)==baseline[rel],('Unrelated existing asset mutation',rel)
        elif dest.exists():assert sha(dest)==sha(p),('Concurrent new asset',rel)
    preserved={}
    for rel in PREFABS:
        old=blocks((OUT/'Before'/rel).read_text(encoding='utf-8'));new=blocks((WORK/rel).read_text(encoding='utf-8'))
        assert old.keys()<=new.keys(),('Original fileID removed',rel)
        assert sha(ROOT/(rel+'.meta'))==sha(WORK/(rel+'.meta'))
        preserved[rel]=len(old)
    installed=[]
    for p in sorted(paths,key=lambda p:(p.relative_to(WORK).as_posix() in PREFABS+[SCENE],p.as_posix())):
        rel=p.relative_to(WORK).as_posix();dest=ROOT/rel
        if rel in baseline:assert sha(dest)==expected[rel],('Concurrent edit immediately before installation',rel)
        if dest.exists() and sha(dest)==sha(p):
            if rel not in baseline:installed.append(dict(path=rel,sha256=sha(dest),bytes=dest.stat().st_size))
            continue
        dest.parent.mkdir(parents=True,exist_ok=True)
        temporary=dest.with_name(dest.name+'.campus-install');temporary.write_bytes(p.read_bytes());os.replace(temporary,dest)
        installed.append(dict(path=rel,sha256=sha(dest),bytes=dest.stat().st_size))
    installed.append(dict(path=ATMOSPHERE,sha256=sha(ROOT/ATMOSPHERE),bytes=(ROOT/ATMOSPHERE).stat().st_size))
    for p in (WORK/'CampusEvidence').iterdir():
        if p.is_file():shutil.copy2(p,OUT/p.name)
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2))
    allowed=set(PREFABS+[SCENE,ATMOSPHERE])
    def check(item):
        rel,digest=item;p=ROOT/rel
        assert p.exists(),('Original removed',rel)
        if rel not in allowed:assert sha(p)==digest,('Unexpected existing file change',rel)
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:list(pool.map(check,baseline.items()))
    result=dict(status='PASS',installed_files=len(installed),original_prefab_fileids_preserved=preserved,all_original_metas_preserved=True,unrelated_existing_assets_unchanged=True,concurrent_scene_changes_preserved=(OUT/'scene-merge.json').exists())
    (OUT/'installed-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))

if __name__=='__main__':
    if '--preserve-ids' in sys.argv:preserve_ids()
    else:install()
