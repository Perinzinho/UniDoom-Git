using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
public static class CylinderMacabrePolish
{
    public static void Run()
    {
        FixImageMaterials();
        var room=PrefabUtility.LoadPrefabContents(CylinderMacabreBuilder.Prefab);
        try
        {
            var transforms=room.GetComponentsInChildren<Transform>(true);
            var radio=transforms.Single(t=>t.name=="Poly Haven military radio and accessories");radio.localRotation=Quaternion.Euler(0,180,0);
            foreach(var t in transforms.Where(t=>t.name=="Old enamel microwave"))t.localRotation=Quaternion.identity;
            var writing=transforms.Single(t=>t.name=="Wall residue story - bay 29").GetChild(0);
            writing.localPosition=new Vector3(0,3.65f,-.263f);writing.localScale=new Vector3(1,.50f/writing.GetComponent<MeshFilter>().sharedMesh.bounds.size.y,1);
            var atrium=room.transform.Find("Abandoned circular atrium");var entrance=atrium.Find("Service bay 1");
            var blood=transforms.Single(t=>t.name=="Wall residue story - bay 9");
            blood.position=entrance.position;blood.rotation=entrance.rotation;
            var bloodQuad=blood.GetChild(0);bloodQuad.localPosition=new Vector3(.80f,1.18f,-.263f);bloodQuad.localScale=new Vector3(1.0f/.54f,1.25f/1.35f,1);
            var drawing=transforms.Single(t=>t.name=="Wall residue story - bay 19");
            drawing.position=entrance.position;drawing.rotation=entrance.rotation;drawing.GetChild(0).localPosition=new Vector3(-1.18f,2.24f,-.263f);
            BatchSignBackings(atrium);
            PrefabUtility.SaveAsPrefabAsset(room,CylinderMacabreBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(room);}
        AssetDatabase.SaveAssets();CylinderMacabreVerifier.Capture();Debug.Log("CYLINDER_MACABRE_POLISH_PASS");
    }

    public static void FinalizeAndVerify()
    {
        Run();
        var main=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../.."));
        var info=new System.Diagnostics.ProcessStartInfo("python","Tools/Cylinder/MacabreRevision/install.py"){WorkingDirectory=main,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        using(var p=System.Diagnostics.Process.Start(info))
        {var stdout=p.StandardOutput.ReadToEnd();var stderr=p.StandardError.ReadToEnd();p.WaitForExit();Debug.Log(stdout);if(p.ExitCode!=0)throw new Exception(stderr);}
        AssetDatabase.ImportAsset(CylinderMacabreBuilder.Prefab,ImportAssetOptions.ForceUpdate);
        CylinderMacabreVerifier.Verify();
    }

    static void FixImageMaterials()
    {
        const string root="Assets/_Project/Scenes/Cylinder";
        foreach(var name in new[]{"Classroom","Library","Director","Entrance","Electrical","Storage","Maintenance","Washroom","Residue"})
        {
            bool clip=name!="Residue";
            var m=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/MacabreRevision/"+(clip?"Downloaded PNG - "+name:"Embedded dried pigment and charcoal")+".mat");
            var path=root+"/Textures/MacabreRevision/"+(clip?"DoorPNGs/"+name+".png":"WallResidueAtlas.png");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture)throw new Exception(path);
            m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",clip?Color.white:new Color(.74f,.65f,.58f,.90f));m.SetFloat("_Smoothness",.06f);
            m.SetFloat("_AlphaClip",clip?1:0);m.SetFloat("_Cutoff",.3f);m.SetFloat("_Surface",clip?0:1);m.SetFloat("_Blend",0);
            m.SetFloat("_BlendModePreserveSpecular",0);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_EnvironmentReflections",0);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetFloat("_SrcBlend",(float)(clip?BlendMode.One:BlendMode.SrcAlpha));m.SetFloat("_DstBlend",(float)(clip?BlendMode.Zero:BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)(clip?BlendMode.Zero:BlendMode.OneMinusSrcAlpha));m.SetFloat("_ZWrite",clip?1:0);
            if(clip){m.EnableKeyword("_ALPHATEST_ON");m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.AlphaTest;m.SetOverrideTag("RenderType","TransparentCutout");}
            else{m.DisableKeyword("_ALPHATEST_ON");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;m.SetOverrideTag("RenderType","Transparent");}
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
        }
    }

    static void BatchSignBackings(Transform atrium)
    {
        var root=atrium.Find("Macabre texture and prop revision/Internet PNG door signage");
        if(root.Find("Static sign sector 0"))return;
        var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.name.StartsWith("Original internet PNG - ")).ToArray();
        for(int sector=0;sector<8;sector++)
        {
            var go=new GameObject("Static sign sector "+sector);go.transform.SetParent(root,false);
            var selected=rs.Where(r=>r).Where(r=>
            {
                var p=atrium.InverseTransformPoint(r.transform.position);float angle=Mathf.Atan2(p.x/27.1f,p.z/19.1f);
                int bay=(Mathf.RoundToInt(angle*24/Mathf.PI)+48)%48;return bay/6==sector;
            }).ToArray();
            foreach(var group in selected.GroupBy(r=>r.sharedMaterial))
            {
                var combine=new List<CombineInstance>();
                foreach(var r in group)
                {
                    var f=r.GetComponent<MeshFilter>();
                    for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)combine.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=sub,transform=go.transform.worldToLocalMatrix*r.transform.localToWorldMatrix});
                }
                var mesh=new Mesh{name="Sign backings sector "+sector,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);
                AssetDatabase.CreateAsset(mesh,"Assets/_Project/Scenes/Cylinder/Meshes/MacabreRevision/StaticSignSector_"+sector+"_"+group.Key.name+".asset");
                var part=new GameObject("Static sign backing - "+group.Key.name);part.transform.SetParent(go.transform,false);
                part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
                foreach(var r in group){UnityEngine.Object.DestroyImmediate(r.GetComponent<MeshFilter>());UnityEngine.Object.DestroyImmediate(r);}
            }
        }
    }
}
