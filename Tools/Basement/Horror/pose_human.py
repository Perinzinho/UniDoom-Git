"""Pose the CC0 MakeHuman anatomical mesh and drape the sheet over its actual surface."""
import json
from pathlib import Path
import numpy as np
from mesh_io import write

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / '.utmp/BasementPreview/Preview/ArenaHorror/HumanSource'
OUTPUT = ROOT / '.utmp/BasementPreview/Preview/ArenaHorror/Sources'


def load():
    vertices,uv,faces=[],[],[]
    group=''
    for line in (SOURCE/'base.obj').read_text().splitlines():
        if line.startswith('v '):vertices.append([float(x) for x in line.split()[1:]])
        elif line.startswith('vt '):uv.append([float(x) for x in line.split()[1:3]])
        elif line.startswith('g '):group=line[2:]
        elif line.startswith('f ') and group=='body':
            face=[tuple(int(x)-1 for x in p.split('/')[:2]) for p in line.split()[1:]]
            faces.extend([[face[0],face[i],face[i+1]] for i in range(1,len(face)-1)])
    return np.asarray(vertices),np.asarray(uv),np.asarray(faces,dtype=np.int32)


def pose(v,faces):
    skeleton=json.loads((SOURCE/'default.mhskel').read_text())
    weights=json.loads((SOURCE/'default_weights.mhw').read_text())['weights']
    result=v.copy()
    def descendants(name):
        found={name};old=-1
        while old!=len(found):
            old=len(found)
            found.update(n for n,b in skeleton['bones'].items() if b['parent'] in found)
        return found
    def joint(bone,endpoint):return v[skeleton['joints'][skeleton['bones'][bone][endpoint]]].mean(0)
    def rotate(bone,axis,angle,pivot):
        weight=np.zeros(len(v))
        for name in descendants(bone):
            if name in weights:
                data=np.asarray(weights[name]);np.add.at(weight,data[:,0].astype(int),data[:,1])
        weight=np.clip(weight,0,1)
        a=np.deg2rad(angle);c,s=np.cos(a),np.sin(a)
        r=np.eye(3)
        if axis==0:r=np.array([[1,0,0],[0,c,-s],[0,s,c]])
        if axis==1:r=np.array([[c,0,s],[0,1,0],[-s,0,c]])
        if axis==2:r=np.array([[c,-s,0],[s,c,0],[0,0,1]])
        transformed=(result-pivot)@r.T+pivot
        result[:]=result*(1-weight[:,None])+transformed*weight[:,None]
    for side in ['L','R']:
        elbow=joint('lowerarm01.'+side,'head');wrist=joint('lowerarm02.'+side,'tail')
        d=wrist-elbow;angle=np.rad2deg(np.arctan2(d[2],-d[1]))
        rotate('lowerarm01.'+side,0,angle,elbow)
        rotate('upperarm01.'+side,2,-30 if side=='L' else 30,joint('upperarm01.'+side,'head'))
    rotate('neck02',1,-13,joint('neck02','head'))
    used=np.unique(faces[:,:,0]);minimum=v[used,1].min();maximum=v[used,1].max()
    scale=1.97/(maximum-minimum)
    posed=np.stack([-result[:,0]*scale,result[:,2]*scale,(result[:,1]-minimum)*scale-1.075],axis=1)
    posed[:,1]-=posed[used,1].min()-.012
    triangles=faces[:,:,0]
    n=np.zeros_like(posed);a,b,c=posed[triangles[:,0]],posed[triangles[:,1]],posed[triangles[:,2]]
    cross=np.cross(b-a,c-a)
    for i in range(3):np.add.at(n,triangles[:,i],cross)
    n/=np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-12)
    return posed,n


def mesh(posed,normals,uv,faces):
    # Keep the complete head and arms; all omitted torso geometry remains covered by the sheet.
    visible=(posed[:,2]>.485)|(np.abs(posed[:,0])>.19)
    faces=faces[np.any(visible[faces[:,:,0]],axis=1)]
    pairs,inverse=np.unique(faces.reshape(-1,2),axis=0,return_inverse=True)
    vertices=posed[pairs[:,0]];coords=uv[pairs[:,1]];ns=normals[pairs[:,0]];tri=inverse.reshape(-1,3)
    write('AnatomicalAdultBody',vertices,tri,coords,ns)
    keys=np.column_stack([np.floor(vertices/.025),np.floor(coords/.04)]).astype(int)
    _,ids=np.unique(keys,axis=0,return_inverse=True);count=np.bincount(ids)
    low=np.zeros((len(count),3));luv=np.zeros((len(count),2));ln=np.zeros_like(low)
    np.add.at(low,ids,vertices);np.add.at(luv,ids,coords);np.add.at(ln,ids,ns)
    low/=count[:,None];luv/=count[:,None];ln/=np.maximum(np.linalg.norm(ln,axis=1,keepdims=True),1e-12)
    lt=ids[tri];lt=lt[(lt[:,0]!=lt[:,1])&(lt[:,1]!=lt[:,2])&(lt[:,0]!=lt[:,2])]
    write('AdultCorpseLOD',low,lt,luv,ln)


def sheet(posed,triangles,nx,nz,name):
    xs=np.linspace(-.665,.665,nx+1);zs=np.linspace(-1.285,.475,nz+1)
    top=np.full((nz+1,nx+1),.014)
    for tri in triangles:
        p=posed[tri];xz=p[:,[0,2]]
        lo=xz.min(0);hi=xz.max(0)
        if hi[0]<xs[0] or lo[0]>xs[-1] or hi[1]<zs[0] or lo[1]>zs[-1]:continue
        ix0=max(0,np.searchsorted(xs,lo[0])-1);ix1=min(nx,np.searchsorted(xs,hi[0]))
        iz0=max(0,np.searchsorted(zs,lo[1])-1);iz1=min(nz,np.searchsorted(zs,hi[1]))
        a,b=xz[1]-xz[0],xz[2]-xz[0];det=a[0]*b[1]-a[1]*b[0]
        if abs(det)<1e-10:continue
        x,z=np.meshgrid(xs[ix0:ix1+1],zs[iz0:iz1+1]);q=np.stack([x-xz[0,0],z-xz[0,1]],axis=-1)
        u=(q[:,:,0]*b[1]-q[:,:,1]*b[0])/det;w=(a[0]*q[:,:,1]-a[1]*q[:,:,0])/det
        hit=(u>=0)&(w>=0)&(u+w<=1)
        y=p[0,1]+u*(p[1,1]-p[0,1])+w*(p[2,1]-p[0,1])
        window=top[iz0:iz1+1,ix0:ix1+1];window[:]=np.maximum(window,np.where(hit,y,.014))
    # Cloth bridges small anatomical gaps, then folds back around the exposed hands.
    for _ in range(5):
        padded=np.pad(top,1,mode='edge')
        top=np.maximum(top,np.maximum.reduce([padded[:-2,1:-1],padded[2:,1:-1],padded[1:-1,:-2],padded[1:-1,2:]])-.009)
    kernel=np.exp(-np.arange(-4,5)**2/(2*1.7**2));kernel/=kernel.sum()
    for axis in [0,1]:top=np.apply_along_axis(lambda r:np.convolve(np.pad(r,4,mode='edge'),kernel,mode='valid'),axis,top)
    x,z=np.meshgrid(xs,zs);x-=np.sign(x)*(.42*np.exp(-((z+.08)/.20)**2))*np.clip(np.abs(x)/.665,0,1)**1.5
    for row in range(nz+1):top[row]=np.interp(x[row],xs,top[row])
    folds=.007*np.sin(x*48+z*11)+.004*np.sin(x*83-z*21)
    y=top+.009+folds;overhang=np.clip((np.abs(x)-.49)/.175,0,1)
    y=y*(1-overhang)-.27*overhang+.006*np.sin(z*31)*overhang
    vertices=np.stack([x,y,z],axis=-1).reshape(-1,3);tris=[]
    for iz in range(nz):
        for ix in range(nx):
            a=iz*(nx+1)+ix;b=a+nx+1
            if ix in [0,nx-1] and iz%31<2:continue
            tris.extend([[a,b,a+1],[a+1,b,b+1]])
    write(name,vertices,tris,vertices[:,[0,2]]*2)


def main():
    v,uv,faces=load();posed,normals=pose(v,faces)
    mesh(posed,normals,uv,faces)
    sheet(posed,faces[:,:,0],84,152,'GravityDrapedMortuaryLinen')
    sheet(posed,faces[:,:,0],28,52,'DrapedLinenLOD')
    output=OUTPUT/'pose-metrics.json'
    body=posed[np.unique(faces[:,:,0])]
    output.write_text(json.dumps({'bounds_min':body.min(0).tolist(),'bounds_max':body.max(0).tolist(),
        'source':'CC0 MakeHuman core mesh','pose':'supine adult; forearms flattened, arms relaxed, head turned 13 degrees'},indent=2))


if __name__=='__main__':main()
