"""Undo only the 16/10 follow-up. --check is read-only; --apply requires Unity closed."""
import argparse
from pathlib import Path
import re
import shutil
import sys

sys.dont_write_bytecode = True
from continue_organization import ROOT, OUT, load, sha, require_closed


def check():
    failures = [name for name, expected in load('state-after.json')['files'].items()
                if not (ROOT / name).is_file() or sha(ROOT / name) != expected]
    expected_assets = {name for name in load('state-after.json')['files'] if name.startswith('Assets/')}
    current = {p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets').rglob('*') if p.is_file()}
    failures += sorted(current - expected_assets)
    for edit in load('edit-journal.json')['edits']:
        backup = OUT / 'Backups' / edit['path']
        if not backup.is_file() or sha(backup) != edit['before_sha256']:
            failures.append('Backup missing/modified: ' + edit['path'])
    if failures:
        raise RuntimeError('Refusing to overwrite later changes:\n' + '\n'.join(failures[:50]))
    print('PASS: final files and original-byte backups match the recorded state.')


def pair(source, destination):
    src, dst = ROOT / source, ROOT / destination
    assert src.resolve().is_relative_to(ROOT) and dst.resolve().is_relative_to(ROOT)
    assert src.exists() and not dst.exists() and not Path(str(dst) + '.meta').exists()
    assert Path(str(src) + '.meta').is_file()
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(src), str(dst))
    try:
        shutil.move(str(src) + '.meta', str(dst) + '.meta')
    except Exception:
        shutil.move(str(dst), str(src))
        raise


def apply():
    require_closed()
    check()
    # Recover the old empty folder metas before returning their files.
    for folder in reversed(load('empty-folders.json')['folders']):
        pair(folder['destination'], folder['source'])
    for move in reversed(load('move-journal.json')['moves']):
        pair(move['destination'], move['source'])
    for edit in load('edit-journal.json')['edits']:
        shutil.copyfile(OUT / 'Backups' / edit['path'], ROOT / edit['path'])
    # Preserve only new empty destination folders outside Assets; do not delete them.
    created = {e['guid'] for e in load('created-folders.json')['folders']}
    for folder in sorted((ROOT / 'Assets/_Project/Global').rglob('*'), key=lambda p: -len(p.parts)):
        meta = Path(str(folder) + '.meta')
        if not folder.is_dir() or any(folder.iterdir()) or not meta.is_file():
            continue
        m = re.search(r'^guid: ([a-f0-9]{32})', meta.read_text(encoding='utf-8-sig'), re.M)
        if m and m[1] in created:
            pair(folder.relative_to(ROOT).as_posix(), '_Quarantine/2026-10-16/RollbackFolders/' + m[1])
    print('Restored the locations and path consumers from before this follow-up. Reopen Unity to reimport.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if args.check == args.apply:
        parser.error('Specify exactly one of --check / --apply')
    apply() if args.apply else check()
