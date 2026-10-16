"""Stage/install the rebuilt library; protect original shell IDs and unrelated user files."""
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Tools/Library/DecayRevision'
WORK=ROOT/'.utmp/LibraryPreview'
ART=Path('Assets/_Project/Scenes/Library')
PREFAB=ART/'Prefabs/Library.prefab'
GUID='a169c3bc943cf154cad01854206c9e41'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def blocks(text):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',text,re.S)}

def preserve(source):
    factory=blocks((ROOT/'Tools/Library/Before/Library.prefab').read_text(encoding='utf-8'))
    before=blocks((OUT/'Before/Library/Prefabs/Library.prefab').read_text(encoding='utf-8'))
    new=blocks(source)
    assert set(factory).issubset(new),'Original shell object IDs missing'
    removed=set(before)-set(new)
    # Scan every active serialized consumer of the prefab, including recovery scenes.
    result=subprocess.run(['rg','-uuu','-l',GUID,'Assets','ProjectSettings'],cwd=ROOT,text=True,capture_output=True)
    consumers=[]
    for name in result.stdout.splitlines():
        path=Path(name)
        if path.as_posix()==PREFAB.as_posix():continue
        text=(ROOT/path).read_text(errors='replace')
        refs=re.findall(r'\{fileID: (-?\d+), guid: '+GUID+r',',text)
        assert not (set(refs)&removed),'External reference to removed artwork: '+name
        consumers.append(dict(path=path.as_posix(),fileIDs=sorted(set(refs))))
    merged=source[:source.index('--- !u!')];changed=[]
    for fid in factory:
        kind,block=before[fid]
        allowed={4:['m_Children'] if fid=='1420356484721759852' else [],23:['m_Materials']}
        for field in allowed.get(kind,[]):
            pattern=rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
            a=re.search(pattern,block,re.M|re.S);b=re.search(pattern,new[fid][1],re.M|re.S)
            if a and b and a[0]!=b[0]:block=block[:a.start()]+b[0]+block[a.end():];changed.append(dict(fileID=fid,field=field))
        merged+=block
    merged+=''.join(re.sub(r'[ \t]+$','',block,flags=re.M) for fid,(_,block) in new.items() if fid not in factory)
    return merged.encode('utf-8'),dict(original_shell_ids_preserved=len(factory),removed_previous_artwork_ids=len(removed),new_artwork_ids=len(set(new)-set(factory)),external_consumers=consumers,changed_shell_fields=changed)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--install',action='store_true');args=parser.parse_args()
    baseline=json.loads((OUT/'before-manifest.json').read_text())
    assert sha(ROOT/PREFAB)==baseline[PREFAB.as_posix()],'Concurrent edit to library'
    assert sha(ROOT/(PREFAB.as_posix()+'.meta'))==baseline[PREFAB.as_posix()+'.meta']
    log=(WORK/('decay-final.log' if args.install else 'decay-build.log')).read_text(errors='replace')
    assert 'LIBRARY_DECAY_VERIFY_PASS' in log,'Unity verification incomplete'
    if args.install:assert 'LIBRARY_DECAY_PLAY_PASS' in log and (WORK/'Evidence/Decay/play-validation.txt').read_text().startswith('PASS\n')
    else:assert 'Exiting batchmode successfully' in log
    assert 'error CS' not in log and '\nException:' not in log
    source=(WORK/PREFAB).read_text(encoding='utf-8');data,report=preserve(source)
    if not args.install:
        (WORK/PREFAB).write_bytes(data);(OUT/'structure-validation.json').write_text(json.dumps(report,indent=2));print('Staged; original shell IDs and external consumers verified');return
    assert (WORK/PREFAB).read_bytes()==data,'Staged YAML changed after verification'
    dependencies=set((WORK/'Evidence/Decay/dependencies.txt').read_text().splitlines());installed=[]
    for path in (WORK/ART).rglob('*'):
        if not path.is_file():continue
        rel=path.relative_to(WORK);name=rel.as_posix();base=name.removesuffix('.meta')
        if '/DecayRevision/' not in name and not name.endswith('/DecayRevision.meta') and name!=ART.as_posix()+'/Reflections.meta':continue
        if '/Meshes/' in name and base not in dependencies and not name.endswith('/DecayRevision.meta'):continue
        target=ROOT/rel
        if target.exists():
            assert sha(target)==sha(path),'Unexpected overwrite: '+name
            continue
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,target);installed.append(dict(path=name,sha256=sha(target)))
    assert sha(ROOT/PREFAB)==baseline[PREFAB.as_posix()]
    temporary=ROOT/'.utmp/LibraryDecay-install.prefab';temporary.write_bytes(data);os.replace(temporary,ROOT/PREFAB)
    installed.append(dict(path=PREFAB.as_posix(),sha256=sha(ROOT/PREFAB)))
    for path in (WORK/'Evidence/Decay').iterdir():
        if path.is_file():shutil.copy2(path,OUT/path.name)
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2));print('Installed',len(installed),'files; other assets were preserved')

if __name__=='__main__':main()
