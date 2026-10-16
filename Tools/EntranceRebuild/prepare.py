import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / '.utmp/EntranceRebuild'
OUT = Path(__file__).resolve().parent

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

if __name__ == '__main__':
    if WORK.exists():raise SystemExit('Workspace already exists; preserve its artifacts and choose a fresh isolated workspace before regenerating.')
    WORK.mkdir(parents=True, exist_ok=True)
    before = {p.relative_to(ROOT).as_posix(): sha(p) for base in ['Assets', 'Packages', 'ProjectSettings'] for p in (ROOT/base).rglob('*') if p.is_file()}
    (OUT/'before-manifest.json').write_text(json.dumps(before, indent=2))
    for base in ['Assets', 'Packages', 'ProjectSettings']:
        shutil.copytree(ROOT/base, WORK/base, dirs_exist_ok=True)
    (WORK/'Assets/_TemporaryTools/Editor').mkdir(parents=True, exist_ok=True)
    for room in ['EntranceGarden', 'EntranceHallway', 'DirectorRoom']:
        p=Path(f'Assets/_Project/Scenes/{room}/Prefabs/{room}.prefab')
        dest=OUT/'Before'/p
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT/p,dest)
        shutil.copy2(ROOT/(str(p)+'.meta'),str(dest)+'.meta')
    print('Isolated project ready; original hashes and prefabs saved.',flush=True)
