using UnityEditor;
using UnityEngine;

public static class LibraryFinish
{
    public static void Run()
    {
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            LibraryBuilder.SplitLightingSurfaces(root.transform.Find("Abandoned Library Interior"));
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                if(f.name!="Worn reading-room carpet")continue;
                var m=f.sharedMesh;var uv=m.uv;var v=m.vertices;var n=m.normals;
                for(int i=0;i<uv.Length;i++)if(n[i].y>.5f)uv[i]=new Vector2(.04f+.92f*(v[i].z/5.05f+.5f),.033f+.935f*(.5f-v[i].x/10.25f));
                m.uv=uv;EditorUtility.SetDirty(m);
            }
            PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();LibraryVerifier.VerifyAndRender();
    }
}
