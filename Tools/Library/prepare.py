"""Create an isolated Unity art workspace and preserve the incoming project state."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / '.utmp/LibraryPreview'
EVIDENCE = ROOT / 'Tools/Library'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

if __name__ == '__main__':
    if WORK.exists():
        raise SystemExit('Workspace already exists; refusing to overwrite')
    manifest = {p.relative_to(ROOT).as_posix(): digest(p)
                for folder in ('Assets', 'ProjectSettings', 'Packages')
                for p in (ROOT / folder).rglob('*') if p.is_file()}
    (EVIDENCE / 'before-manifest.json').write_text(json.dumps(manifest, indent=2))
    backup = EVIDENCE / 'Before'
    backup.mkdir(exist_ok=True)
    for name in ('Library.prefab', 'Library.prefab.meta'):
        shutil.copy2(ROOT / 'Assets/_Project/Scenes/Library/Prefabs' / name, backup / name)
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        shutil.copytree(ROOT / folder, WORK / folder, dirs_exist_ok=True)
    print('Isolated workspace prepared; original hashes and prefab saved.')
