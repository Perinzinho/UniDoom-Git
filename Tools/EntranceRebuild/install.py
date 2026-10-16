"""Install only validated rebuilt prefabs and their new assets; keep all existing GUIDs."""
import concurrent.futures
import hashlib
import json
import os
import re
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/EntranceRebuild'
ROOMS=['EntranceGarden','EntranceHallway','DirectorRoom']
PREFABS=[f'Assets/_Project/Scenes/{r}/Prefabs/{r}.prefab' for r in ROOMS]

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(t):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',t,re.S)}

def main():
    before=json.loads((OUT/'before-manifest.json').read_text())
    log=(WORK/'final.log').read_text(errors='replace')
    assert 'ENTRANCE_REBUILD_PASS' in (WORK/'generation.log').read_text(errors='replace')
    for marker in ['ENTRANCE_FINAL_PASS','ENTRANCE_VERIFY_PASS','ENTRANCE_MAP_PASS']:
        assert marker in log,marker
    assert 'error CS' not in log and 'Shader error' not in log
    for f in ['validation.txt','map-validation.txt','play-validation.txt']:
        assert (WORK/'Evidence'/f).is_file(),f
    assert 'ENTRANCE_PLAY_PASS' in (WORK/'Evidence/play-validation.txt').read_text()
    fileids={}
    for rel in PREFABS:
        assert sha(ROOT/rel)==before[rel],'Concurrent prefab edit: '+rel
        assert sha(WORK/(rel+'.meta'))==before[rel+'.meta'],'Changed prefab GUID'
        old=blocks((ROOT/rel).read_text(encoding='utf-8'))
        new=blocks((WORK/rel).read_text(encoding='utf-8'))
        assert old.keys()<=new.keys(),'Removed original fileIDs: '+rel
        for fid,(kind,block) in old.items():
            assert new[fid][0]==kind
            if kind==4 and 'stripped' not in block.splitlines()[0]:
                for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
                    pattern=rf'^  {field}:.*$'
                    assert re.search(pattern,block,re.M)[0]==re.search(pattern,new[fid][1],re.M)[0],(rel,fid,field)
        fileids[rel]=len(old)
    installed=[]
    paths=[WORK/p for p in PREFABS]
    paths += [p for p in (WORK/'Assets/_Project').rglob('*') if p.is_file() and ('/EntranceRebuild/' in p.relative_to(WORK).as_posix() or p.name=='EntranceRebuild.meta' or p.name=='EntranceRebuild.asset' or p.name=='EntranceRebuild.asset.meta')]
    # Include metas for newly needed parent folders, without replacing existing folder GUIDs.
    for p in list(paths):
        for parent in p.parents:
            if parent==WORK/'Assets':break
            meta=Path(str(parent)+'.meta')
            if meta.is_file() and not (ROOT/meta.relative_to(WORK)).exists():paths.append(meta)
    paths=sorted(set(paths),key=lambda p:(2 if p.relative_to(WORK).as_posix() in PREFABS else 0 if p.name.endswith('.meta') else 1,p.as_posix()))
    for p in paths:
        rel=p.relative_to(WORK).as_posix()
        if rel in before and rel not in PREFABS:
            assert sha(p)==before[rel],'Attempt to change unrelated existing asset '+rel
            assert sha(ROOT/rel)==before[rel],'Refusing to overwrite concurrent existing asset '+rel
        if (ROOT/rel).exists() and rel not in before:
            assert sha(ROOT/rel)==sha(p),'Concurrent new asset '+rel
    def changed(item):
        name,digest=item;p=ROOT/name
        assert p.is_file(),'Existing file removed: '+name
        current=sha(p)
        return (name,current) if current!=digest else None
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        concurrent_changes=dict(p for p in pool.map(changed,before.items()) if p)
    for p in paths:
        rel=p.relative_to(WORK).as_posix();target=ROOT/rel
        if target.exists() and sha(target)==sha(p):continue
        target.parent.mkdir(parents=True,exist_ok=True)
        data=p.read_bytes()
        if rel in PREFABS:data=re.sub(r'[ \t]+$','',data.decode('utf-8'),flags=re.M).encode('utf-8')
        temporary=target.with_name(target.name+'.entrance-install');temporary.write_bytes(data);os.replace(temporary,target)
        installed.append(dict(path=rel,sha256=sha(target),bytes=len(data)))
    for p in (WORK/'Evidence').iterdir():
        if p.is_file():shutil.copy2(p,OUT/p.name)
    shutil.copy2(WORK/'final.log',OUT/'unity-validation.log')
    shutil.copy2(WORK/'generation.log',OUT/'unity-generation.log')
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        final_changes=dict(p for p in pool.map(changed,before.items()) if p)
    assert set(final_changes)<=set(PREFABS)|set(concurrent_changes)
    for p in before:
        if p.endswith('.meta'):assert sha(ROOT/p)==before[p],'Original meta changed: '+p
    result=dict(status='PASS',installed_files=len(installed),original_fileids_preserved=fileids,all_original_metas_preserved=True,
                original_transforms_preserved=True,main_scene_unchanged=sha(ROOT/'Assets/_Project/Scenes/Main/SampleScene.unity')==before['Assets/_Project/Scenes/Main/SampleScene.unity'],
                concurrent_changes_preserved=list(concurrent_changes))
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2));(OUT/'installed-validation.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))

if __name__=='__main__':main()
