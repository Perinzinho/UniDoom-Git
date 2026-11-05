"""Read-only Unity text audit. Run from the project root; no third-party modules."""
import collections
import datetime
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / '.ai/map-review'
GUID = re.compile(r'guid: ([0-9a-f]{32})')
HEADER = re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$', re.M)
REF = re.compile(r'\{fileID: (-?\d+)([^{}]*)\}')
TEXT = {'.unity', '.prefab', '.mat', '.asset', '.controller', '.overrideController', '.anim', '.shader', '.hlsl', '.cs', '.json', '.meta'}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def rel(path):
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return str(path)

def read(path):
    return path.read_text(encoding='utf-8-sig', errors='replace')

def docs(text):
    matches = list(HEADER.finditer(text))
    return [(int(m[1]), m[2], text[m.end():matches[i+1].start() if i+1 < len(matches) else len(text)]) for i, m in enumerate(matches)]

def prop(text, key):
    m = re.search(r'^  ' + re.escape(key) + r': (.*)$', text, re.M)
    return m[1].strip() if m else None

def main():
    previous_path = OUT/'static-audit.json'
    previous = json.loads(read(previous_path)) if previous_path.exists() else {}
    previous_assets = {a['path']:a for a in previous.get('assets',[])}
    scanner_hash = sha(pathlib.Path(__file__))
    cache_compatible = previous.get('scanner_sha256') == scanner_hash
    reused = 0
    index = collections.defaultdict(list)
    for base in [ROOT/'Assets', ROOT/'Packages', ROOT/'Library/PackageCache']:
        if base.exists():
            for path in base.rglob('*.meta'):
                m = re.search(r'^guid: ([0-9a-f]{32})', read(path), re.M)
                if m:
                    index[m[1]].append(path.with_suffix(''))
    cache = {}
    index_hash = hashlib.sha256(json.dumps({g:sorted(rel(p) for p in ps) for g,ps in sorted(index.items())}, sort_keys=True).encode()).hexdigest()
    cache_compatible = cache_compatible and previous.get('guid_index_sha256') == index_hash
    def inspect(path):
        nonlocal reused
        if path in cache:
            return cache[path]
        digest = sha(path)
        meta = pathlib.Path(str(path)+'.meta')
        meta_digest = sha(meta) if meta.is_file() else None
        old = previous_assets.get(rel(path))
        if cache_compatible and old and old['sha256'] == digest and old.get('meta_sha256') == meta_digest and all((ROOT/p).is_file() for p in old['dependencies']):
            # Reuse parsing only; transitive dependencies are still traversed and hashed.
            data = dict(old)
            # GUID targets may have changed without this consumer changing.
            for issue in data['issues']:
                if issue['kind'] == 'unresolved_guid' and issue['guid'] in index:
                    data = None
                    break
            if data is not None:
                cache[path] = data
                reused += 1
                return data
        data = {'path': rel(path), 'sha256': digest, 'meta_sha256':meta_digest, 'dependencies': [], 'issues': []}
        cache[path] = data
        if path.suffix not in TEXT:
            return data
        text = read(path)
        if not (text.startswith('%YAML') or path.suffix in {'.shader', '.hlsl', '.cs', '.json', '.meta'}):
            return data
        for guid in sorted(set(GUID.findall(text))):
            if guid.startswith('0000000000000000'):
                continue
            candidates = index.get(guid, [])
            if not candidates:
                data['issues'].append({'rule':'R01', 'kind':'unresolved_guid', 'guid':guid})
            elif len(candidates) == 1 and candidates[0].is_file():
                data['dependencies'].append(rel(candidates[0]))
        parsed = docs(text)
        ids = {d[1] for d in parsed}
        if len(ids) != len(parsed):
            data['issues'].append({'rule':'R01','kind':'duplicate_file_id'})
        for cid, fid, body in parsed:
            for match in REF.finditer(body):
                if match[1] != '0' and 'guid:' not in match[2] and match[1] not in ids:
                    data['issues'].append({'rule':'R01','kind':'unresolved_local_file_id','owner':fid,'file_id':match[1]})
            if cid == 114 and prop(body, 'm_Script') == '{fileID: 0}':
                data['issues'].append({'rule':'R01','kind':'missing_script','owner':fid})
        if path.suffix in {'.unity','.prefab'}:
            counts = collections.Counter(d[0] for d in parsed)
            data['components'] = {str(k):v for k,v in sorted(counts.items())}
            data['names'] = [prop(b,'m_Name') for c,f,b in parsed if c == 1]
            data['lights'] = [{k:prop(b,k) for k in ['m_GameObject','m_Type','m_Intensity','m_Range','m_Color','m_Enabled']} for c,f,b in parsed if c == 108]
            data['regions'] = [{k:prop(b,k) for k in ['theme','fogDensity','ambient','fogColor','directionalLightMultiplier','blendDistance']} for c,f,b in parsed if c == 114 and prop(b,'theme')]
            data['transitions'] = [{k:prop(b,k) for k in ['destinationScene','destinationSpawnId']} for c,f,b in parsed if c == 114 and prop(b,'destinationScene')]
        return data
    scenes = []
    for path in sorted((ROOT/'Assets').rglob('*.unity')):
        excluded = 'DirectorRoom' in path.parts
        entry = {'path':rel(path),'sha256':sha(path),'size':path.stat().st_size,'modified_utc':datetime.datetime.fromtimestamp(path.stat().st_mtime, datetime.timezone.utc).isoformat(),'eligible':not excluded}
        if excluded:
            entry.update(status='excluded', reason='Sala do diretor em desenvolvimento; exclusão explícita do usuário.')
        else:
            pending, visited = [path], set()
            while pending:
                dep = pending.pop()
                if dep in visited:
                    continue
                visited.add(dep)
                item = inspect(dep)
                pending.extend(ROOT/p for p in item['dependencies'])
            entry.update(status='audited', dependencies=sorted(rel(p) for p in visited if p != path))
            entry['issues'] = [dict(issue, path=rel(p)) for p in sorted(visited) for issue in cache[p]['issues']]
            entry['serialized_components'] = dict(sum((collections.Counter(cache[p].get('components',{})) for p in visited if p.suffix in {'.unity','.prefab'}), collections.Counter()))
            entry['regions'] = [dict(region,path=rel(p)) for p in visited for region in cache[p].get('regions',[])]
            entry['transitions'] = [dict(transition,path=rel(p)) for p in visited for transition in cache[p].get('transitions',[])]
        scenes.append(entry)
    rules = {rel(p):sha(p) for p in [ROOT/'AGENTS.md', ROOT/'.codex/reviewer/SKILL.md', ROOT/'.codex/reviewer/PROMPT-DE-EXECUCAO.md', ROOT/'.codex/SKILL.md', ROOT/'.codex/PROMPT_MESTRE.md']}
    result = {'generated_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scanner_sha256':scanner_hash,'guid_index_sha256':index_hash,'reused_assets':reused,'mode':'fix','batch_size':5,'rules':rules,'authorship':'Não confirmada; todas as cenas elegíveis foram incluídas.','scenes':scenes,'assets':list(cache.values()),'duplicate_guids':{g:[rel(p) for p in ps] for g,ps in index.items() if len(ps)>1}}
    target = OUT/'static-audit.json'
    temporary = target.with_suffix('.json.tmp')
    temporary.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    temporary.replace(target)
    for entry in scenes:
        print(entry['path'],entry['status'], 'deps=',len(entry.get('dependencies',[])), 'issues=',len(entry.get('issues',[])))
    print('Assets inspected:',len(cache),'Duplicate GUIDs:',len(result['duplicate_guids']))

if __name__ == '__main__':
    main()
