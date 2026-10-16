import hashlib
import json
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from download_assets import fetch

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Tools/Library/DecayRevision'
WORK=ROOT/'.utmp/LibraryPreview'
manifest=json.loads((OUT/'tree-manifest.json').read_text())
info=json.loads(fetch('https://api.polyhaven.com/info/tree_small_02'))
records=json.loads((OUT/'asset-sources.json').read_text())
for role in ['leaves_diff','leaves_alpha','leaves_nor_gl']:
    item=manifest[role]['1k']['png']
    path=WORK/'Assets/_Project/Scenes/Library/Textures/DecayRevision/tree_small_02'/f'{role}.png'
    path.parent.mkdir(parents=True,exist_ok=True)
    data=fetch(item['url']);assert hashlib.md5(data).hexdigest()==item['md5'];assert data.startswith(b'\x89PNG\r\n\x1a\n');path.write_bytes(data)
    records.append(dict(asset='tree_small_02',role=role,file=path.relative_to(WORK).as_posix(),url=item['url'],page='https://polyhaven.com/a/tree_small_02',license='CC0-1.0',license_url='https://polyhaven.com/license',sha256=hashlib.sha256(data).hexdigest(),authors=info.get('authors')))
(OUT/'asset-sources.json').write_text(json.dumps(records,indent=2))
print('Real leaves and opacity PNGs downloaded and verified')
