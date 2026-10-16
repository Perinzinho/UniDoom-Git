import concurrent.futures
import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/LibraryPreview'
ART=Path('Assets/_Project/Scenes/Library')

if __name__=='__main__':
    assert not (OUT/'before-manifest.json').exists(), 'Baseline already recorded'
    paths=[p for folder in ['Assets','Packages','ProjectSettings'] for p in (ROOT/folder).rglob('*') if p.is_file()]
    def digest(p):return p.relative_to(ROOT).as_posix(),hashlib.sha256(p.read_bytes()).hexdigest()
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:manifest=dict(pool.map(digest,paths))
    (OUT/'before-manifest.json').write_text(json.dumps(manifest,indent=2))
    for rel in [ART/'Prefabs/Library.prefab',ART/'Scripts/EncounterRevision/LibraryEncounterDirector.cs',ART/'Navigation/EncounterRevision/LibraryObstacleNavigation.asset']:
        shutil.copy2(ROOT/rel,OUT/rel.name)
        shutil.copy2(ROOT/rel,WORK/rel)
        shutil.copy2(ROOT/(str(rel)+'.meta'),WORK/(str(rel)+'.meta'))
    rel=Path('Assets/_Project/Scenes/Main/SampleScene.unity')
    shutil.copy2(ROOT/rel,WORK/rel)
    print('BASELINE_AND_CLONE_SYNC_PASS',len(manifest))
