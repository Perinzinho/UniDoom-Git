"""Author a continuous adult silhouette and gravity-shaped mortuary cloth, not textures."""
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
TARGET = ROOT / '.utmp/BasementPreview/Preview/ArenaHorror/Sources'
PARTS = [
    ((0, .16, .32), (.235, .135, .235)),
    ((0, .15, .12), (.205, .12, .25)),
    ((.015, .13, -.13), (.16, .10, .22)),
    ((.008, .135, -.31), (.18, .11, .16)),
    ((.014, .19, .565), (.062, .067, .10)),
    ((.025, .20, .755), (.105, .13, .137)),
    ((.029, .308, .725), (.021, .038, .033)),
    ((.012, .238, .648), (.068, .082, .063)),
    ((-.10, .11, -.53), (.081, .094, .245)),
    ((-.10, .104, -.745), (.068, .072, .077)),
    ((-.09, .087, -.93), (.053, .07, .20)),
    ((-.10, .105, -1.12), (.055, .075, .105)),
    ((.12, .115, -.52), (.083, .094, .23)),
    ((.16, .107, -.72), (.066, .073, .085)),
    ((.175, .089, -.91), (.053, .07, .20)),
    ((.19, .105, -1.10), (.055, .075, .105)),
    ((-.27, .13, .31), (.066, .068, .18)),
    ((-.35, .075, .075), (.058, .055, .17)),
    ((-.405, .030, -.095), (.038, .028, .064)),
    ((.275, .135, .29), (.067, .067, .17)),
    ((.35, .075, .045), (.057, .05, .18)),
    ((.405, .030, -.15), (.037, .029, .067)),
]
for side in [-1, 1]:
    for finger in range(4):
        PARTS.append(((side * (.388 + finger * .015), .020, -.180 - (side+1)*.028),
                      (.0075, .012, .042 - abs(finger-1.5)*.004)))
    PARTS.append(((side*.368, .032, -.13-(side+1)*.027), (.014, .017, .03)))


def field(points):
    distance = np.full(points.shape[:-1], 10., dtype=np.float64)
    for center, radii in PARTS:
        r = np.array(radii)
        q = (points - np.array(center)) / r
        d = (np.sqrt(np.sum(q*q, axis=-1)) - 1) * r.min()
        k = .024 if r.min() > .025 else .005
        h = np.clip(.5 + .5*(d-distance)/k, 0, 1)
        distance = d*(1-h) + distance*h - k*h*(1-h)
    # Shallow impressions read as a covered face beneath the winding, rather than a featureless oval.
    for center,radii in [((-.010,.325,.769),(.023,.018,.018)),((.060,.325,.769),(.023,.018,.018)),
                         ((.025,.298,.664),(.041,.012,.007))]:
        r=np.asarray(radii);q=(points-np.asarray(center))/r
        cavity=(np.sqrt(np.sum(q*q,axis=-1))-1)*r.min()
        distance=np.maximum(distance,-cavity)
    return distance


def gradient(points):
    result = np.stack([field(points+np.eye(3)[axis]*.0008)-field(points-np.eye(3)[axis]*.0008)
                       for axis in range(3)], axis=-1)
    return result / np.maximum(np.linalg.norm(result, axis=-1, keepdims=True), 1e-10)


def write(name, vertices, triangles, uv, normals=None):
    vertices = np.asarray(vertices)
    triangles = np.asarray(triangles, dtype=np.int32)
    assert np.isfinite(vertices).all() and triangles.max() < len(vertices)
    data = {'vertices': [dict(zip('xyz', map(float, p))) for p in np.round(vertices, 6)],
            'uv': [dict(zip('xy', map(float, p))) for p in np.round(uv, 6)], 'triangles': triangles.ravel().tolist()}
    if normals is not None:
        data['normals'] = [dict(zip('xyz', map(float, p))) for p in np.round(normals, 6)]
    TARGET.mkdir(parents=True, exist_ok=True)
    (TARGET / (name + '.json')).write_text(json.dumps(data, separators=(',', ':')))
    print(name, len(vertices), 'vertices;', len(triangles), 'triangles', flush=True)


def body(step=.014, name='AnatomicalWrappedAdult'):
    axes = [np.arange(-.500327, .51, step), np.arange(-.024913, .40, step), np.arange(-1.270213, .94, step)]
    points = np.stack(np.meshgrid(*axes, indexing='ij'), axis=-1)
    values = field(points)
    offsets = np.array([[0,0,0],[1,0,0],[1,1,0],[0,1,0],[0,0,1],[1,0,1],[1,1,1],[0,1,1]])
    shape = np.array(values.shape)-1
    cells = np.stack(np.meshgrid(*[np.arange(n) for n in shape], indexing='ij'), axis=-1).reshape(-1, 3)
    ids = cells[:, None, :] + offsets[None, :, :]
    cube_values = values[ids[:,:,0], ids[:,:,1], ids[:,:,2]]
    crossing = (cube_values.min(axis=1) < 0) & (cube_values.max(axis=1) >= 0)
    ids, cube_values = ids[crossing], cube_values[crossing]
    cube_points = points[ids[:,:,0], ids[:,:,1], ids[:,:,2]]
    tetrahedra = [[0,5,1,6],[0,1,2,6],[0,2,3,6],[0,3,7,6],[0,7,4,6],[0,4,5,6]]
    edges = [(0,1),(0,2),(0,3),(1,2),(1,3),(2,3)]
    faces = []
    for corners in tetrahedra:
        p, d = cube_points[:,corners], cube_values[:,corners]
        mask = (d.min(axis=1) < 0) & (d.max(axis=1) >= 0)
        for ps, ds in zip(p[mask], d[mask]):
            polygon = [ps[a]+(ps[b]-ps[a])*(ds[a]/(ds[a]-ds[b])) for a,b in edges if (ds[a]<0)!=(ds[b]<0)]
            q = np.asarray(polygon); center=q.mean(axis=0)
            # The tetrahedron's scalar gradient gives the outward winding without costly SDF resampling.
            normal = np.linalg.solve(ps[1:]-ps[0], ds[1:]-ds[0]); normal/=np.linalg.norm(normal)
            u=q[0]-center;u/=np.linalg.norm(u);v=np.cross(normal,u)
            angles=np.arctan2((q-center)@v,(q-center)@u);q=q[np.argsort(angles)]
            for j in range(1,len(q)-1):faces.append((q[0],q[j],q[j+1]))
    raw=np.asarray(faces).reshape(-1,3)
    vertices, inverse=np.unique(np.round(raw,9),axis=0,return_inverse=True)
    triangles=inverse.reshape(-1,3)
    normals=gradient(vertices)
    # Closed manifold check catches clipped limbs and extraction cracks.
    edge_ids=np.sort(np.concatenate([triangles[:,[0,1]],triangles[:,[1,2]],triangles[:,[2,0]]]),axis=1)
    _, counts=np.unique(edge_ids,axis=0,return_counts=True)
    assert np.all(counts==2), 'Body surface has open edges: ' + str(np.unique(counts, return_counts=True))
    # The covered torso is retained in the source silhouette but not submitted to the GPU.
    exposed=(vertices[:,2]>.48) | (np.abs(vertices[:,0])>.235)
    triangles=triangles[np.any(exposed[triangles],axis=1)]
    used, remap=np.unique(triangles.ravel(), return_inverse=True)
    write(name,vertices[used],remap.reshape(-1,3),vertices[used][:,[0,2]]*2,normals[used])


def cloth(nx=84,nz=152,name='GravityDrapedMortuaryLinen'):
    x,z=np.meshgrid(np.linspace(-.665,.665,nx+1),np.linspace(-1.285,.475,nz+1),indexing='xy')
    # Fold the side corners inward around the wrists, exposing their anatomy and winding.
    pull=.30*np.exp(-((z+.14)/.17)**2)
    x=x-np.sign(x)*pull*np.clip(np.abs(x)/.665,0,1)**1.5
    top=np.full_like(x,.014)
    for center,radii in PARTS[:22]:
        cx,cy,cz=center;rx,ry,rz=radii
        q=((x-cx)/rx)**2+((z-cz)/rz)**2
        top=np.maximum(top,np.where(q<1,cy+ry*np.sqrt(np.maximum(0,1-q)),.014))
    # A sheet bridges the limbs; soft envelope and diagonals converge at the restraint points.
    body_envelope=.25*np.exp(-((x/.29)**4))*np.exp(-((z-.16)/.5)**4)
    pelvis=.205*np.exp(-((x-.01)/.26)**4)*np.exp(-((z+.34)/.20)**2)
    legs=.12*np.exp(-((x-.025)/.24)**4)*np.exp(-((z+.84)/.44)**4)
    top=np.maximum(top,np.maximum(body_envelope,np.maximum(pelvis,legs)))
    kernel=np.exp(-np.arange(-5,6)**2/(2*2.4**2));kernel/=kernel.sum()
    for axis in [0,1]:
        top=np.apply_along_axis(lambda row:np.convolve(np.pad(row,5,mode='edge'),kernel,mode='valid'),axis,top)
    y=top+.011+.012*np.sin(36*x+11*z)+.007*np.sin(66*x-21*z)
    overhang=np.clip((np.abs(x)-.49)/.175,0,1)
    y=y*(1-overhang)-.29*overhang+.010*np.sin(z*29+x*17)*overhang
    x=x+.006*np.sin(z*53)*np.clip((np.abs(x)-.58)/.08,0,1)
    vertices=np.stack([x,y,z],axis=-1).reshape(-1,3)
    triangles=[]
    for iz in range(nz):
        for ix in range(nx):
            a=iz*(nx+1)+ix;b=a+nx+1
            if (ix in [0,nx-1] and iz%29 in [0,1,2]) or (iz==nz-1 and ix%17 in [0,1]):continue
            triangles.extend([[a,b,a+1],[a+1,b,b+1]])
    write(name,vertices,triangles,vertices[:,[0,2]]*2)


if __name__ == '__main__':
    body()
    cloth()
    body(.031,'WrappedAdultLOD')
    cloth(28,52,'DrapedLinenLOD')
