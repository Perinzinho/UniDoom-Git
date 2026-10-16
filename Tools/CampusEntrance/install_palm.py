import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
WORK = ROOT / '.utmp/EntranceRebuild'
SOURCE = OUT / 'Downloads/Palm'
BASE = WORK / 'Assets/_Project/Scenes/EntranceGarden'
records = []
model = BASE / 'Models/CampusReference/PalmTree/palm_tree.obj'
model.parent.mkdir(parents=True, exist_ok=True)
shutil.copy2(SOURCE / 'palm_tree.obj', model)
for name in ['diffus', 'normal', 'specular']:
    path = BASE / f'Textures/CampusReference/PalmTree/{name}.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    image = Image.open(SOURCE / (name + '.tga'))
    image.save(path)
    records.append(dict(file=path.relative_to(WORK).as_posix(), source=name+'.tga', mode=image.mode, size=image.size, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(OUT / 'palm-conversion.json').write_text(json.dumps(records, indent=2))
print('Palm mesh and original TGA channels installed as PNG.')
