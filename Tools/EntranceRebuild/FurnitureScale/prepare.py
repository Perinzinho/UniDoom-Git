import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/EntranceRebuild'
PREFAB='Assets/_Project/Scenes/DirectorRoom/Prefabs/DirectorRoom.prefab'
SCENE='Assets/_Project/Scenes/Main/SampleScene.unity'
PLAYER='Assets/_Project/Global/Prefabs/Player/Player.prefab'

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

if __name__=='__main__':
    assert not (OUT/'before.json').exists(),'Backup already exists'
    paths=[PREFAB,PREFAB+'.meta',SCENE,SCENE+'.meta',PLAYER,PLAYER+'.meta']
    before={p:sha(ROOT/p) for p in paths}
    for p in paths:
        dst=OUT/'Before'/p;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/p,dst)
        shutil.copy2(ROOT/p,WORK/p)
    (OUT/'before.json').write_text(json.dumps(before,indent=2))
    shutil.copy2(OUT/'DirectorFurnitureScale.cs',WORK/'Assets/_TemporaryTools/Editor/DirectorFurnitureScale.cs')
    print('Current office, player and user scene copied to isolated workspace; backups preserved.')
