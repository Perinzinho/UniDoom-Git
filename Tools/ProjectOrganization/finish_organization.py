"""Prepare/apply path edits and quarantine after the reviewed Unity move plan.

--prepare only writes staging files outside Assets. --apply requires the Unity
move journal, and refuses to touch the project while its Editor lock is held.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import posixpath
import shutil

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Tools/ProjectOrganization'
STAGE = ROOT / '.utmp/ProjectOrganization/staged'
BACKUP = OUT / 'Backups'


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def write(name, data):
    (OUT / name).write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def load(name):
    return json.loads((OUT / name).read_text(encoding='utf-8-sig'))


def confined(path):
    p = (ROOT / path).resolve()
    if not p.is_relative_to(ROOT) or p == ROOT:
        raise ValueError('Path outside project: ' + str(path))
    return p


def require_closed():
    lock = ROOT / 'Temp/UnityLockfile'
    if os.name == 'nt' and lock.exists():
        import msvcrt
        try:
            with lock.open('r+b') as handle:
                msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
                msvcrt.locking(handle.fileno(), msvcrt.LK_UNLCK, 1)
        except OSError as ex:
            raise RuntimeError('Close Unity before applying or rolling back') from ex


def prepare():
    mapping = {m['source']: m['destination'] for m in load('path-map.json')['mappings']}
    aliases = load('path-aliases.json')['mappings']
    replacements = sorted(mapping.items(), key=lambda pair: -len(pair[0]))
    prefixes = {
        'Assets/Art/SkillAtmosphere/Materials/ClassRoom/': 'Assets/_Project/Scenes/ClassRoom/Materials/SkillAtmosphere/ClassRoom/',
        'Assets/Art/ClassRoom/': 'Assets/_Project/Scenes/ClassRoom/',
        'Assets/Art/Basement/': 'Assets/_Project/Scenes/Basement/',
        'Assets/Settings/': 'Assets/_Project/Global/Settings/',
        'Assets/Sprites/': 'Assets/_Project/Global/Sprites/',
    }

    def replace(text):
        for old, new in replacements:
            text = text.replace(old, new)
        for old, new in prefixes.items():
            text = text.replace(old, new)
        return text

    edits = []

    def stage(source, destination, new_raw):
        old_raw = confined(source).read_bytes() if source else None
        if old_raw == new_raw:
            return
        staged = STAGE / destination
        staged.parent.mkdir(parents=True, exist_ok=True)
        staged.write_bytes(new_raw)
        backup = BACKUP / destination
        backup.parent.mkdir(parents=True, exist_ok=True)
        if old_raw is not None:
            backup.write_bytes(old_raw)
        edits.append({'original': source, 'destination': destination,
                      'before_sha256': digest(old_raw) if old_raw is not None else None,
                      'after_sha256': digest(new_raw)})

    for source, destination in mapping.items():
        if not destination.startswith('Assets/'):
            continue
        if Path(source).suffix not in {'.cs', '.json'}:
            continue
        raw = confined(source).read_bytes()
        text = raw.decode('utf-8-sig')
        if source.endswith('.json'):
            data = json.loads(text)

            def transform(value):
                if isinstance(value, list):
                    return [transform(v) for v in value]
                if isinstance(value, dict):
                    return {k: transform(v) for k, v in value.items()}
                if isinstance(value, str):
                    if value in mapping:
                        return mapping[value]
                    relative_source = posixpath.normpath(posixpath.join(posixpath.dirname(source), value))
                    if relative_source in mapping and not value.startswith(('Assets/', 'http')):
                        return posixpath.relpath(mapping[relative_source], posixpath.dirname(destination))
                return value

            updated = transform(data)
            if updated == data:
                continue
            result = (json.dumps(updated, indent=2, ensure_ascii=False) + '\n').encode('utf-8')
        else:
            updated = replace(text)
            if source.endswith('TotalChaosMaterialConverter.cs'):
                updated = updated.replace('AssetDatabase.LoadAssetAtPath<Texture2D>(path)', 'AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectAssetPaths.Resolve(path))')
            if source.endswith('MedkitPrefabCreator.cs'):
                updated = updated.replace('"Assets/Prefabs/Medkit.prefab"', '"Assets/_Project/Global/Prefabs/Pickups/Medkit.prefab"')
                updated = updated.replace('if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))\n        {\n            AssetDatabase.CreateFolder("Assets", "Prefabs");\n        }',
                                          'EnsureFolder("Assets/_Project/Global/Prefabs/Pickups");')
                updated = updated.replace('    [MenuItem(', '    static void EnsureFolder(string path)\n    {\n        if (AssetDatabase.IsValidFolder(path)) return;\n        string parent = System.IO.Path.GetDirectoryName(path).Replace(\'\\\\\', \'/\');\n        EnsureFolder(parent);\n        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));\n    }\n\n    [MenuItem(', 1)
            if updated == text:
                continue
            result = updated.encode('utf-8-sig' if raw.startswith(b'\xef\xbb\xbf') else 'utf-8')
        stage(source, destination, result)
    settings = 'ProjectSettings/EditorBuildSettings.asset'
    stage(settings, settings, replace(confined(settings).read_text(encoding='utf-8-sig')).encode('utf-8'))
    bindings = 'Tools/ClassRoom/Optimization/texture-bindings.json'
    stage(bindings, bindings, replace(confined(bindings).read_text(encoding='utf-8-sig')).encode('utf-8'))
    # Keep the existing verification scripts usable with their historical backups.
    for script in ('Tools/Basement/Optimization/verify_basement.py', 'Tools/ClassRoom/Optimization/verify_classroom.py'):
        text = confined(script).read_text(encoding='utf-8-sig')
        text = text.replace("ROOT = Path(__file__).resolve().parents[3]", "ROOT = Path(__file__).resolve().parents[3]\nimport sys\nsys.path.insert(0, str(ROOT / 'Tools/ProjectOrganization'))\nfrom resolve_paths import resolve")
        if 'Basement' in script:
            text = text.replace("ROOT / 'Assets/prefabs/Scenario/Basement.prefab'", "resolve(ROOT / 'Assets/prefabs/Scenario/Basement.prefab')")
            text = text.replace('ROOT / path for guid, path', 'resolve(ROOT / path) for guid, path')
        else:
            text = text.replace('ROOT / p for g, p', 'resolve(ROOT / p) for g, p')
            text = text.replace('read(ROOT / path)', 'read(resolve(ROOT / path))')
        stage(script, script, text.encode('utf-8'))
    helper = 'using System.Collections.Generic;\n\n// Canonical aliases for paths constructed by maintained Editor tools.\npublic static class ProjectAssetPaths\n{\n    static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>\n    {\n'
    for alias in aliases:
        helper += '        { ' + json.dumps(alias['source']) + ', ' + json.dumps(alias['destination']) + ' },\n'
    helper += '    };\n    public static string Resolve(string path) => Aliases.TryGetValue(path, out var actual) ? actual : path;\n}\n'
    stage(None, 'Assets/_Project/Editor/Tools/ProjectAssetPaths.cs', helper.encode('utf-8'))
    write('edit-plan.json', {'edits': edits})
    print('Prepared edits:', len(edits))


def move_pair(source, destination):
    src, dst = confined(source), confined(destination)
    if not src.exists() or dst.exists() or confined(destination + '.meta').exists():
        raise RuntimeError('Unsafe move: ' + source + ' -> ' + destination)
    dst.parent.mkdir(parents=True, exist_ok=True)
    meta = confined(source + '.meta')
    if not meta.is_file():
        raise RuntimeError('Missing meta: ' + source)
    shutil.move(str(src), str(dst))
    try:
        shutil.move(str(meta), str(dst) + '.meta')
    except Exception:
        shutil.move(str(dst), str(src))
        raise


def apply():
    require_closed()
    planned = load('move-plan.json')['moves']
    assert load('move-journal.json')['moves'] == planned, 'Unity did not finish the complete move plan'
    edits = load('edit-plan.json')['edits']
    for edit in edits:
        target = confined(edit['destination'])
        actual = digest(target.read_bytes()) if target.exists() else None
        assert actual in {edit['before_sha256'], edit['after_sha256']}, ('Changed since preparation', edit['destination'])
    for edit in edits:
        target = confined(edit['destination'])
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((STAGE / edit['destination']).read_bytes())
    quarantined = []
    for asset in load('quarantine-plan.json')['assets']:
        move_pair(asset['original'], asset['destination'])
        quarantined.append(asset)
        write('quarantine-journal.json', {'assets': quarantined})
    # Preserve obsolete empty folders and their metas outside Assets as well.
    empty = []
    for folder in sorted((ROOT / 'Assets').rglob('*'), key=lambda p: -len(p.parts)):
        if not folder.is_dir() or folder.as_posix().startswith((ROOT / 'Assets/_Project').as_posix() + '/'):
            continue
        if any(folder.iterdir()) or not Path(str(folder) + '.meta').is_file():
            continue
        source = folder.relative_to(ROOT).as_posix()
        destination = '_Quarantine/2026-10-15/EmptyFolders/' + f'{len(empty):04d}_' + folder.name
        move_pair(source, destination)
        empty.append({'original': source, 'destination': destination, 'date': '2026-10-15', 'reason': 'Pasta vazia após reorganização; meta preservado.'})
        write('empty-folder-journal.json', {'folders': empty})
    print('Applied edits:', len(edits), 'Quarantined assets:', len(quarantined), 'Empty folders:', len(empty))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--prepare', action='store_true')
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if args.prepare == args.apply:
        parser.error('Specify exactly one of --prepare / --apply')
    prepare() if args.prepare else apply()
