"""Prepare an isolated Unity project and reversible baseline for this revision."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
WORK = ROOT / '.utmp/EntranceRebuild'
ROOMS = ['EntranceGarden', 'EntranceHallway']

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    marker = OUT / 'before.json'
    if marker.exists():
        raise SystemExit('Baseline already exists; do not overwrite it.')
    before = {}
    for base in ['Assets', 'Packages', 'ProjectSettings']:
        for p in (ROOT / base).rglob('*'):
            if not p.is_file():
                continue
            rel = p.relative_to(ROOT).as_posix()
            before[rel] = sha(p)
            dst = WORK / rel
            if not dst.exists() or sha(dst) != before[rel]:
                dst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(p, dst)
    marker.write_text(json.dumps(before, indent=2))
    for room in ROOMS:
        rel = Path(f'Assets/_Project/Scenes/{room}/Prefabs/{room}.prefab')
        dst = OUT / 'Before' / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / rel, dst)
        shutil.copy2(ROOT / (str(rel) + '.meta'), str(dst) + '.meta')
    rel = Path('Assets/_Project/Scenes/Main/SampleScene.unity')
    dst = OUT / 'Before' / rel
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ROOT / rel, dst)
    (WORK / 'CampusEvidence').mkdir(exist_ok=True)
    (WORK / 'Assets/_TemporaryTools/Editor').mkdir(parents=True, exist_ok=True)
    print(f'Preserved {len(before)} files; isolated project synchronized.')

if __name__ == '__main__':
    main()
