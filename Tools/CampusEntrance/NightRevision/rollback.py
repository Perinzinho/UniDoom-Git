import hashlib
import json
import os
import sys
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
TARGETS=[f'Assets/_Project/Scenes/{r}/Prefabs/{r}.prefab' for r in ['EntranceGarden','EntranceHallway']]+['Assets/_Project/Global/Scripts/EntranceRebuild/CampusEntranceAnchor.cs']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
records={r['path']:r['sha256'] for r in json.loads((OUT/'installed.json').read_text())}
before=json.loads((OUT/'before.json').read_text())
for rel in TARGETS:
    assert sha(OUT/'Before'/rel)==before[rel],('Invalid backup',rel)
    assert sha(ROOT/rel)==records[rel],('Later edit requires selective review',rel)
if '--apply' in sys.argv:
    lock=ROOT/'Temp/UnityLockfile'
    if lock.exists():
        try:
            with lock.open('r+b'):pass
        except OSError:raise SystemExit('Close Unity before restoring the previous revision.')
    for rel in TARGETS:
        path=ROOT/rel;temporary=path.with_name(path.name+'.night-rollback');temporary.write_bytes((OUT/'Before'/rel).read_bytes());os.replace(temporary,path)
    print('Previous prefabs and anchor restored; new assets preserved, nothing deleted.')
else:print('Revision backups and installed files verified; no files modified.')
