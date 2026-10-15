"""Produce a conservative move manifest from both Unity and serialized dependencies.

Read-only for Assets. Run once before Apply; audit-before.json is an immutable baseline.
"""
import collections
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Tools/ProjectOrganization'
PROJECT = 'Assets/_Project/'
QUARANTINE = '_Quarantine/2026-10-15/'
GENERATORS = {'TCMapBuilder.cs', 'TCAssets.cs', 'TCAtmosphere.cs', 'TCTheme.cs'}


def write(name, data):
    (OUT / name).write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def owner(path):
    for folder, scene in [('Basement', 'Basement'), ('ClassRoom', 'ClassRoom'),
                          ('Cylinder', 'Cylinder'), ('DirectorOffice', 'DirectorRoom'),
                          ('EntranceGarden', 'EntranceGarden')]:
        if path.startswith('Assets/Art/' + folder + '/'):
            return scene
    if path.startswith(('Assets/Art/SkillAtmosphere/Materials/', 'Assets/Art/TotalChaosInspired/Materials/')):
        name = path.split('/')[4]
    elif path.startswith(('Assets/Art/SkillAtmosphere/Meshes/', 'Assets/Art/TotalChaosInspired/Meshes/')):
        name = Path(path).name.split('_')[0]
    else:
        return None
    return 'Basement' if name.startswith('Basement') else name


def default_destination(path, usage):
    p = Path(path)
    if path.startswith('Assets/Art/'):
        parts = path.split('/')
        source = parts[2]
        if source in {'Basement', 'ClassRoom', 'Cylinder', 'DirectorOffice', 'EntranceGarden'}:
            scene = owner(path)
            rel = '/'.join(parts[3:])
            if p.suffix == '.prefab' and '/' not in rel:
                rel = 'Prefabs/' + rel
            return PROJECT + 'Scenes/' + scene + '/' + rel
        if source in {'SkillAtmosphere', 'TotalChaosInspired'}:
            kind = parts[3]
            scene = owner(path)
            if kind == 'Materials':
                return PROJECT + 'Scenes/' + scene + '/Materials/' + source + '/' + '/'.join(parts[4:])
            if kind == 'Meshes':
                return PROJECT + 'Scenes/' + scene + '/Meshes/' + source + '/' + p.name
            if kind == 'Profiles' and len(usage.get(path, [])) == 1:
                return PROJECT + 'Scenes/' + next(iter(usage[path])) + '/Profiles/' + source + '/' + p.name
            return PROJECT + 'Global/' + kind + '/' + source + '/' + '/'.join(parts[4:])
    if path.startswith('Assets/prefabs/Scenario/'):
        if '/Doors/' not in path:
            return PROJECT + 'Scenes/' + p.stem + '/Prefabs/' + p.name
        users = usage.get(path, set())
        prefix = 'Scenes/' + next(iter(users)) if len(users) == 1 else 'Global'
        return PROJECT + prefix + '/Prefabs/Doors/' + p.name
    if path.startswith('Assets/prefabs/Materials/'):
        return PROJECT + 'Global/Materials/Prefabs/' + p.name
    if path.startswith('Assets/prefabs/'):
        return PROJECT + 'Global/Prefabs/' + path.removeprefix('Assets/prefabs/')
    if path.startswith('Assets/Scripts/Editor/'):
        return PROJECT + 'Editor/Tools/' + p.name
    if path.startswith('Assets/Scripts/Scenario/'):
        scene = 'EntranceGarden' if p.name.startswith('EntranceGarden') else 'Basement'
        return PROJECT + 'Scenes/' + scene + '/Scripts/' + p.name
    if path.startswith('Assets/Scripts/'):
        return PROJECT + 'Global/Scripts/' + path.removeprefix('Assets/Scripts/')
    if path.startswith('Assets/TotalChaos/Runtime/'):
        return PROJECT + 'Global/Scripts/TotalChaos/' + p.name
    if path.startswith('Assets/TotalChaos/Editor/'):
        return PROJECT + 'Editor/Tools/TotalChaos/' + p.name
    if path.startswith('Assets/TotalChaos/Shaders/'):
        return PROJECT + 'Global/Shaders/TotalChaos/' + p.name
    if path.startswith('Assets/Materials/'):
        kind = 'Textures' if p.suffix == '.png' else 'Materials'
        return PROJECT + 'Global/' + kind + '/Placeholders/' + path.removeprefix('Assets/Materials/')
    if path.startswith('Assets/Scenes/'):
        return PROJECT + 'Scenes/Main/' + p.name
    if path.startswith('Assets/_Recovery/'):
        return PROJECT + 'Scenes/_Recovery/' + p.name
    if path.startswith('Assets/Settings/'):
        if p.name == 'SampleSceneProfile.asset':
            return PROJECT + 'Scenes/Main/Profiles/' + p.name
        return PROJECT + 'Global/Settings/' + p.name
    if path.startswith('Assets/sfx/'):
        return PROJECT + 'Global/Audio/' + path.removeprefix('Assets/sfx/')
    for kind in ('Fonts', 'Sprites', 'Textures'):
        if path.startswith('Assets/' + kind + '/'):
            return PROJECT + 'Global/' + kind + '/' + path.removeprefix('Assets/' + kind + '/')
    if path == 'Assets/InputSystem_Actions.inputactions':
        return PROJECT + 'Global/Settings/' + p.name
    if path == 'Assets/Editor/ProjectOrganizationAudit.cs':
        return PROJECT + 'Editor/Tools/' + p.name
    return path


def main():
    audit = json.loads((OUT / 'audit-before.json').read_text(encoding='utf-8'))
    unity = json.loads((OUT / 'unity-before.json').read_text(encoding='utf-8-sig'))
    nodes, index = audit['nodes'], audit['guid_index']
    by_path = {e['path']: e for e in unity['entries']}
    usage = collections.defaultdict(set)
    for name, path in audit['scenario_roots'].items():
        for guid in by_path[path]['dependencies']:
            if guid in index:
                usage[index[guid]].add(name)
    for path in nodes:
        scene = owner(path)
        if scene:
            usage[path].add(scene)
    # Include retained original materials and unplaced authored assets, not only build scenes.
    changed = True
    while changed:
        changed = False
        for path, node in nodes.items():
            for guid in node['references']:
                target = index.get(guid)
                if target and usage[path] - usage[target]:
                    usage[target].update(usage[path])
                    changed = True
    incoming = collections.defaultdict(set)
    for e in unity['entries']:
        for guid in e['dependencies']:
            target = index.get(guid)
            if target and target != e['path']:
                incoming[target].add(e['path'])
    for path, node in nodes.items():
        for guid in node['references']:
            target = index.get(guid)
            if target and target != path:
                incoming[target].add(path)
    # Configuration references are roots too, even when not reported by GetDependencies.
    for settings in (ROOT / 'ProjectSettings').rglob('*'):
        if not settings.is_file():
            continue
        for guid in re.findall(rb'\bguid:\s*([a-f0-9]{32})', settings.read_bytes()):
            target = index.get(guid.decode())
            if target:
                incoming[target].add(settings.relative_to(ROOT).as_posix())
    quarantine = {}
    for name in sorted(GENERATORS):
        path = 'Assets/TotalChaos/Editor/' + name
        assert not incoming[path], (path, incoming[path])
        quarantine[path] = {'reason': 'Gerador de Editor arquivado por escolha explícita do usuário; resultados persistentes preservados; sem referências serializadas externas.',
                            'canonical': 'Tools/TotalChaosSkill/Source/total-chaos-unity-maps/assets/unity/Editor/' + name}
    # Only duplicate persistent Mesh assets with no incoming references are disposable.
    # Textures and materials may be loaded by constructed Editor paths; keep all of them.
    meshes = collections.defaultdict(list)
    for path in nodes:
        if Path(path).suffix != '.asset' or not path.startswith('Assets/Art/'):
            continue
        raw = (ROOT / path).read_bytes()
        if not raw.startswith(b'%YAML') or b'--- !u!43 &' not in raw[:200]:
            continue
        normalized = re.sub(rb'(?m)^  m_Name:[^\r\n]*', b'', raw)
        meshes[hashlib.sha256(normalized).hexdigest()].append(path)
    literal_sources = []
    for base in ('Assets', 'ProjectSettings', 'Packages'):
        for p in (ROOT / base).rglob('*'):
            if p.is_file() and p.suffix in {'.cs', '.json', '.asset', '.yaml', '.xml'} and p.stat().st_size < 1_000_000:
                raw = p.read_bytes()
                if b'Assets/' in raw:
                    literal_sources.append(raw)
    literal_text = b'\n'.join(literal_sources)
    for group in meshes.values():
        used = sorted(p for p in group if incoming[p])
        if not used:
            continue
        for path in group:
            if incoming[path]:
                continue
            # Literal paths in active code/config/data are a reason to keep the asset.
            literal = path.encode()
            if literal in literal_text:
                continue
            quarantine[path] = {'reason': 'Mesh persistente idêntica em todos os campos serializados, exceto nome, a uma mesh mantida; nenhuma dependência Unity/GUID/caminho literal encontrada.', 'canonical': used[0]}
    mapping, records = {}, []
    all_paths = list(nodes) + ['Assets/Editor/ProjectOrganizationAudit.cs']
    for path in all_paths:
        destination = default_destination(path, usage)
        natural = destination
        if path.startswith('Assets/Art/') and len(usage[path]) > 1:
            kind = 'Textures' if Path(path).suffix == '.png' else 'Materials' if Path(path).suffix == '.mat' else 'Meshes'
            destination = PROJECT + 'Global/' + kind + '/' + path.removeprefix('Assets/Art/')
        if path in quarantine:
            destination = QUARANTINE + path
            classification = 'DESCARTE SEGURO'
            reason = quarantine[path]['reason']
        elif path.startswith(('Assets/TextMesh Pro/', 'Assets/TutorialInfo/', 'Assets/ProBuilder Data/', 'Assets/Unity.VisualScripting.Generated/')) or path == 'Assets/Readme.asset':
            classification, reason = 'MANTER', 'Pacote externo, conteúdo de template ou pasta gerenciada pela Unity preservada.'
        elif path in nodes and not incoming[path] and Path(path).suffix not in {'.cs', '.hlsl', '.shader', '.md'}:
            classification, reason = 'REVISÃO MANUAL', 'Sem dependência serializada de entrada; preservado porque ausência de referência não comprova descarte.'
        elif '/Global/' in destination:
            classification, reason = 'GLOBAL', 'Compartilhado entre ambientes ou pertencente a um sistema global.'
        else:
            classification, reason = 'REORGANIZAR', 'Asset do cenário ou ferramenta de Editor mantida.'
        mapping[path] = destination
        records.append({'original': path, 'destination': destination, 'classification': classification, 'reason': reason,
                        'scenarios': sorted(usage[path]), 'incoming': sorted(incoming[path]), 'natural_destination': natural})
    assert len(set(mapping.values())) == len(mapping), 'Destination collision'
    moves = []
    covered = set()
    folder_destinations = set()
    # Collapse complete directory moves to preserve their folder GUIDs as well.
    directories = sorted((p for p in (ROOT / 'Assets').rglob('*') if p.is_dir()), key=lambda p: (len(p.parts), str(p)))
    for folder in directories:
        source = folder.relative_to(ROOT).as_posix()
        children = [p for p in mapping if p.startswith(source + '/')]
        if not children or any(p in covered for p in children):
            continue
        first = children[0]
        suffix = first[len(source):]
        target = mapping[first][:-len(suffix)] if mapping[first].endswith(suffix) else ''
        if not target.startswith(PROJECT) or target == source:
            continue
        if target in folder_destinations:
            continue
        if all(mapping[p] == target + p[len(source):] for p in children):
            moves.append({'source': source, 'destination': target})
            folder_destinations.add(target)
            covered.update(children)
    for path, destination in mapping.items():
        if path not in covered and destination != path and destination.startswith(PROJECT):
            moves.append({'source': path, 'destination': destination})
    for path, detail in quarantine.items():
        detail.update(original=path, destination=mapping[path], checked=['AssetDatabase.GetDependencies de todos os assets', 'GUIDs em Assets e ProjectSettings', 'Código de runtime e Editor', 'Carregamento dinâmico/Resources/Addressables'], date='2026-10-15')
    write('move-plan.json', {'moves': moves})
    write('path-map.json', {'mappings': [{'source': p, 'destination': q} for p, q in mapping.items() if p != q]})
    write('classification.json', {'assets': records})
    write('quarantine-plan.json', {'assets': list(quarantine.values())})
    # Aliases for paths constructed by maintained tools (not serialized references).
    write('path-aliases.json', {'mappings': [{'source': r['natural_destination'], 'destination': r['destination']} for r in records
                                            if r['natural_destination'] != r['destination'] and r['original'] not in quarantine]})
    print('Assets:', len(records), 'Move operations:', len(moves), 'Quarantine:', len(quarantine))
    print(collections.Counter(r['classification'] for r in records))


if __name__ == '__main__':
    main()
