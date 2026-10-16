"""Restore existing files changed by this revision, retaining new assets."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
TARGETS=[f'Assets/_Project/Scenes/{room}/Prefabs/{room}.prefab' for room in ['EntranceGarden','EntranceHallway']]
TARGETS+=['Assets/_Project/Scenes/Main/SampleScene.unity','Assets/_Project/Global/Scripts/TotalChaos/TCRegionAtmosphere.cs']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');parser.add_argument('--check',action='store_true');args=parser.parse_args()
    before=json.loads((OUT/'before.json').read_text());installed={item['path']:item['sha256'] for item in json.loads((OUT/'installed.json').read_text())}
    backups={rel:OUT/'Before'/rel for rel in TARGETS}
    scene='Assets/_Project/Scenes/Main/SampleScene.unity'
    if (OUT/'scene-merge.json').exists():
        before[scene]=json.loads((OUT/'scene-merge.json').read_text())['expected_project_scene_sha256']
        backups[scene]=OUT/'ConcurrentBeforeInstall'/scene
    for rel in TARGETS:
        assert sha(backups[rel])==before[rel],('Damaged backup',rel)
        assert sha(ROOT/rel)==installed[rel],('Later modification; selective review required',rel)
    if args.apply:
        lock=ROOT/'Temp/UnityLockfile'
        if lock.exists():
            try:
                with lock.open('r+b'):pass
            except OSError:raise SystemExit('Close the Unity Editor before restoring existing assets.')
        for rel in TARGETS:shutil.copy2(backups[rel],ROOT/rel)
        print('Original prefabs, scene and atmosphere script restored. New assets retained; nothing deleted.')
    else:print('Backups and current installation verified. No files changed.')
