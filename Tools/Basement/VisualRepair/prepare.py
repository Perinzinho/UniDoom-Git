"""Snapshot the current working assets and synchronize the existing isolated preview."""
import hashlib
import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
WORK = ROOT / '.utmp/BasementVisualRepair'
TARGETS = ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab', 'Assets/Art/Basement/BasementFinalSanctum.prefab']


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert not (WORK / 'baseline.json').exists(), 'Keep the existing baseline; do not overwrite it.'
    assert (PREVIEW / 'ProjectSettings/ProjectVersion.txt').exists()
    WORK.mkdir(parents=True, exist_ok=True)
    paths = set(TARGETS + ['Assets/Scenes/SampleScene.unity'])
    for folder in ['Assets/Art/Basement', 'Assets/Art/SkillAtmosphere', 'Assets/TotalChaos']:
        paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT / folder).rglob('*') if p.is_file())
    paths.update(p for p in subprocess.check_output(['git', 'diff', '--name-only', '-z'], cwd=ROOT).decode().split('\0') if p.startswith('Assets/'))
    baseline = {}
    copied = 0
    for relative in sorted(paths):
        source = ROOT / relative
        digest = sha(source)
        baseline[relative] = digest
        snapshot = WORK / 'before' / relative
        snapshot.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, snapshot)
        destination = PREVIEW / relative
        if destination.exists() and digest == sha(destination):
            continue
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(source.read_bytes())
        copied += 1
    (WORK / 'baseline.json').write_text(json.dumps(baseline, indent=2), encoding='utf-8')
    print(f'Snapshotted {len(baseline)} assets; synchronized {copied} preview files.')


if __name__ == '__main__':
    main()
