"""Import a real CC0 weave for the new mortuary cloth."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / '.utmp/BasementPreview/Assets/Art/Basement/ArenaHorror'


def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'UniDoomMaterialImport/1.0'}), timeout=60) as response:
        return response.read()


def main():
    asset = 'terlenka'
    files = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
    info = json.loads(fetch('https://api.polyhaven.com/info/' + asset))
    records = []
    for role in ['Diffuse', 'nor_gl', 'arm']:
        item = files[role]['1k']['png']
        path = ART / 'Textures' / (role + '.png')
        path.parent.mkdir(parents=True, exist_ok=True)
        data = path.read_bytes() if path.exists() else fetch(item['url'])
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        if item.get('md5'):
            assert hashlib.md5(data).hexdigest() == item['md5']
        path.write_bytes(data)
        records.append({'asset': asset, 'role': role, 'resolution': '1k', 'file': 'Textures/' + path.name,
                        'page': 'https://polyhaven.com/a/' + asset, 'url': item['url'], 'license': 'CC0-1.0',
                        'license_url': 'https://polyhaven.com/license', 'authors': info.get('authors'),
                        'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})
        print(role, 'verified', len(data), 'bytes', flush=True)
    dirt_asset = 'dirty_carpet'
    dirt_files = json.loads(fetch('https://api.polyhaven.com/files/' + dirt_asset))
    dirt_info = json.loads(fetch('https://api.polyhaven.com/info/' + dirt_asset))
    item = dirt_files['Diffuse']['1k']['png']
    path = ART / 'Textures/DirtDetail.png'
    data = path.read_bytes() if path.exists() else fetch(item['url'])
    assert data[:8] == b'\x89PNG\r\n\x1a\n'
    if item.get('md5'):
        assert hashlib.md5(data).hexdigest() == item['md5']
    path.write_bytes(data)
    records.append({'asset': dirt_asset, 'role': 'Diffuse detail', 'resolution': '1k', 'file': 'Textures/DirtDetail.png',
                    'page': 'https://polyhaven.com/a/' + dirt_asset, 'url': item['url'], 'license': 'CC0-1.0',
                    'license_url': 'https://polyhaven.com/license', 'authors': dirt_info.get('authors'),
                    'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})
    print('Dirt detail verified', len(data), 'bytes', flush=True)
    (ART / 'texture-sources.json').write_text(json.dumps(records, indent=2))


if __name__ == '__main__':
    main()
