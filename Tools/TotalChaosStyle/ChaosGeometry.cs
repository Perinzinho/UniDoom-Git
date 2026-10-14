using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Original geometry, authored for UniDoom. No meshes or textures are taken from the reference mod.
public static class ChaosGeometry
{
    public sealed class Builder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        public void Face(params Vector3[] points)
        {
            int start = vertices.Count;
            Vector3 u = (points[1] - points[0]).normalized;
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
            Vector3 v = Vector3.Cross(normal, u);
            foreach(var p in points) { vertices.Add(p); uv.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v))); }
            for(int i = 1; i < points.Length - 1; i++) triangles.AddRange(new[] { start, start + i, start + i + 1 });
        }
        public void Append(Mesh mesh, Matrix4x4 matrix)
        {
            int start = vertices.Count; vertices.AddRange(mesh.vertices.Select(matrix.MultiplyPoint3x4));
            var coords = mesh.uv; uv.AddRange(coords.Length == mesh.vertexCount ? coords : new Vector2[mesh.vertexCount]);
            triangles.AddRange(mesh.triangles.Select(i => i + start));
        }
        public Mesh Mesh(string name)
        {
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(vertices); m.SetUVs(0, uv); m.SetTriangles(triangles, 0); m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds(); return m;
        }
    }
    public static Mesh BevelBox(Vector3 size, float bevel)
    {
        var b = new Builder(); var h = size / 2; float d = Mathf.Min(bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .75f);
        var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
        var hs = new[] { h.x, h.y, h.z };
        // Six inset faces, twelve bevel strips, eight triangular corners; complete closed topology.
        for(int axis = 0; axis < 3; axis++) foreach(int s in new[] { -1, 1 })
        {
            int j = (axis + 1) % 3, k = (axis + 2) % 3;
            Vector3 c = axes[axis] * hs[axis] * s, u = axes[j] * (hs[j] - d), v = axes[k] * (hs[k] - d);
            if(s > 0) b.Face(c-u-v,c+u-v,c+u+v,c-u+v); else b.Face(c-u+v,c+u+v,c+u-v,c-u-v);
        }
        for(int axis = 0; axis < 3; axis++) foreach(int sj in new[] { -1, 1 }) foreach(int sk in new[] { -1, 1 })
        {
            int j = (axis + 1) % 3, k = (axis + 2) % 3;
            Vector3 a = axes[j] * hs[j] * sj + axes[k] * (hs[k]-d) * sk;
            Vector3 c = axes[j] * (hs[j]-d) * sj + axes[k] * hs[k] * sk;
            Vector3 end = axes[axis] * (hs[axis]-d);
            if(sj*sk > 0) b.Face(a-end,c-end,c+end,a+end); else b.Face(a+end,c+end,c-end,a-end);
        }
        foreach(int x in new[] { -1,1 }) foreach(int y in new[] { -1,1 }) foreach(int z in new[] { -1,1 })
        {
            var a = new Vector3(h.x*x,(h.y-d)*y,(h.z-d)*z);
            var c = new Vector3((h.x-d)*x,h.y*y,(h.z-d)*z);
            var e = new Vector3((h.x-d)*x,(h.y-d)*y,h.z*z);
            if(x*y*z > 0) b.Face(a,c,e); else b.Face(e,c,a);
        }
        return b.Mesh("Closed chamfered solid");
    }
    public static Mesh Sweep(IReadOnlyList<Vector3> path, float radius, int sides = 12, float endRadius = -1)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>(); float distance = 0;
        Vector3 prior = Vector3.right;
        for(int i=0;i<path.Count;i++)
        {
            if(i>0) distance += Vector3.Distance(path[i-1],path[i]);
            Vector3 tangent = (path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;
            Vector3 normal = Vector3.ProjectOnPlane(prior,tangent).normalized;
            if(normal.sqrMagnitude < .2f) normal = Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.forward:Vector3.up).normalized;
            prior=normal; Vector3 side=Vector3.Cross(tangent,normal).normalized;
            float r=endRadius<0?radius:Mathf.Lerp(radius,endRadius,i/(float)(path.Count-1));
            for(int j=0;j<=sides;j++) { float a=j*Mathf.PI*2/sides;vertices.Add(path[i]+r*(normal*Mathf.Cos(a)+side*Mathf.Sin(a)));uv.Add(new Vector2(j/(float)sides,distance)); }
        }
        for(int i=0;i<path.Count-1;i++)for(int j=0;j<sides;j++)
        { int a=i*(sides+1)+j,c=a+sides+1;tris.AddRange(new[]{a,a+1,c,a+1,c+1,c}); }
        var m=new Mesh{ name="Curved tubular sweep"};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
    }
    public static List<Vector3> Curve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int segments=24)
    { var p=new List<Vector3>();for(int i=0;i<=segments;i++){float t=i/(float)segments,s=1-t;p.Add(s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d);}return p; }
    public static Mesh Lathe(Vector2[] profile, int sides=24, float dents=0, int seed=1)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int row=0;row<profile.Length;row++)for(int col=0;col<=sides;col++)
        {
            float a=col*Mathf.PI*2/sides,r=profile[row].x;
            r*=1-dents*Mathf.Pow(Mathf.Max(0,Mathf.Sin(a*3+seed+row*.31f)),4);
            v.Add(new Vector3(Mathf.Cos(a)*r,profile[row].y,Mathf.Sin(a)*r));uv.Add(new Vector2(col/(float)sides,profile[row].y));
        }
        for(int i=0;i<profile.Length-1;i++)for(int j=0;j<sides;j++) {int a=i*(sides+1)+j,c=a+sides+1;t.AddRange(new[]{a,c,a+1,a+1,c,c+1});}
        var m=new Mesh{name="Profiled vessel"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
    }
    public static Mesh Cloth(float width,float length,int seed,bool hanging=false)
    {
        int nx=18,nz=30;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
        {
            float u=x/(float)nx,w=z/(float)nz,px=(u-.5f)*width,pz=(w-.5f)*length;
            float silhouette=Mathf.Exp(-px*px/(width*width*.065f))*(.15f+.18f*Mathf.Sin(w*Mathf.PI));
            float folds=.025f*Mathf.Sin(u*49+w*8+seed)+.014f*Mathf.Sin(w*53+u*8);
            float y=hanging? .05f*Mathf.Sin(u*20+w*6):silhouette+folds+.035f;
            v.Add(hanging?new Vector3(px,-w*length,y+folds):new Vector3(px,y,pz));uv.Add(new Vector2(u,w));
        }
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,c=a+nx+1;t.AddRange(new[]{a,c,a+1,a+1,c,c+1});}
        int back=v.Count;v.AddRange(v.ToArray());uv.AddRange(uv.ToArray());var front=t.ToArray();
        for(int i=0;i<front.Length;i+=3)t.AddRange(new[]{front[i+2]+back,front[i+1]+back,front[i]+back});
        var m=new Mesh{name="Folded cloth drape"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
    }
    public static Mesh Ellipsoid(Vector3 size,int seed=0)
    {
        var profile=new Vector2[17];for(int i=0;i<profile.Length;i++){float a=Mathf.PI-i*Mathf.PI/16;profile[i]=new Vector2(Mathf.Sin(a)*.5f,Mathf.Cos(a)*.5f);}
        var mesh=Lathe(profile,24,.035f,seed);var b=new Builder();b.Append(mesh,Matrix4x4.Scale(size));UnityEngine.Object.DestroyImmediate(mesh);return b.Mesh("Smooth irregular ellipsoid");
    }
    public static Mesh Rubble(int seed)
    {
        var random=new System.Random(seed);var b=new Builder();int n=7;var top=new Vector3[n];var bottom=new Vector3[n];
        for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,r=.32f+(float)random.NextDouble()*.18f;top[i]=new Vector3(Mathf.Cos(a)*r,.09f+(float)random.NextDouble()*.16f,Mathf.Sin(a)*r);bottom[i]=new Vector3(top[i].x*.9f,0,top[i].z*.9f);}
        b.Face(top.Reverse().ToArray());b.Face(bottom);for(int i=0;i<n;i++){int j=(i+1)%n;b.Face(bottom[i],top[i],top[j],bottom[j]);}return b.Mesh("Fractured seven-sided masonry");
    }
    public static Mesh ChamferCubes(Mesh source,float maximum,out int count)
    {
        count=0;var b=new Builder();var vs=source.vertices;var sourceUV=source.uv;var tris=source.triangles;
        // Combined Unity primitive boxes retain 24-vertex/36-index slices. Preserve every other slice verbatim.
        for(int offset=0;offset<tris.Length;)
        {
            int length=Math.Min(36,tris.Length-offset);var ids=tris.Skip(offset).Take(length).Distinct().OrderBy(i=>i).ToArray();
            bool box=length==36&&ids.Length==24&&ids.Last()-ids.First()==23;
            Vector3 center=Vector3.zero,x=Vector3.zero,y=Vector3.zero,z=Vector3.zero;Vector3 size=Vector3.zero;
            if(box)
            {
                var points=ids.Select(i=>vs[i]).ToArray();center=points.Aggregate(Vector3.zero,(a,p)=>a+p)/24;
                var normals=new List<Vector3>();
                for(int i=offset;i<offset+36;i+=3){var n=Vector3.Cross(vs[tris[i+1]]-vs[tris[i]],vs[tris[i+2]]-vs[tris[i]]).normalized;if(n.sqrMagnitude>.8f&&!normals.Any(q=>Mathf.Abs(Vector3.Dot(q,n))>.99f))normals.Add(n);}
                box=normals.Count==3&&Mathf.Abs(Vector3.Dot(normals[0],normals[1]))<.01f&&Mathf.Abs(Vector3.Dot(normals[0],normals[2]))<.01f&&Mathf.Abs(Vector3.Dot(normals[1],normals[2]))<.01f;
                if(box){x=normals[0];y=normals[1];z=Vector3.Cross(x,y);size=new Vector3(points.Max(p=>Vector3.Dot(p-center,x))*2,points.Max(p=>Vector3.Dot(p-center,y))*2,points.Max(p=>Vector3.Dot(p-center,z))*2);box=Mathf.Min(size.x,Mathf.Min(size.y,size.z))>.006f;}
            }
            if(box)
            {
                var m=BevelBox(size,Mathf.Min(maximum,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.18f));
                var matrix=Matrix4x4.identity;matrix.SetColumn(0,x);matrix.SetColumn(1,y);matrix.SetColumn(2,z);matrix.SetColumn(3,new Vector4(center.x,center.y,center.z,1));b.Append(m,matrix);UnityEngine.Object.DestroyImmediate(m);count++;offset+=36;
            }
            else
            {
                // A non-box primitive can span many triangles; copying in triples avoids assumptions about its size.
                b.Face(vs[tris[offset]],vs[tris[offset+1]],vs[tris[offset+2]]);offset+=3;
            }
        }
        return b.Mesh("Chamfered " + source.name);
    }
}
