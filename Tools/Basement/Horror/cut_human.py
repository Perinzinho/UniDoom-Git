"""Split the reused MakeHuman anatomical mesh into corpses and detached parts."""
import json
from pathlib import Path
import numpy as np
from mesh_io import write

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / '.utmp/BasementPreview/Preview/ArenaHorror/Sources/AnatomicalAdultBody.json'
OUTPUT = SOURCE.parent


def main():
    data=json.loads(SOURCE.read_text())
    vertices=np.asarray([[v['x'],v['y'],v['z']] for v in data['vertices']])
    uv=np.asarray([[v['x'],v['y']] for v in data['uv']])
    normals=np.asarray([[n['x'],n['y'],n['z']] for n in data['normals']])
    triangles=np.asarray(data['triangles'],dtype=np.int32).reshape(-1,3)
    centers=vertices[triangles].mean(axis=1)
    head=centers[:,2]>.51
    arms=(np.abs(centers[:,0])>.20)&(centers[:,2]>-.18)&(centers[:,2]<.50)
    # The posing pass maps the original standing height onto local Z. Start at
    # the upper thighs so the remaining torso has a clean hip-level amputation.
    legs=(centers[:,2]<-.12)&(np.abs(centers[:,0])>.055)
    groups={
        'SeveredHead':head,
        'SeveredArmL':arms&(centers[:,0]<0),
        'SeveredArmR':arms&(centers[:,0]>0),
        'SeveredLegL':legs&(centers[:,0]<0),
        'SeveredLegR':legs&(centers[:,0]>0),
    }
    for name,mask in groups.items():
        assert mask.sum()>500, f'{name} split is unexpectedly small'
        faces=triangles[mask]
        used,inverse=np.unique(faces,return_inverse=True)
        write(name,vertices[used],inverse.reshape(-1,3),uv[used],normals[used])
    parts=np.logical_or.reduce(list(groups.values()))
    for name,remove in [('DismemberedTorsoWest',head|groups['SeveredArmL']|groups['SeveredLegR']),
                        ('DismemberedTorsoEast',head|groups['SeveredArmR']|groups['SeveredLegL'])]:
        faces=triangles[~remove]
        used,inverse=np.unique(faces,return_inverse=True)
        write(name,vertices[used],inverse.reshape(-1,3),uv[used],normals[used])
    report={name:int(mask.sum()) for name,mask in groups.items()}
    report.update({name:int((~mask).sum()) for name,mask in [
        ('DismemberedTorsoWest',head|groups['SeveredArmL']|groups['SeveredLegR']),
        ('DismemberedTorsoEast',head|groups['SeveredArmR']|groups['SeveredLegL'])]})
    (OUTPUT/'dismemberment-metrics.json').write_text(json.dumps(report,indent=2))
    print('Saved anatomical segments:',report,flush=True)


if __name__=='__main__':main()
