import importlib.util, json, os, re, shutil, sys
from pathlib import Path
from prepare import OUT, ROOT, WORK, PREFABS, SCENE, sha

def preserve_ids():
    spec=importlib.util.spec_from_file_location('prior_install',OUT.parent/'NightRevision/install.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    module.OUT=OUT;module.WORK=WORK;module.preserve_ids()

def install():
    assert 'CAMPUS_IMMERSION_VALIDATION_PASS' in (WORK/'ImmersionEvidence/validation.txt').read_text()
    assert 'CAMPUS_NIGHT_PLAY_PASS' in (WORK/'ImmersionEvidence/play-validation.txt').read_text()
    log=(WORK/'immersion-final.log').read_text(errors='replace')
    assert 'CAMPUS_IMMERSION_VALIDATION_PASS' in log and 'error CS' not in log and 'Shader error' not in log
    before=json.loads((OUT/'before.json').read_text())
    paths={WORK/p for p in PREFABS}
    paths.update(p for p in (WORK/'Assets/_Project/Scenes/EntranceHallway').rglob('*') if p.is_file() and ('/ImmersionRevision/' in p.as_posix() or p.name=='ImmersionRevision.meta'))
    dependencies=set((WORK/'ImmersionEvidence/EntranceHallway-dependencies.txt').read_text().splitlines())
    unused={p for p in paths if '/Meshes/' in p.as_posix() and p.suffix=='.asset' and p.relative_to(WORK).as_posix() not in dependencies}
    paths.difference_update(unused|{Path(str(p)+'.meta') for p in unused})
    for path in list(paths):
        for parent in path.parents:
            if parent==WORK/'Assets':break
            meta=Path(str(parent)+'.meta')
            if meta.is_file() and not (ROOT/meta.relative_to(WORK)).exists():paths.add(meta)
    for rel in PREFABS:
        assert sha(ROOT/rel)==before[rel],('Concurrent modification',rel)
        assert sha(ROOT/(rel+'.meta'))==before[rel+'.meta']
        def ids(p):return set(re.findall(r'^--- !u!\d+ &(-?\d+)',p.read_text(),re.M))
        assert ids(ROOT/rel)<=ids(WORK/rel),('Lost original fileIDs',rel)
    for path in paths:
        rel=path.relative_to(WORK).as_posix();dest=ROOT/rel
        if rel not in before:assert not dest.exists() or sha(dest)==sha(path),('Concurrent asset',rel)
    scene=sha(ROOT/SCENE);installed=[]
    for path in sorted(paths,key=lambda p:(p.relative_to(WORK).as_posix() in PREFABS,p.as_posix())):
        rel=path.relative_to(WORK).as_posix();dest=ROOT/rel;dest.parent.mkdir(parents=True,exist_ok=True)
        if not dest.exists() or sha(dest)!=sha(path):
            temp=dest.with_name(dest.name+'.immersion-install');temp.write_bytes(path.read_bytes());os.replace(temp,dest)
        installed.append(dict(path=rel,sha256=sha(dest)))
    assert scene==sha(ROOT/SCENE)
    for p in (WORK/'ImmersionEvidence').iterdir():
        if p.is_file():shutil.copy2(p,OUT/p.name)
    (OUT/'installed.json').write_text(json.dumps(installed,indent=2))
    result=dict(status='PASS',files=len(installed),original_fileids_and_metas_preserved=True,main_scene_preserved=True,scene_sha256=scene)
    (OUT/'installed-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))

if __name__=='__main__':
    if '--preserve-ids' in sys.argv:preserve_ids()
    else:install()
