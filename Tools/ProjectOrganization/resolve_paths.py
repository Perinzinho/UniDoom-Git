"""Resolve historical verification paths, including files preserved in quarantine."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
_mapping = None


def resolve(path):
    global _mapping
    if _mapping is None:
        data = json.loads((ROOT / 'Tools/ProjectOrganization/path-map.json').read_text(encoding='utf-8'))
        _mapping = {m['source']: m['destination'] for m in data['mappings']}
        for batch in sorted((ROOT / 'Tools/ProjectOrganization').glob('????-??-??/path-map.json')):
            later = json.loads(batch.read_text(encoding='utf-8'))
            _mapping.update({m['source']: m['destination'] for m in later['mappings']})
    path = Path(path)
    relative = path.relative_to(ROOT).as_posix()
    suffix = '.meta' if relative.endswith('.meta') else ''
    key = relative[:-5] if suffix else relative
    seen = set()
    while key in _mapping and key not in seen:
        seen.add(key)
        key = _mapping[key]
    return ROOT / (key + suffix)
