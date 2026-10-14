"""Download the MakeHuman core graphical assets, which the official license identifies as CC0."""
import hashlib
import json
import urllib.request
import io
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TARGET = ROOT / '.utmp/BasementPreview/Preview/ArenaHorror/HumanSource'
REPO = 'https://raw.githubusercontent.com/makehumancommunity/makehuman/master/'


def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'UniDoomAssetImport/1.0'}), timeout=60) as response:
        return response.read()


def download(relative, name):
    path = TARGET / name
    data = path.read_bytes() if path.exists() else fetch(REPO + relative)
    path.write_bytes(data)
    return {'file': name, 'url': REPO + relative, 'license': 'CC0-1.0', 'sha256': hashlib.sha256(data).hexdigest()}


class RemoteZip(io.RawIOBase):
    def __init__(self, url):
        self.url, self.position, self.cache = url, 0, {}
        request=urllib.request.Request(url,method='HEAD',headers={'User-Agent':'UniDoomAssetImport/1.0'})
        with urllib.request.urlopen(request,timeout=60) as response:self.size=int(response.headers['Content-Length'])
    def seekable(self):return True
    def readable(self):return True
    def tell(self):return self.position
    def seek(self,offset,whence=0):
        self.position=offset if whence==0 else self.position+offset if whence==1 else self.size+offset
        return self.position
    def read(self,count=-1):
        if count<0:count=self.size-self.position
        if not count:return b''
        key=(self.position,count)
        if key not in self.cache:
            request=urllib.request.Request(self.url,headers={'User-Agent':'UniDoomAssetImport/1.0','Range':f'bytes={self.position}-{self.position+count-1}'})
            with urllib.request.urlopen(request,timeout=60) as response:
                assert response.status==206, 'Server does not support selective archive downloads'
                self.cache[key]=response.read()
        self.position+=len(self.cache[key]);return self.cache[key]


def skin(records):
    page='https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html'
    html=fetch(page).decode()
    urls=re.findall(r'href=["\']([^"\']+\.zip)["\']',html)
    url=next(u for u in urls if 'files.makehumancommunity.org' in u)
    archive=zipfile.ZipFile(RemoteZip(url))
    candidates=[name for name in archive.namelist() if 'middleage_caucasian_male' in name and name.endswith('.mhmat')]
    assert candidates, 'The verified system skin is absent from the archive'
    name=candidates[0];material=archive.read(name).decode()
    diffuse=re.search(r'^diffuseTexture\s+(.+)$',material,re.M)[1].strip()
    image_name=str(Path(name).parent / diffuse).replace('\\','/')
    if image_name not in archive.namelist():
        image_name=next(n for n in archive.namelist() if n.endswith('/'+Path(diffuse).name))
    data=archive.read(image_name);assert data[:8]==b'\x89PNG\r\n\x1a\n'
    output=ROOT/'.utmp/BasementPreview/Assets/Art/Basement/ArenaHorror/Textures/SkinDiffuse.png';output.write_bytes(data)
    records.append({'file':'Textures/SkinDiffuse.png','url':url,'archive_entry':image_name,'page':page,
                    'license':'CC0-1.0','author':'MakeHuman system assets','sha256':hashlib.sha256(data).hexdigest()})
    print('Verified system skin downloaded from archive entry:',image_name,flush=True)


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    license_text = fetch(REPO + 'LICENSE.md').decode()
    assert 'CC0' in license_text and 'base mesh' in license_text
    (TARGET / 'LICENSE-MakeHuman.md').write_text(license_text)
    records = [download('makehuman/data/3dobjs/base.obj', 'base.obj'),
               download('makehuman/data/rigs/default.mhskel', 'default.mhskel'),
               download('makehuman/data/rigs/default_weights.mhw', 'default_weights.mhw'),
               download('makehuman/data/skins/default.mhmat', 'default.mhmat')]
    url = 'https://api.github.com/repos/makehumancommunity/makehuman/git/trees/master?recursive=1'
    tree = json.loads(fetch(url))['tree']
    interesting = [item['path'] for item in tree if any(word in item['path'].lower() for word in ['weights.mhw', 'default.mhskel']) or
                   ('data/skins/' in item['path'] and item['path'].endswith(('.mhmat', '.png', '.jpg')))]
    (TARGET / 'asset-paths.json').write_text(json.dumps(interesting, indent=2))
    skin(records)
    (TARGET / 'model-sources.json').write_text(json.dumps(records, indent=2))
    print('\n'.join(interesting[:45]), flush=True)
    print('Core human mesh downloaded and source license saved.', flush=True)


if __name__ == '__main__':
    main()
