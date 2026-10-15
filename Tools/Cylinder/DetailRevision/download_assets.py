"""Download verified CC0 Poly Haven FBX models and PNG PBR maps."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/CylinderDetailPreview'
SCENE = WORK / 'Assets/_Project/Scenes/Cylinder'
ASSETS = ['utility_box_02', 'barrel_03', 'plastic_monobloc_chair_01']


def fetch(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'UniDoomEnvironmentArt/1.0'})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()


def main():
    records = []
    for asset in ASSETS:
        manifest = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
        info = json.loads(fetch('https://api.polyhaven.com/info/' + asset))
        entries = [('model', manifest['fbx']['1k']['fbx'])]
        entries += [(role, manifest[role]['1k']['png']) for role in ['Diffuse', 'nor_gl', 'arm']]
        for role, item in entries:
            if role == 'model':
                target = SCENE / f'Meshes/DetailRevision/PolyHaven/{asset}/model.fbx'
            else:
                target = SCENE / f'Textures/DetailRevision/{asset}/{role}.png'
            target.parent.mkdir(parents=True, exist_ok=True)
            data = target.read_bytes() if target.exists() else fetch(item['url'])
            if hashlib.md5(data).hexdigest() != item['md5']:
                raise ValueError('Download hash mismatch: ' + asset + '/' + role)
            if role != 'model' and not data.startswith(b'\x89PNG\r\n\x1a\n'):
                raise ValueError('Invalid PNG: ' + item['url'])
            target.write_bytes(data)
            records.append(dict(asset=asset, role=role, file=target.relative_to(WORK).as_posix(),
                                url=item['url'], page='https://polyhaven.com/a/' + asset,
                                license='CC0-1.0', license_url='https://polyhaven.com/license',
                                authors=info.get('authors'), sha256=hashlib.sha256(data).hexdigest(),
                                bytes=len(data)))
        print(asset + ': FBX and three PNG maps verified', flush=True)
    (ROOT / 'Tools/Cylinder/DetailRevision/asset-sources.json').write_text(json.dumps(records, indent=2))


if __name__ == '__main__':
    main()
