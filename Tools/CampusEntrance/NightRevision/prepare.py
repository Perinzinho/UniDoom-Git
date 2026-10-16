import hashlib
import json
import shutil
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
WORK=ROOT/'.utmp/EntranceRebuild'
TARGETS=[f'Assets/_Project/Scenes/{r}/Prefabs/{r}.prefab' for r in ['EntranceGarden','EntranceHallway']]
TARGETS+=['Assets/_Project/Global/Scripts/EntranceRebuild/CampusEntranceAnchor.cs']
SCENE='Assets/_Project/Scenes/Main/SampleScene.unity'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
if __name__=='__main__':
    assert not (OUT/'before.json').exists(),'Keep the existing baseline'
    before={}
    for rel in TARGETS+[SCENE]:
        for name in [rel,rel+'.meta']:
            src=ROOT/name;before[name]=sha(src)
            dst=OUT/'Before'/name;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
            dst=WORK/name;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
    for rel in ['Assets/_Project/Global/Scripts/TotalChaos/TCRegionAtmosphere.cs']:
        shutil.copy2(ROOT/rel,WORK/rel)
    (OUT/'before.json').write_text(json.dumps(before,indent=2))
    (WORK/'NightEvidence').mkdir(exist_ok=True)
    print('Current prefabs, anchor and scene backed up; isolated workspace synchronized.')
