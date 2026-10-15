"""Fetch original free PNGs and CC0 models; record source, author, license, hash."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/CylinderDetailPreview'
SCENE = WORK / 'Assets/_Project/Scenes/Cylinder'
ASSETS = ['vintage_radio_transceiver', 'vintage_electric_kettle', 'vintage_microwave', 'russian_food_cans_01', 'wooden_bowl_01']
ICONS = dict(Classroom='1F3EB', Library='1F4DA', Director='1F4BC', Entrance='1F6AA', Electrical='26A1', Storage='1F4E6', Maintenance='1F6E0', Washroom='1F6BB')

def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent':'UniDoomEnvironmentArt/1.0'}), timeout=90) as response:
        return response.read()

def main():
    records=[]
    for asset in ASSETS:
        manifest=json.loads(fetch('https://api.polyhaven.com/files/'+asset))
        info=json.loads(fetch('https://api.polyhaven.com/info/'+asset))
        (ROOT / 'Tools/Cylinder/MacabreRevision' / (asset+'-files.json')).write_text(json.dumps(manifest,indent=2))
        roles=[k for k in manifest if k in ('Diffuse','nor_gl','arm') or k.endswith(('_diff','_nor_gl','_arm'))]
        entries=[('model',manifest['fbx']['1k']['fbx'])]+[(role,manifest[role]['1k']['png']) for role in roles]
        for role,item in entries:
            target=SCENE / (f'Meshes/MacabreRevision/PolyHaven/{asset}/model.fbx' if role=='model' else f'Textures/MacabreRevision/{asset}/{role}.png')
            target.parent.mkdir(parents=True,exist_ok=True)
            data=target.read_bytes() if target.exists() else fetch(item['url'])
            assert hashlib.md5(data).hexdigest()==item['md5'], (asset,role,'MD5 mismatch')
            if role!='model': assert data.startswith(b'\x89PNG\r\n\x1a\n')
            target.write_bytes(data)
            records.append(dict(asset=asset,role=role,file=target.relative_to(WORK).as_posix(),url=item['url'],page='https://polyhaven.com/a/'+asset,license='CC0-1.0',license_url='https://polyhaven.com/license',authors=info.get('authors'),sha256=hashlib.sha256(data).hexdigest(),bytes=len(data)))
        print(asset+': '+str(len(entries))+' verified files',flush=True)
    for label,code in ICONS.items():
        url=f'https://raw.githubusercontent.com/hfg-gmuend/openmoji/master/black/618x618/{code}.png'
        target=SCENE / f'Textures/MacabreRevision/DoorPNGs/{label}.png'
        data=fetch(url)
        assert data.startswith(b'\x89PNG\r\n\x1a\n'),label
        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
        records.append(dict(asset=label,role='Door PNG',file=target.relative_to(WORK).as_posix(),url=url,page='https://openmoji.org/library/emoji-'+code+'/',authors='OpenMoji contributors / HfG Schwaebisch Gmuend',license='CC-BY-SA-4.0',license_url='https://creativecommons.org/licenses/by-sa/4.0/',modifications='None; original PNG mapped onto a separate aged physical sign',sha256=hashlib.sha256(data).hexdigest(),bytes=len(data)))
        print('PNG '+label+' verified',flush=True)
    (ROOT / 'Tools/Cylinder/MacabreRevision/asset-sources.json').write_text(json.dumps(records,indent=2))
    print('DOWNLOAD_COMPLETE',flush=True)

if __name__=='__main__': main()
