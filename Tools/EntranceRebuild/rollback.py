import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
PREFABS=[f'Assets/_Project/Scenes/{r}/Prefabs/{r}.prefab' for r in ['EntranceGarden','EntranceHallway','DirectorRoom']]

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');parser.add_argument('--check',action='store_true');args=parser.parse_args()
    installed={x['path']:x['sha256'] for x in json.loads((OUT/'installed.json').read_text())}
    before=json.loads((OUT/'before-manifest.json').read_text())
    for rel in PREFABS:
        assert digest(ROOT/rel)==installed[rel],'Prefab changed after installation: '+rel
        assert digest(OUT/'Before'/rel)==before[rel],'Damaged backup: '+rel
        assert digest(ROOT/(rel+'.meta'))==before[rel+'.meta'],'Changed GUID: '+rel
    if args.apply:
        import msvcrt
        lock=ROOT/'Temp/UnityLockfile'
        if lock.exists():
            with lock.open('r+b') as handle:
                try:msvcrt.locking(handle.fileno(),msvcrt.LK_NBLCK,1)
                except OSError:raise SystemExit('Close the Unity Editor before restoring prefabs.')
                msvcrt.locking(handle.fileno(),msvcrt.LK_UNLCK,1)
        for rel in PREFABS:(ROOT/rel).write_bytes((OUT/'Before'/rel).read_bytes())
        print('Three original prefabs restored. New assets retained; no files deleted.')
    else:print('PASS: original backups and installed prefabs verified; no changes made.')
