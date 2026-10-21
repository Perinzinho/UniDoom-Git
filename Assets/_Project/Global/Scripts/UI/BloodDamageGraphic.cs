using UnityEngine;
using UnityEngine.UI;

/// <summary>Soft red screen edges with irregular blood splats; keeps the centre readable.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BloodDamageGraphic : MaskableGraphic
{
    private int pattern;

    public void SetPattern(int value)
    {
        pattern = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        var wash = new Color(.6f, .005f, .015f, .07f);
        AddQuad(mesh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin), wash, wash, wash, wash);

        Vector2[] outer = { new Vector2(rect.xMin,rect.yMin), new Vector2(rect.xMin,rect.yMax),
            new Vector2(rect.xMax,rect.yMax), new Vector2(rect.xMax,rect.yMin) };
        Rect centre = new Rect(rect.xMin + rect.width*.19f, rect.yMin + rect.height*.22f, rect.width*.62f, rect.height*.56f);
        Vector2[] inner = { new Vector2(centre.xMin,centre.yMin), new Vector2(centre.xMin,centre.yMax),
            new Vector2(centre.xMax,centre.yMax), new Vector2(centre.xMax,centre.yMin) };
        var edge = new Color(.38f,.002f,.012f,.8f);
        var clear = new Color(edge.r,edge.g,edge.b,0);
        for (int side=0; side<4; side++)
        {
            int next=(side+1)%4;
            AddQuad(mesh, outer[side],outer[next],inner[next],inner[side],edge,edge,clear,clear);
        }

        // Local RNG never changes enemy spawning, loot rolls or weapon spread.
        var random = new System.Random(unchecked(7919 + pattern*104729));
        float unit = Mathf.Min(rect.width,rect.height);
        for (int i=0; i<62; i++)
        {
            int side=i%4;
            float along=.02f+(float)random.NextDouble()*.96f;
            float depth=.02f+(float)random.NextDouble()*.17f;
            Vector2 uv=side==0?new Vector2(depth,along):side==1?new Vector2(1-depth,along):
                side==2?new Vector2(along,depth):new Vector2(along,1-depth);
            Vector2 position=new Vector2(rect.xMin+uv.x*rect.width,rect.yMin+uv.y*rect.height);
            bool drop=i>=26;
            float radius=unit*(drop?.0025f+(float)random.NextDouble()*.007f:.016f+(float)random.NextDouble()*.04f);
            float stretch=drop?1f+(float)random.NextDouble()*1.8f:.7f+(float)random.NextDouble()*.8f;
            float phase=(float)random.NextDouble()*Mathf.PI*2;
            AddSplat(mesh,position,radius,stretch,phase,unit,
                new Color(.32f+(float)random.NextDouble()*.18f,.002f,.008f,.5f+(float)random.NextDouble()*.28f));
        }
    }

    private static void AddSplat(VertexHelper mesh, Vector2 position, float radius, float stretch, float phase, float unit, Color tint)
    {
        const int segments=24;
        int first=mesh.currentVertCount;
        AddVertex(mesh,position,tint);
        var feather=tint;feather.a=0;
        for(int i=0;i<segments;i++)
        {
            float angle=i*(Mathf.PI*2/segments);
            float size=radius*(1f+.23f*Mathf.Sin(angle*3+phase)+.12f*Mathf.Sin(angle*7-phase));
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)*stretch);
            AddVertex(mesh,position+direction*size,tint);
            AddVertex(mesh,position+direction*(size+Mathf.Max(1.1f,unit*.0014f)),feather);
        }
        for(int i=0;i<segments;i++)
        {
            int a=first+1+i*2,b=first+1+(i+1)%segments*2;
            mesh.AddTriangle(first,a,b);
            mesh.AddTriangle(a,a+1,b+1);
            mesh.AddTriangle(a,b+1,b);
        }
    }

    private static void AddVertex(VertexHelper mesh,Vector2 position,Color tint)
        => mesh.AddVert(new Vector3(position.x,position.y,0),tint,Vector2.zero);

    private static void AddQuad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ca,Color cb,Color cc,Color cd)
    {
        int first=mesh.currentVertCount;
        AddVertex(mesh,a,ca);AddVertex(mesh,b,cb);AddVertex(mesh,c,cc);AddVertex(mesh,d,cd);
        mesh.AddTriangle(first,first+1,first+2);
        mesh.AddTriangle(first,first+2,first+3);
    }
}
