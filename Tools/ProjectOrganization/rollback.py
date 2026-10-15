"""Revert this organization without discarding subsequent edits.

Close Unity, then run: py Tools/ProjectOrganization/rollback.py --check
Use --apply only if the check succeeds. Requires the preserved local edit backups.
No file is permanently deleted; newly added assets are archived outside Assets.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import re

from finish_organization import ROOT, OUT, BACKUP, confined, load, move_pair, require_closed


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check():
    state = load('state-after.json')['files']
    problems = [p for p, expected in state.items() if not confined(p).is_file() or sha(confined(p)) != expected]
    recorded_assets = {p for p in state if p.startswith('Assets/')}
    current_assets = {p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets').rglob('*') if p.is_file()}
    problems.extend(sorted(current_assets - recorded_assets))
    for edit in load('edit-plan.json')['edits']:
        if edit['before_sha256']:
            backup = BACKUP / edit['destination']
            if not backup.is_file() or sha(backup) != edit['before_sha256']:
                problems.append('Missing or changed backup: ' + edit['destination'])
    if problems:
        raise RuntimeError('Refusing to overwrite later changes:\n' + '\n'.join(problems[:50]))
    print('PASS: recorded assets unchanged; original-byte backups available. Apply additionally requires Unity closed.')


def apply():
    require_closed()
    check()
    created = {e['guid'] for e in load('created-folders.json')['folders']}

    def archive_new_empty_folders(scope):
        folders = [p for p in scope.rglob('*') if p.is_dir()] if scope.is_dir() else []
        if scope.is_dir():
            folders.append(scope)
        for folder in sorted(folders, key=lambda p: -len(p.parts)):
            meta = Path(str(folder) + '.meta')
            if not folder.exists() or any(folder.iterdir()) or not meta.is_file():
                continue
            match = re.search(r'^guid: ([a-f0-9]{32})', meta.read_text(encoding='utf-8-sig'), re.M)
            if match and match[1] in created:
                move_pair(folder.relative_to(ROOT).as_posix(), '_Quarantine/2026-10-15/RollbackFolders/' + match[1])
    for edit in load('edit-plan.json')['edits']:
        if edit['before_sha256'] is None:
            destination = '_Quarantine/2026-10-15/RollbackArtifacts/' + edit['destination']
            move_pair(edit['destination'], destination)
        else:
            confined(edit['destination']).write_bytes((BACKUP / edit['destination']).read_bytes())
    for asset in reversed(load('quarantine-journal.json')['assets']):
        move_pair(asset['destination'], asset['original'])
    # Reverse compressed folder moves and individual asset moves as exact pairs.
    for move in reversed(load('move-journal.json')['moves']):
        archive_new_empty_folders(confined(move['destination']))
        move_pair(move['destination'], move['source'])
    for folder in reversed(load('empty-folder-journal.json')['folders']):
        source, target = confined(folder['destination']), confined(folder['original'])
        if not target.exists():
            move_pair(folder['destination'], folder['original'])
        else:
            old_meta, new_meta = Path(str(source) + '.meta'), Path(str(target) + '.meta')
            if new_meta.exists():
                assert old_meta.read_bytes() == new_meta.read_bytes(), 'Folder meta collision: ' + str(target)
            else:
                shutil.move(str(old_meta), str(new_meta))
    archive_new_empty_folders(ROOT / 'Assets')
    print('Restored original asset paths and pre-organization edits. Open Unity to reimport.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if args.check == args.apply:
        parser.error('Specify exactly one of --check / --apply')
    apply() if args.apply else check()
