"""Current-state continuation of the approved organization plan, without regenerating assets."""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
sys.path.insert(0, str(OUT.parent))
sys.dont_write_bytecode = True
from finish_organization import require_closed


def sha(path):
    with path.open('rb') as handle:
        return hashlib.file_digest(handle, 'sha256').hexdigest()


def load(name):
    return json.loads((OUT / name).read_text(encoding='utf-8-sig'))


def write(name, data):
    (OUT / name).write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def prepare():
    audit = json.loads((OUT.parent / 'recheck-2026-10-16.json').read_text(encoding='utf-8'))
    mappings = []
    for asset in audit['shared_assets_still_in_scenes']:
        source = asset['path']
        parts = source.split('/')
        destination = '/'.join(['Assets', '_Project', 'Global', parts[4], parts[3]] + parts[5:])
        assert Path(ROOT / source).is_file() and not Path(ROOT / destination).exists()
        mappings.append({'source': source, 'destination': destination, 'scenarios': asset['scenarios'], 'classification': 'GLOBAL'})
    assert len({e['destination'] for e in mappings}) == len(mappings)
    write('path-map.json', {'mappings': mappings})
    write('move-plan.json', {'moves': [{k: e[k] for k in ('source', 'destination')} for e in mappings]})
    baseline = {}
    for base in ('Assets', 'ProjectSettings'):
        for path in (ROOT / base).rglob('*'):
            if path.is_file():
                baseline[path.relative_to(ROOT).as_posix()] = sha(path)
    write('baseline.json', {'files': baseline})
    # Save all settings: Unity build hooks may write computed URP/Player values.
    backups = [ROOT / p for p in baseline if p.startswith('ProjectSettings/')]
    backups += list((ROOT / 'Assets/_Project/Global/Settings').glob('*.asset'))
    for path in backups:
        target = OUT / 'Backups' / path.relative_to(ROOT)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)
    scene = 'Assets/_Project/Scenes/Main/SampleScene.unity'
    patch = subprocess.run(['git', 'diff', '--binary', '--', scene], cwd=ROOT, capture_output=True, check=True).stdout
    (OUT / 'user-saved-scene.patch').write_bytes(patch)
    # Preserve pre-task versions of the audit utility, which now supports separate batches.
    for name in ('Assets/_Project/Editor/Tools/ProjectOrganizationAudit.cs', 'Tools/ProjectOrganization/ProjectOrganizationAudit.cs'):
        raw = subprocess.run(['git', 'show', 'HEAD:' + name], cwd=ROOT, capture_output=True, check=True).stdout
        target = OUT / 'Backups' / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw)
    print('Prepared', len(mappings), 'moves;', len(baseline), 'baseline files; user scene patch preserved.')


def finish():
    require_closed()
    mappings = load('path-map.json')['mappings']
    assert load('move-journal.json')['moves'] == load('move-plan.json')['moves']
    edits = []
    helper = 'Assets/_Project/Editor/Tools/ProjectAssetPaths.cs'
    path = ROOT / helper
    raw = path.read_bytes()
    text = raw.decode('utf-8-sig')
    additions = ''.join('        { ' + json.dumps(e['source']) + ', ' + json.dumps(e['destination']) + ' },\n' for e in mappings)
    updated = text.replace('    };', additions + '    };', 1)
    targets = [(helper, updated.encode('utf-8'))]
    # Direct source paths in the retained provenance JSONs follow the asset location.
    for path in (ROOT / 'Assets').rglob('*.json'):
        raw = path.read_bytes()
        updated = raw
        for e in mappings:
            updated = updated.replace(e['source'].encode(), e['destination'].encode())
        if updated != raw:
            targets.append((path.relative_to(ROOT).as_posix(), updated))
    resolver = 'Tools/ProjectOrganization/resolve_paths.py'
    raw = (ROOT / resolver).read_text(encoding='utf-8')
    raw = raw.replace("        _mapping = {m['source']: m['destination'] for m in data['mappings']}",
        "        _mapping = {m['source']: m['destination'] for m in data['mappings']}\n"
        "        for batch in sorted((ROOT / 'Tools/ProjectOrganization').glob('????-??-??/path-map.json')):\n"
        "            later = json.loads(batch.read_text(encoding='utf-8'))\n"
        "            _mapping.update({m['source']: m['destination'] for m in later['mappings']})")
    raw = raw.replace("    return ROOT / (_mapping.get(key, key) + suffix)",
        "    seen = set()\n    while key in _mapping and key not in seen:\n        seen.add(key)\n        key = _mapping[key]\n    return ROOT / (key + suffix)")
    targets.append((resolver, raw.encode('utf-8')))
    for name, updated in targets:
        path = ROOT / name
        backup = OUT / 'Backups' / name
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, backup)
        before = sha(path)
        path.write_bytes(updated)
        edits.append({'path': name, 'before_sha256': before, 'after_sha256': sha(path)})
    write('edit-journal.json', {'edits': edits})
    empty = []
    # Only retire folders emptied by this batch; preserve managed Unity/package folders.
    for folder in sorted((ROOT / 'Assets/_Project/Scenes').rglob('*'), key=lambda p: -len(p.parts)):
        if not folder.is_dir() or any(folder.iterdir()) or not Path(str(folder) + '.meta').exists():
            continue
        original = folder.relative_to(ROOT).as_posix()
        target = '_Quarantine/2026-10-16/EmptyFolders/' + f'{len(empty):04d}_' + folder.name
        destination = ROOT / target
        destination.parent.mkdir(parents=True, exist_ok=True)
        assert not destination.exists() and not Path(str(destination) + '.meta').exists()
        shutil.move(str(folder), str(destination))
        shutil.move(str(folder) + '.meta', str(destination) + '.meta')
        empty.append({'source': original, 'destination': target, 'date': '2026-10-16', 'reason': 'Pasta vazia após centralização dos recursos compartilhados; meta preservado.'})
    write('empty-folders.json', {'folders': empty})
    print('Updated', len(edits), 'consumers; preserved', len(empty), 'empty folders outside Assets.')


def restore_build_settings():
    require_closed()
    restored = []
    for name, expected in load('baseline.json')['files'].items():
        if not (name.startswith('ProjectSettings/') or name.startswith('Assets/_Project/Global/Settings/')):
            continue
        backup = OUT / 'Backups' / name
        path = ROOT / name
        if backup.exists() and path.is_file() and sha(path) != expected:
            assert sha(backup) == expected
            incidental = ROOT / '.utmp/ProjectOrganization/2026-10-16/build-incidental' / name
            incidental.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, incidental)
            shutil.copyfile(backup, path)
            restored.append(name)
    write('build-incidental.json', {'restored': restored})
    print('Restored baseline bytes of', len(restored), 'settings serialized by Unity build hooks.')


def verify():
    mapping = {e['source']: e['destination'] for e in load('path-map.json')['mappings']}
    folders = {e['source']: e['destination'] for e in load('empty-folders.json')['folders']}
    edits = {e['path']: e['after_sha256'] for e in load('edit-journal.json')['edits']}
    failures = []
    baseline = load('baseline.json')['files']
    for source, expected in baseline.items():
        suffix = '.meta' if source.endswith('.meta') else ''
        key = source[:-5] if suffix else source
        destination = mapping.get(key, folders.get(key, key)) + suffix
        path = ROOT / destination
        if not path.is_file() or sha(path) != edits.get(destination, expected):
            failures.append('File differs: ' + destination)
    before = {e['guid']: e for e in load('unity-before.json')['entries']}
    after = {e['guid']: e for e in load('unity-after.json')['entries']}
    fields = ('type', 'dependencies', 'objects', 'components', 'missingScripts', 'missingReferences', 'nullMaterials', 'nullMeshes', 'bindings', 'error')
    for guid, old in before.items():
        if guid not in after:
            failures.append('Missing GUID: ' + old['path'])
            continue
        new = after[guid]
        for field in fields:
            if old.get(field) != new.get(field):
                failures.append('Unity difference: ' + new['path'] + ' / ' + field)
        if new['path'] != mapping.get(old['path'], old['path']):
            failures.append('Wrong path: ' + new['path'])
    usage = collections.defaultdict(set)
    for entry in after.values():
        if entry['path'].startswith('Assets/_Project/Scenes/'):
            scene = entry['path'].split('/')[3]
            if scene not in {'Main', '_Recovery'}:
                for guid in entry['dependencies']:
                    usage[guid].add(scene)
    shared = [e['path'] for guid, e in after.items()
              if e['path'].startswith('Assets/_Project/Scenes/') and len(usage[guid]) > 1]
    failures.extend('Shared asset still local: ' + path for path in shared)
    result = {'passed': not failures, 'files_checked': len(baseline), 'unity_assets_checked': len(before),
              'moves': len(mapping), 'scenes': sum(e['path'].endswith('.unity') for e in before.values()),
              'prefabs': sum(e['path'].endswith('.prefab') for e in before.values()),
              'shared_assets_still_in_scenes': shared, 'build': load('build-result.json'), 'failures': failures}
    write('validation.json', result)
    print(json.dumps(result, indent=2))
    if failures:
        raise SystemExit(1)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'finish', 'restore-build-settings', 'verify'])
    args = parser.parse_args()
    {'prepare': prepare, 'finish': finish, 'restore-build-settings': restore_build_settings, 'verify': verify}[args.action]()
