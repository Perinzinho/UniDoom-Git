"""Download selected CC0 PNGs using official Poly Haven file manifests."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TARGET = ROOT / '.utmp/BasementPreview/Assets/Art/ClassRoom/Surface/Textures'
ASSETS = ['concrete_wall_006', 'rusty_metal_02', 'wood_table_worn', 'worn_tile_floor', 'rock_face']


def fetch(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'UniDoomMaterialImport/1.0'})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()


def main():
    records = []
    for asset in ASSETS:
        files = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
        info = json.loads(fetch('https://api.polyhaven.com/info/' + asset))
        directory = TARGET / asset
        directory.mkdir(parents=True, exist_ok=True)
        for role in ['Diffuse', 'nor_gl', 'arm', 'Displacement']:
            resolution = '2k' if asset == 'concrete_wall_006' else '1k'
            item = files[role][resolution]['png']
            path = directory / (role + '.png')
            data = fetch(item['url']) if not path.exists() else path.read_bytes()
            assert data[:8] == b'\x89PNG\r\n\x1a\n', item['url']
            if item.get('md5'):
                assert hashlib.md5(data).hexdigest() == item['md5'], path
            path.write_bytes(data)
            records.append({'asset': asset, 'role': role, 'resolution': resolution,
                            'file': str(path.relative_to(TARGET.parent)).replace('\\', '/'),
                            'page': 'https://polyhaven.com/a/' + asset, 'url': item['url'],
                            'license': 'CC0-1.0', 'license_url': 'https://polyhaven.com/license',
                            'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data),
                            'authors': info.get('authors'), 'physical_size': info.get('physical_size')})
        print(asset, 'downloaded and verified')
    (TARGET.parent / 'texture-sources.json').write_text(json.dumps(records, indent=2))


if __name__ == '__main__':
    main()
