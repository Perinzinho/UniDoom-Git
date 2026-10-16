import hashlib, json, shutil
from pathlib import Path
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
WORK=ROOT/'.utmp/EntranceRebuild'
PREFABS=[f'Assets/_Project/Scenes/{s}/Prefabs/{s}.prefab' for s in ['EntranceGarden','EntranceHallway']]
SCENE='Assets/_Project/Scenes/Main/SampleScene.unity'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
if __name__=='__main__':
    assert not (OUT/'before.json').exists(), 'Baseline already saved'
    paths=PREFABS+[SCENE,'Assets/_Project/Global/Scripts/EntranceRebuild/CampusEntranceAnchor.cs','Assets/_Project/Global/Scripts/TotalChaos/TCRegionAtmosphere.cs']
    paths += [str(p.relative_to(ROOT)).replace('\\','/') for p in (ROOT/'Assets/_Project').rglob('*') if p.is_file() and ('/CampusNight/' in p.as_posix() or p.name in ['CampusNight.meta','CampusNight.asset','CampusNight.asset.meta'])]
    before={}
    for rel in sorted(set(paths+[r+'.meta' for r in paths if (ROOT/(r+'.meta')).is_file()])):
        source=ROOT/rel; before[rel]=sha(source)
        for destination in [OUT/'Before'/rel,WORK/rel]:
            destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,destination)
    (OUT/'before.json').write_text(json.dumps(before,indent=2))
    (WORK/'ImmersionEvidence').mkdir(exist_ok=True)
    print('Current assets preserved and isolated project synchronized.')
