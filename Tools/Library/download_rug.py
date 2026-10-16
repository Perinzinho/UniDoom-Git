"""Use an explicitly public-domain museum photograph for the reading-room rugs."""
import hashlib
import io
import json
import urllib.request
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
def fetch(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent':'Mozilla/5.0'}), timeout=60).read()

if __name__ == '__main__':
    art = json.loads(fetch('https://collectionapi.metmuseum.org/public/collection/v1/objects/452565'))
    assert art['isPublicDomain'] is True
    url = art['primaryImage']
    data = fetch(url)
    target = ROOT / '.utmp/LibraryPreview/Assets/_Project/Scenes/Library/Textures/MuseumRug/Carpet.png'
    target.parent.mkdir(parents=True, exist_ok=True)
    image = Image.open(io.BytesIO(data)).convert('RGB')
    image.thumbnail((2048, 2048))
    image.save(target)
    record = dict(title=art['title'], artist=art.get('artistDisplayName'), date=art.get('objectDate'),
                  page=art['objectURL'], url=url, license='CC0-1.0',
                  license_url='https://www.metmuseum.org/policies/image-resources',
                  file=target.relative_to(ROOT / '.utmp/LibraryPreview').as_posix(),
                  conversion='Museum JPEG decoded to PNG without painted or generated content',
                  sha256=hashlib.sha256(target.read_bytes()).hexdigest())
    (ROOT / 'Tools/Library/rug-source.json').write_text(json.dumps(record, indent=2))
    print(record)
