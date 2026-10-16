import concurrent.futures
import hashlib
import json
import os
import re
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/LibraryPreview'
ART=Path('Assets/_Project/Scenes/Library')
PREFAB=ART/'Prefabs/Library.prefab'
CHANGES=[ART/'Scripts/EncounterRevision/LibraryEncounterDirector.cs',
         ART/'Scripts/EncounterRevision/LibraryZombieNavigator.cs',
         ART/'Navigation/EncounterRevision/LibraryObstacleNavigation.asset',PREFAB]

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(t):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',t,re.S)}

def main():
    before=json.loads((OUT/'before-manifest.json').read_text())
    for path in CHANGES:
        assert sha(ROOT/path)==before[path.as_posix()], 'Concurrent edit: '+str(path)
        assert sha(WORK/(str(path)+'.meta'))==before[path.as_posix()+'.meta'],'Changed GUID/importer: '+str(path)
    log=(WORK/'pressure-build.log').read_text(errors='replace')
    for marker in ['BUILD','VERIFY','MAP','PLAY']:assert 'LIBRARY_PRESSURE_'+marker+'_PASS' in log,marker
    assert 'error CS' not in log and 'Shader error' not in log
    for name in ['validation.txt','play-validation.txt']:assert (WORK/'Evidence/Pressure'/name).read_text().startswith('PASS')
    for name in ['LibraryEncounterDirector.cs','LibraryZombieNavigator.cs']:
        assert sha(OUT/'Runtime'/name)==sha(WORK/ART/'Scripts/EncounterRevision'/name)
    old=blocks((OUT/'Library.prefab').read_text(encoding='utf-8'))
    new=blocks((WORK/PREFAB).read_text(encoding='utf-8'))
    assert old.keys()<=new.keys(),'Removed original fileIDs'
    original=blocks((ROOT/'Tools/Library/Before/Library.prefab').read_text(encoding='utf-8'))
    for fid,(kind,block) in old.items():
        assert new[fid][0]==kind
        if kind==4 and fid in original:
            for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
                pattern=rf'^  {field}:.*$'
                assert re.search(pattern,block,re.M)[0]==re.search(pattern,new[fid][1],re.M)[0],(fid,field)
    allowed={p.as_posix() for p in CHANGES}
    def check(item):
        name,digest=item;path=ROOT/name
        assert path.is_file(),'Existing file removed: '+name
        current=sha(path)
        return (name,current) if name not in allowed and current!=digest else None
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:concurrent_changes=dict(x for x in pool.map(check,before.items()) if x)
    installed=[]
    for rel in CHANGES:
        data=(WORK/rel).read_bytes()
        if rel==PREFAB:data=re.sub(r'[ \t]+$','',data.decode('utf-8'),flags=re.M).encode('utf-8')
        temporary=ROOT/'.utmp'/('Pressure-'+rel.name);temporary.write_bytes(data);os.replace(temporary,ROOT/rel)
        installed.append(dict(path=rel.as_posix(),sha256=sha(ROOT/rel)))
    for path in (WORK/'Evidence/Pressure').iterdir():
        if path.is_file():shutil.copy2(path,OUT/path.name)
    shutil.copy2(WORK/'pressure-build.log',OUT/'unity-validation.log')
    changed=[]
    for name,digest in before.items():
        assert (ROOT/name).is_file(),'Existing file removed: '+name
        current=sha(ROOT/name)
        if name not in allowed and current!=digest:
            assert concurrent_changes.get(name)==current,'Unexpected change: '+name
            changed.append(name)
    for item in installed:assert sha(ROOT/item['path'])==item['sha256']
    result=dict(status='PASS',installed_files=len(installed),existing_fileIDs_preserved=len(old),original_shell_transforms_preserved=True,
                all_existing_metas_preserved=True,unrelated_changes_preserved=changed,
                main_scene_unchanged=sha(ROOT/'Assets/_Project/Scenes/Main/SampleScene.unity')==before['Assets/_Project/Scenes/Main/SampleScene.unity'],
                validation='Unity compilation, prefab and actual map inspection, capsule clearance, sixteen spawns, navigation, Key02 pickup, blackout/restoration, traversal with dead bodies retained.')
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2));(OUT/'installed-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))

if __name__=='__main__':main()
