"""Synchronize the isolated preview and retain the three current prefab originals."""
from pathlib import Path
import hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[3]
PREVIEW=ROOT/'.utmp/BasementPreview'
WORK=ROOT/'.utmp/BasementRitualExpansion'
TARGETS=['Assets/prefabs/Scenario/Basement.prefab','Assets/Art/Basement/BasementFinalArena.prefab','Assets/Art/Basement/BasementFinalSanctum.prefab']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
if __name__=='__main__':
    WORK.mkdir(parents=True,exist_ok=True)
    assert not (WORK/'baseline.json').exists(),'Existing snapshot retained'
    baseline={p:sha(ROOT/p) for p in TARGETS}
    for rel in TARGETS:
        dst=WORK/'before'/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/rel,dst)
    (WORK/'baseline.json').write_text(json.dumps(baseline,indent=2))
    copied=0
    for folder in ['Assets','Packages','ProjectSettings']:
        for p in (ROOT/folder).rglob('*'):
            if not p.is_file():continue
            dst=PREVIEW/p.relative_to(ROOT)
            if dst.exists() and p.stat().st_size==dst.stat().st_size and sha(p)==sha(dst):continue
            dst.parent.mkdir(parents=True,exist_ok=True)
            with dst.open('r+b' if dst.exists() else 'wb') as out:out.write(p.read_bytes());out.truncate()
            copied+=1
    print('Isolated preview synchronized:',copied,'files; original prefabs snapshotted')
