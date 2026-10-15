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
    path = Path(path)
    relative = path.relative_to(ROOT).as_posix()
    suffix = '.meta' if relative.endswith('.meta') else ''
    key = relative[:-5] if suffix else relative
    return ROOT / (_mapping.get(key, key) + suffix)
