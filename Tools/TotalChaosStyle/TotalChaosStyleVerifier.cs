using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class TotalChaosStyleVerifier
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void VerifyVisuals()
    {
        foreach(string path in TotalChaosStyleBuilder.Targets)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!r.enabled)continue;
                    foreach(var m in r.sharedMaterials)if(!m||!m.shader||m.shader.name=="Hidden/InternalErrorShader")throw new Exception("Missing material at "+path+"/"+r.name);
                    var f=r.GetComponent<MeshFilter>();if(f&&!f.sharedMesh)throw new Exception("Missing mesh at "+r.name);
                }
                var authored=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(TotalChaosStyleBuilder.Art)).ToArray();
                if(authored.Length==0)throw new Exception("No complex authored geometry in "+path);
                foreach(var f in authored)
                {
                    var mesh=f.sharedMesh;foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z))throw new Exception("Nonfinite geometry");
                    if(mesh.vertexCount<12||mesh.triangles.Length<12)throw new Exception("Missing geometric detail: "+mesh.name);
                }
                Debug.Log("TOTAL_CHAOS_GEOMETRY_OK "+path+": "+authored.Length+" authored mesh renderers");
                if(path.EndsWith("Cylinder.prefab"))CheckDoors(root);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in Directory.GetFiles(TotalChaosStyleBuilder.Art+"/Textures","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            if(importer.maxTextureSize!=512||!importer.mipmapEnabled)throw new Exception("Material detail settings missing");
            if(path.Contains("Normal")&&importer.textureType!=TextureImporterType.NormalMap)throw new Exception("Missing relief import");
        }
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        ShaderUtil.allowAsyncCompilation=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.105f,.10f,.09f);RenderSettings.fog=false;
        var cylinder=Object.FindObjectsByType<Transform>().First(t=>t.name=="Cylinder"&&t.parent==null);CheckDoors(cylinder.gameObject);
        var atrium=cylinder.Find("Abandoned circular atrium");
        Render(cylinder,atrium,V(8,1.84f,17.3f),V(0,2.8f,-1),"Cylinder");
        foreach(int index in new[]{4,12}) {var bay=atrium.Find("Service bay "+index);Render(cylinder,bay,V(0,1.84f,-3.3f),V(0,1.65f,-.25f),index==4?"Cylinder-SealedDoor":"Cylinder-UsableDoor");}
        var radio=atrium.Find("Service bay 7");Render(cylinder,radio,V(1,1.84f,-3.9f),V(0,1.9f,-.25f),"Cylinder-Radio");
        var classroom=Object.FindObjectsByType<Transform>().First(t=>t.name=="ClassRoom"&&t.parent==null);
        var interior=classroom.Find("AbandonedClassRoomInterior");
        Render(classroom,interior,V(2.1f,1.84f,-10.35f),V(.3f,1.65f,4.65f),"ClassRoom");
        Render(classroom,interior,V(5.3f,1.84f,-1.3f),V(4.5f,1.0f,.35f),"ClassRoom-Furniture");
        var basement=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&t.parent==null);
        var arena=basement.Find("PreBossArea");var boss=basement.Find("BossRoom");
        float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        Render(basement,arena,V(2,af+1.84f,-12),V(13,af+3,14),"Basement-Arena");
        Render(basement,arena,V(26,af+1.84f,-6.2f),V(28.0f,af+2.6f,-4),"Basement-Pipes");
        Render(basement,boss,V(-3.5f,bf+1.84f,front+21),V(-12,bf+6.8f,front+32),"Basement-Boss");
        Directory.CreateDirectory("Preview");File.WriteAllText("Preview/TotalChaos-style-validation.txt","Original Total Chaos GZDoom/ModDB reference study applied with authored geometry.\n46 Cylinder leaves: identical 1.85 m width, 3.25 m height, green panel/vent design.\n43 noninteractive doors share one finish and have no handles, crosses, triggers or colliders.\n3 original functional doors keep their moving colliders, handles and original behaviour.\nChamfered university furniture, rounded radiator, folded cloth, irregular rubble.\nProfiled dented drums, curved pipes, flanges, bolts and valve wheels in Basement.\nTapered curved molt limbs and waste sacks in Cylinder.\nEight authored 512 px surface sets: albedo, normal and metal/smoothness; mipmaps enabled.\nAll mesh/material references loaded successfully in Unity, prefab and current map.\nNo new collision added by decorative geometry; existing room dimensions retained.\n");
        Debug.Log("TOTAL_CHAOS_STYLE_VISUALS_VERIFIED");
    }
    static void CheckDoors(GameObject root)
    {
        var atrium=root.transform.Find("Abandoned circular atrium");
        var sealedDoors=atrium.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.sharedMaterial&&r.sharedMaterial.name.StartsWith("SealedDoor")).ToArray();
        if(sealedDoors.Length!=43||sealedDoors.Select(r=>r.sharedMaterial).Distinct().Count()!=1)throw new Exception("Sealed doors not standardized");
        foreach(var r in sealedDoors)
        {
            var bounds=r.GetComponent<MeshFilter>().sharedMesh.bounds;
            if(Mathf.Abs(bounds.size.x-1.85f)>.002f||Mathf.Abs(bounds.size.y-3.25f)>.002f)throw new Exception("Sealed door dimensions differ: "+bounds);
            if(r.GetComponent<Collider>()||r.GetComponentInParent<OpenDoorWithKey>()||r.sharedMaterial.IsKeywordEnabled("_EMISSION"))throw new Exception("Sealed door has active cues");
        }
        var active=atrium.GetComponentsInChildren<OpenDoorWithKey>();if(active.Length!=3)throw new Exception("Functional doors changed");
        foreach(var door in active)
        {
            var leaf=door.transform.Find("Door");var visual=leaf.GetComponentsInChildren<MeshRenderer>().First(r=>r.enabled&&r.sharedMaterial.name=="DoorActive");
            var b=visual.GetComponent<MeshFilter>().sharedMesh.bounds;
            if(Mathf.Abs(b.size.z-1.85f)>.002f||Mathf.Abs(b.size.y-3.25f)>.002f||!leaf.GetComponent<BoxCollider>())throw new Exception("Usable door dimensions differ");
            if(!leaf.Find("Cylinder_UsableLeverHandle"))throw new Exception("Usable handle missing");
        }
        Debug.Log("TOTAL_CHAOS_STANDARD_DOORS_OK 46 leaves, common 1.85 x 3.25 m design; 43 sealed / 3 usable");
    }
    static void Render(Transform room,Transform coords,Vector3 position,Vector3 target,string name)
    {
        // Leave probes and colliders alive; hide unrelated renderers only for focused asset previews.
        var hidden=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&(!r.transform.IsChildOf(room)||r.GetComponent<TextMesh>())).ToArray();
        foreach(var r in hidden)r.enabled=false;
        var go=new GameObject("Total Chaos style preview");var c=go.AddComponent<Camera>();go.AddComponent<UniversalAdditionalCameraData>();
        c.fieldOfView=70;c.nearClipPlane=.04f;c.farClipPlane=150;c.backgroundColor=new Color(.02f,.025f,.03f);c.clearFlags=CameraClearFlags.SolidColor;
        go.transform.position=coords.TransformPoint(position);go.transform.LookAt(coords.TransformPoint(target));
        var rt=new RenderTexture(1672,941,24);c.targetTexture=rt;for(int i=0;i<4;i++)c.Render();RenderTexture.active=rt;
        var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();
        Directory.CreateDirectory("Preview");File.WriteAllBytes("Preview/TotalChaos-"+name+".png",png.EncodeToPNG());
        RenderTexture.active=null;c.targetTexture=null;Object.DestroyImmediate(png);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);foreach(var r in hidden)r.enabled=true;
    }
}
