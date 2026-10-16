"""Install only validated night revision assets; keep current GUIDs and fileIDs."""
import hashlib
import json
import os
import re
import shutil
import sys
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
WORK=ROOT/'.utmp/EntranceRebuild'
PREFABS=[f'Assets/_Project/Scenes/{r}/Prefabs/{r}.prefab' for r in ['EntranceGarden','EntranceHallway']]
ANCHOR='Assets/_Project/Global/Scripts/EntranceRebuild/CampusEntranceAnchor.cs'
SCENE='Assets/_Project/Scenes/Main/SampleScene.unity'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def blocks(text):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',text,re.S)}
def source_key(block):return tuple(re.search(r'^  '+field+r':.*$',block,re.M)[0] for field in ['m_CorrespondingSourceObject','m_PrefabInstance'])
def preserve_ids():
    for rel in PREFABS:
        old=blocks((OUT/'Before'/rel).read_text(encoding='utf-8'));path=WORK/rel;text=path.read_text(encoding='utf-8');new=blocks(text)
        for fid in old.keys()-new.keys():
            kind,block=old[fid]
            assert kind==4 and 'stripped' in block.splitlines()[0],('Removed original object',rel,fid)
            matches=[i for i,(k,b) in new.items() if k==kind and source_key(b)==source_key(block)]
            assert len(matches)==1,(rel,fid,matches)
            other=matches[0];text=text.replace(f'--- !u!4 &{other} stripped',f'--- !u!4 &{fid} stripped')
            text=re.sub(rf'fileID: {other}(?=[,}}])',f'fileID: {fid}',text)
        assert old.keys()<=blocks(text).keys()
        text=re.sub(r'[ \t]+$','',text,flags=re.M)
        path.write_text(text,encoding='utf-8',newline='\n')
    print('Original prefab IDs retained; serializer whitespace normalized.')
def install():
    before=json.loads((OUT/'before.json').read_text())
    assert 'CAMPUS_NIGHT_VALIDATION_PASS' in (WORK/'NightEvidence/validation.txt').read_text()
    assert 'CAMPUS_NIGHT_PLAY_PASS' in (WORK/'NightEvidence/play-validation.txt').read_text()
    log=(WORK/'night-final.log').read_text(errors='replace')
    assert 'CAMPUS_NIGHT_VALIDATION_PASS' in log and 'error CS' not in log and 'Shader error' not in log
    paths={WORK/p for p in PREFABS+[ANCHOR]}
    paths.update(p for p in (WORK/'Assets/_Project').rglob('*') if p.is_file() and ('/CampusNight/' in p.as_posix() or p.name in ['CampusNight.meta','CampusNight.asset','CampusNight.asset.meta']))
    required=set()
    for room in ['EntranceGarden','EntranceHallway']:required.update((WORK/f'NightEvidence/{room}-dependencies.txt').read_text().splitlines())
    unused={p for p in paths if '/Meshes/CampusNight/' in p.as_posix() and p.suffix=='.asset' and p.relative_to(WORK).as_posix() not in required}
    paths.difference_update(unused|{Path(str(p)+'.meta') for p in unused})
    for p in list(paths):
        for parent in p.parents:
            if parent==WORK/'Assets':break
            meta=Path(str(parent)+'.meta')
            if meta.is_file() and not (ROOT/meta.relative_to(WORK)).exists():paths.add(meta)
    for rel in PREFABS+[ANCHOR]:
        current=sha(ROOT/rel)
        assert current==before[rel] or rel==ANCHOR and current==sha(WORK/rel),('Concurrent asset modification',rel)
        assert sha(ROOT/(rel+'.meta'))==before[rel+'.meta']
    preserved={}
    for rel in PREFABS:
        old=blocks((OUT/'Before'/rel).read_text(encoding='utf-8'));new=blocks((WORK/rel).read_text(encoding='utf-8'))
        assert old.keys()<=new.keys()
        for fid in old:assert old[fid][0]==new[fid][0]
        preserved[rel]=len(old)
    for p in paths:
        rel=p.relative_to(WORK).as_posix();dest=ROOT/rel
        if rel not in before and dest.exists():assert sha(dest)==sha(p),('Concurrent new asset',rel)
    scene_before=sha(ROOT/SCENE);installed=[]
    for p in sorted(paths,key=lambda p:(p.relative_to(WORK).as_posix() in PREFABS,p.as_posix())):
        rel=p.relative_to(WORK).as_posix();dest=ROOT/rel
        if dest.exists() and rel in PREFABS:assert sha(dest)==before[rel]
        if not dest.exists() or sha(dest)!=sha(p):
            dest.parent.mkdir(parents=True,exist_ok=True);temporary=dest.with_name(dest.name+'.night-install');temporary.write_bytes(p.read_bytes());os.replace(temporary,dest)
        installed.append(dict(path=rel,sha256=sha(dest),bytes=dest.stat().st_size))
    scene_after=sha(ROOT/SCENE)
    for p in (WORK/'NightEvidence').iterdir():
        if p.is_file():shutil.copy2(p,OUT/p.name)
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2))
    result=dict(status='PASS',installed_files=len(installed),original_fileids_preserved=preserved,original_metas_preserved=True,scene_unchanged_by_installation=True,scene_sha256=scene_after,concurrent_scene_save_preserved=scene_before!=scene_after)
    (OUT/'installed-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
if __name__=='__main__':
    if '--preserve-ids' in sys.argv:preserve_ids()
    else:install()
