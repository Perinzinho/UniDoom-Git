"""Verify content, importer bytes, GUIDs and actual Unity snapshots after organization."""
import collections
import csv
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Tools/ProjectOrganization'


def load(name):
    return json.loads((OUT / name).read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    before = load('audit-before.json')
    mapping = {m['source']: m['destination'] for m in load('path-map.json')['mappings']}
    edits = {m['destination']: m for m in load('edit-plan.json')['edits']}
    journal = load('move-journal.json')['moves']
    empty = {e['original']: e['destination'] for e in load('empty-folder-journal.json')['folders']}
    failures = []
    verified_files = 0
    for path, node in before['nodes'].items():
        target = mapping.get(path, path)
        expected = edits.get(target, {}).get('after_sha256', node['sha256'])
        if not (ROOT / target).is_file() or sha(ROOT / target) != expected:
            failures.append('Unexpected content change or missing file: ' + target)
        else:
            verified_files += 1
    meta_verified = 0
    for path, expected in load('meta-before.json').items():
        original = path[:-5]
        target = mapping.get(original, empty.get(original, original))
        if target == original:
            for move in journal:
                if original == move['source'] or original.startswith(move['source'] + '/'):
                    target = move['destination'] + original[len(move['source']):]
                    break
        target += '.meta'
        if not (ROOT / target).is_file() or sha(ROOT / target) != expected:
            failures.append('Importer/GUID meta changed or missing: ' + target)
        else:
            meta_verified += 1
    old = {e['guid']: e for e in load('unity-before.json')['entries']}
    new = {e['guid']: e for e in load('unity-after.json')['entries']}
    quarantined = {p for p, target in mapping.items() if target.startswith('_Quarantine/')}
    scene_count = prefab_count = compared = 0
    metrics = ('type', 'dependencies', 'objects', 'components', 'missingScripts', 'missingReferences', 'nullMaterials', 'nullMeshes', 'bindings', 'error')
    for guid, entry in old.items():
        if entry['path'] in quarantined:
            if guid in new:
                failures.append('Quarantined asset is still imported: ' + entry['path'])
            continue
        if guid not in new:
            failures.append('GUID lost: ' + guid + ' ' + entry['path'])
            continue
        actual = new[guid]
        for metric in metrics:
            if entry.get(metric) != actual.get(metric):
                failures.append('Unity snapshot changed: ' + actual['path'] + ' / ' + metric)
        if actual['path'] != mapping.get(entry['path'], entry['path']):
            failures.append('Wrong Unity path: ' + actual['path'])
        compared += 1
        scene_count += entry['path'].endswith('.unity')
        prefab_count += entry['path'].endswith('.prefab')
    problems = [{k: e[k] for k in ('path', 'missingScripts', 'missingReferences', 'nullMaterials', 'nullMeshes')}
                for e in new.values() if any(e.get(k, 0) for k in ('missingScripts', 'missingReferences', 'nullMaterials', 'nullMeshes'))]
    records = load('classification.json')['assets']
    for name, rows in [('classification.csv', records), ('manual-review.csv', [r for r in records if r['classification'] == 'REVISÃO MANUAL'])]:
        with (OUT / name).open('w', newline='', encoding='utf-8-sig') as handle:
            writer = csv.writer(handle)
            writer.writerow(['original', 'destination', 'classification', 'reason', 'scenarios', 'incoming_count'])
            for r in rows:
                writer.writerow([r[k] for k in ('original', 'destination', 'classification', 'reason')] + [','.join(r['scenarios']), len(r['incoming'])])
    summary = {'passed': not failures, 'files_verified': verified_files, 'metas_verified': meta_verified,
               'unity_assets_compared': compared, 'scenes_opened': scene_count, 'prefabs_opened': prefab_count,
               'quarantined_assets': len(quarantined), 'classifications': dict(collections.Counter(r['classification'] for r in records)),
               'preexisting_problems': problems, 'failures': failures}
    (OUT / 'validation.json').write_text(json.dumps(summary, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in summary.items() if k != 'preexisting_problems'}, indent=2, ensure_ascii=False))
    if failures:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
