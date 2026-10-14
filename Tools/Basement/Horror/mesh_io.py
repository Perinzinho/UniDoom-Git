import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
TARGET = ROOT / '.utmp/BasementPreview/Preview/ArenaHorror/Sources'


def write(name, vertices, triangles, uv, normals=None):
    vertices=np.asarray(vertices);triangles=np.asarray(triangles,dtype=np.int32)
    assert np.isfinite(vertices).all() and triangles.max()<len(vertices)
    data={'vertices':[dict(zip('xyz',map(float,p))) for p in np.round(vertices,6)],
          'uv':[dict(zip('xy',map(float,p))) for p in np.round(uv,6)],'triangles':triangles.ravel().tolist()}
    if normals is not None:
        data['normals']=[dict(zip('xyz',map(float,p))) for p in np.round(normals,6)]
    TARGET.mkdir(parents=True,exist_ok=True)
    (TARGET/(name+'.json')).write_text(json.dumps(data,separators=(',',':')))
    print(name,len(vertices),'vertices;',len(triangles),'triangles',flush=True)
