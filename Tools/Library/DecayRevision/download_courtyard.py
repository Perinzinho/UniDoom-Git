import hashlib
import json
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from download_assets import fetch

ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'Tools/Library/DecayRevision';WORK=ROOT/'.utmp/LibraryPreview'
asset='abandoned_pathway'
manifest=json.loads(fetch('https://api.polyhaven.com/files/'+asset));info=json.loads(fetch('https://api.polyhaven.com/info/'+asset))
item=manifest['hdri']['2k']['hdr'];data=fetch(item['url']);assert hashlib.md5(data).hexdigest()==item['md5']
path=WORK/'Assets/_Project/Scenes/Library/Textures/DecayRevision/Courtyard/abandoned_pathway.hdr';path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
records=json.loads((OUT/'asset-sources.json').read_text());records.append(dict(asset=asset,role='unmodified courtyard panorama',file=path.relative_to(WORK).as_posix(),url=item['url'],page='https://polyhaven.com/a/'+asset,license='CC0-1.0',license_url='https://polyhaven.com/license',sha256=hashlib.sha256(data).hexdigest(),authors=info.get('authors')))
(OUT/'asset-sources.json').write_text(json.dumps(records,indent=2));print('CC0 courtyard panorama downloaded and verified')
