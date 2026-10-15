"""Snapshot and sync the existing isolated Unity project; never share its Library."""
from pathlib import Path
import hashlib, json, shutil

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
WORK = ROOT / '.utmp/BasementRitual'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

if __name__ == '__main__':
    WORK.mkdir(parents=True, exist_ok=True)
    assert not (WORK/'baseline.json').exists(), 'Do not replace the original snapshot'
    baseline = {}
    for folder in ['Assets', 'Packages', 'ProjectSettings']:
        for p in (ROOT/folder).rglob('*'):
            if not p.is_file(): continue
            rel = p.relative_to(ROOT)
            baseline[rel.as_posix()] = sha(p)
            dst = PREVIEW/rel
            if not dst.exists() or sha(dst) != baseline[rel.as_posix()]:
                dst.parent.mkdir(parents=True, exist_ok=True)
                # Windows rejects CREATE_ALWAYS for existing hidden Unity .meta files.
                with dst.open('r+b' if dst.exists() else 'wb') as out:
                    out.write(p.read_bytes())
                    out.truncate()
    for rel in ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalSanctum.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab']:
        dst = WORK/'before'/rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT/rel, dst)
    (WORK/'baseline.json').write_text(json.dumps(baseline, indent=2))
    print('Snapshot and isolated project synchronized:',len(baseline),'files')
