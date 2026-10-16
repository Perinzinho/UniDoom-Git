"""Preserve the current library and synchronize the existing isolated art workspace."""
import concurrent.futures
import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Tools/Library/DecayRevision'
WORK=ROOT/'.utmp/LibraryPreview'
ART=Path('Assets/_Project/Scenes/Library')

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def record(path):return path.relative_to(ROOT).as_posix(),sha(path)
def copy_changed(source,target):
    source,target=Path(source),Path(target)
    if target.exists() and sha(source)==sha(target):return str(target)
    return shutil.copy2(source,target)

if __name__=='__main__':
    assert not (OUT/'before-manifest.json').exists(),'Revision already prepared'
    paths=[p for folder in ('Assets','Packages','ProjectSettings') for p in (ROOT/folder).rglob('*') if p.is_file()]
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:manifest=dict(pool.map(record,paths))
    (OUT/'before-manifest.json').write_text(json.dumps(manifest,indent=2))
    shutil.copytree(ROOT/ART,OUT/'Before/Library')
    for name in ('LibraryBuilder.cs','LibraryVerifier.cs','LibraryPlayCheck.cs','README.md'):
        shutil.copy2(ROOT/'Tools/Library'/name,OUT/'Before'/name)
    for folder in ('Assets','Packages','ProjectSettings'):
        shutil.copytree(ROOT/folder,WORK/folder,dirs_exist_ok=True,copy_function=copy_changed)
    print('Current library backed up; isolated project synchronized.',flush=True)
