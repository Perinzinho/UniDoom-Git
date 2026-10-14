"""Download verified CC0 floor PNGs; reuse the project's original Poly Haven sets."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / '.utmp/BasementPreview/Assets/Art/Basement/Surface'


def fetch(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'UniDoomMaterialImport/1.0'})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()


def main():
    records = []
    for record in json.loads((ROOT / 'Assets/Art/ClassRoom/Surface/texture-sources.json').read_text()):
        if record['asset'] not in ['concrete_wall_006', 'rusty_metal_02', 'wood_table_worn', 'rock_face']:
            continue
        path = ROOT / 'Assets/Art/ClassRoom/Surface' / record['file']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == record['sha256']
        records.append(dict(record, file=path.relative_to(ROOT).as_posix(), reused=True))
    asset = 'concrete_floor_02'
    files = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
    info = json.loads(fetch('https://api.polyhaven.com/info/' + asset))
    directory = ART / 'Textures' / asset
    directory.mkdir(parents=True, exist_ok=True)
    for role in ['Diffuse', 'nor_gl', 'arm', 'Displacement']:
        item = files[role]['2k']['png']
        path = directory / (role + '.png')
        data = path.read_bytes() if path.exists() else fetch(item['url'])
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        if item.get('md5'):
            assert hashlib.md5(data).hexdigest() == item['md5']
        path.write_bytes(data)
        records.append({'asset': asset, 'role': role, 'resolution': '2k',
                        'file': path.relative_to(ROOT / '.utmp/BasementPreview').as_posix(),
                        'page': 'https://polyhaven.com/a/' + asset, 'url': item['url'],
                        'license': 'CC0-1.0', 'license_url': 'https://polyhaven.com/license',
                        'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data),
                        'authors': info.get('authors'), 'reused': False})
        print(asset, role, 'verified', len(data), 'bytes', flush=True)
    (ART / 'texture-sources.json').write_text(json.dumps(records, indent=2))


if __name__ == '__main__':
    main()
