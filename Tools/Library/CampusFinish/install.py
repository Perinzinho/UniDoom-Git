import concurrent.futures
import hashlib
import json
import os
import re
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Tools/Library/CampusFinish'
WORK=ROOT/'.utmp/LibraryPreview'
ART=Path('Assets/_Project/Scenes/Library')
PREFAB=ART/'Prefabs/Library.prefab'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(t):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',t,re.S)}

def main():
    before=json.loads((OUT/'before-manifest.json').read_text())
    assert sha(ROOT/PREFAB)==before[PREFAB.as_posix()],'Concurrent prefab edit'
    log=(WORK/'campus-finish.log').read_text(errors='replace')
    for marker in ['CAMPUS_LIBRARY_BUILD_PASS','LIBRARY_DECAY_VERIFY_PASS','CAMPUS_FLICKER_PLAY_PASS','LIBRARY_DECAY_PLAY_PASS']:assert marker in log,marker
    assert 'error CS' not in log and 'Shader error' not in log
    old=blocks((OUT/'Library.prefab').read_text(encoding='utf-8'));new=blocks((WORK/PREFAB).read_text(encoding='utf-8'))
    assert set(old)<=set(new),'Existing prefab fileIDs removed'
    for fid,(kind,block) in old.items():
        assert new[fid][0]==kind
        if kind==4:
            for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
                pattern=rf'^  {field}:.*$'
                assert re.search(pattern,block,re.M)[0]==re.search(pattern,new[fid][1],re.M)[0],(fid,field)
    # Capture hashes immediately before installation so any concurrent Editor saves stay protected.
    def check(item):
        name,digest=item;path=ROOT/name
        return (name,sha(path)) if path.exists() and name!=PREFAB.as_posix() and sha(path)!=digest else None
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:concurrent_changes=dict(x for x in pool.map(check,before.items()) if x)
    installed=[]
    paths=[p for p in (WORK/ART).rglob('*') if p.is_file() and ('/CampusFinish/' in p.as_posix() or p.name=='CampusFinish.meta')]
    # Publish all metas first so the open Editor cannot generate a replacement GUID.
    for path in sorted(paths,key=lambda p:(p.suffix!='.meta',str(p))):
        rel=path.relative_to(WORK);target=ROOT/rel;target.parent.mkdir(parents=True,exist_ok=True)
        if target.exists():assert sha(target)==sha(path),'Unexpected overwrite: '+str(rel)
        else:shutil.copy2(path,target)
        installed.append(dict(path=rel.as_posix(),sha256=sha(target)))
    data=re.sub(r'[ \t]+$','',(WORK/PREFAB).read_text(encoding='utf-8'),flags=re.M).encode('utf-8')
    temporary=ROOT/'.utmp/CampusLibrary.prefab';temporary.write_bytes(data);os.replace(temporary,ROOT/PREFAB)
    installed.append(dict(path=PREFAB.as_posix(),sha256=sha(ROOT/PREFAB)))
    for path in (WORK/'Evidence/Decay').iterdir():
        if path.is_file():shutil.copy2(path,OUT/path.name)
    shutil.copy2(WORK/'campus-finish.log',OUT/'unity-validation.log')
    # Verify the installed assets match the Unity-validated sources and all original files remain.
    for item in installed:
        path=ROOT/item['path'];assert sha(path)==item['sha256']
        if path.suffix!='.meta':assert Path(str(path)+'.meta').is_file()
    changed=[]
    for name,digest in before.items():
        if name==PREFAB.as_posix():continue
        assert (ROOT/name).exists(),'Existing file removed: '+name
        current=sha(ROOT/name)
        if current!=digest:
            assert concurrent_changes.get(name)==current,'Concurrent change during install: '+name
            changed.append(name)
    guids={}
    for path in (ROOT/'Assets').rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',path.read_text(errors='replace'),re.M)
        if match:guids.setdefault(match[1],[]).append(path.relative_to(ROOT).as_posix())
    new_paths={x['path'] for x in installed if x['path'] not in before}
    collisions={g:p for g,p in guids.items() if len(p)>1 and set(p)&new_paths};assert not collisions,collisions
    result=dict(status='PASS',installed_files=len(installed),existing_fileIDs_preserved=len(old),existing_transforms_preserved=True,new_guid_collisions=collisions,concurrent_changes_preserved=changed,main_scene_preserved=sha(ROOT/'Assets/_Project/Scenes/Main/SampleScene.unity')==before['Assets/_Project/Scenes/Main/SampleScene.unity'])
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2));(OUT/'installed-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))

if __name__=='__main__':main()
