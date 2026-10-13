// Editor-only. Run in the isolated project, then use install_room_style.py.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RoomStyleBuilder
{
    const string Basement="Assets/prefabs/Scenario/Basement.prefab";
    const string Classroom="Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab";
    const string Office="Assets/Art/DirectorOffice/Materials/";
    struct Pose { public Vector3 position,scale; }
    static Dictionary<string,Pose> baseline;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static string PathOf(Transform t,Transform root)=>AnimationUtility.CalculateTransformPath(t,root);

    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/"))throw new Exception("Use an isolated project");
        ApplyMaterials();
        var room=PrefabUtility.LoadPrefabContents(Basement);
        baseline=room.GetComponentsInChildren<Transform>(true).GroupBy(t=>PathOf(t,room.transform)).ToDictionary(g=>g.Key,g=>new Pose{position=g.First().localPosition,scale=g.First().localScale});
        Resize(room);WarmLights(room,false);
        PrefabUtility.SaveAsPrefabAsset(room,Basement);PrefabUtility.UnloadPrefabContents(room);
        var classroom=PrefabUtility.LoadPrefabContents(Classroom);WarmLights(classroom,true);
        PrefabUtility.SaveAsPrefabAsset(classroom,Classroom);PrefabUtility.UnloadPrefabContents(classroom);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&t.parent==null).gameObject;
        Resize(map);WarmLights(map,false);
        foreach(var t in map.GetComponentsInChildren<Transform>(true))
            if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        var encounter=map.GetComponentInChildren<BasementArenaEncounter>();PrefabUtility.RecordPrefabInstancePropertyModifications(encounter);
        foreach(var l in map.GetComponentsInChildren<Light>())PrefabUtility.RecordPrefabInstancePropertyModifications(l);
        EditorSceneManager.SaveScene(map.scene,"Assets/Scenes/SampleScene.unity");
        Verify();Debug.Log("ROOM_STYLE_BUILD_OK");
    }

    public static void ApplyMaterials()
    {
        foreach(var pair in new[]{"Brick:Plaster","Stone:Plaster","Floor:Floor","Ceiling:Ceiling","Iron:Metal","Steel:Metal","Rust:Metal","Bone:Ivory","Blood:BookRed","Amber:LampGlow","Screen:Screen"})
            CopySurface("Basement",pair);
        Tint("Basement","Brick",new Color(.82f,.80f,.75f));Tint("Basement","Stone",new Color(.72f,.73f,.70f));
        Tint("Basement","Iron",new Color(.50f,.53f,.50f));Tint("Basement","Rust",new Color(.69f,.43f,.25f));
        foreach(var pair in new[]{"Plaster:Plaster","Ceiling:Ceiling","Floor:Floor","Wood:Walnut","Door:DarkWood","Steel:Metal","Iron:Metal","Black:Black","Paper:Ivory","Chalk:Ivory","Rubble:Plaster","BookRed:BookRed","BookBlue:BookBlue","Canvas:Ivory","Tube:LampGlow","WarmTube:LampGlow"})
            CopySurface("ClassRoom",pair);
        Tint("ClassRoom","Iron",new Color(.50f,.53f,.50f));Tint("ClassRoom","Rubble",new Color(.72f,.70f,.65f));
        var floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ClassRoom/Materials/Floor.mat");floor.SetTextureScale("_BaseMap",new Vector2(11.25f,10));EditorUtility.SetDirty(floor);
        var plaster=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ClassRoom/Materials/Plaster.mat");plaster.SetTextureScale("_BaseMap",new Vector2(3,1));EditorUtility.SetDirty(plaster);
        // Unique classroom graphics keep their content with the same pixel sampling as the office.
        foreach(string path in Directory.GetFiles("Assets/Art/ClassRoom/Textures","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.filterMode==FilterMode.Point&&importer.anisoLevel==1&&importer.maxTextureSize==128&&importer.textureCompression==TextureImporterCompression.Uncompressed)continue;
            importer.filterMode=FilterMode.Point;importer.anisoLevel=1;importer.maxTextureSize=128;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
    public static void RefreshStyleAndVerify(){ApplyMaterials();Verify();Debug.Log("ROOM_STYLE_BUILD_OK");}
    static void CopySurface(string area,string pair)
    {
        string[] names=pair.Split(':');var target=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/"+area+"/Materials/"+names[0]+".mat");
        var source=AssetDatabase.LoadAssetAtPath<Material>(Office+names[1]+".mat");
        if(!target||!source)throw new Exception("Missing style material: "+area+"/"+pair);
        var tiling=target.GetTextureScale("_BaseMap");
        EditorUtility.CopySerialized(source,target);target.name=names[0];target.SetTextureScale("_BaseMap",tiling);EditorUtility.SetDirty(target);
    }
    static void Tint(string area,string name,Color tint)
    {var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/"+area+"/Materials/"+name+".mat");m.SetColor("_BaseColor",tint);EditorUtility.SetDirty(m);}
    static void WarmLights(GameObject room,bool classroom)
    {
        foreach(var light in room.GetComponentsInChildren<Light>())
        {
            if(light.color.r>light.color.g*2)continue; // Red combat/exit cues remain legible.
            light.color=classroom&&light.name.StartsWith("Cold window")?new Color(.60f,.64f,.67f):new Color(1,.78f,.49f);
            if(classroom)light.intensity=light.name.StartsWith("Cold window")?1f:2.6f;
        }
    }
    static void Resize(GameObject room)
    {
        var arena=room.transform.Find("PreBossArea");var boss=room.transform.Find("BossRoom");
        ResizeShell(arena,"Hall_Floor","Hall_Ceiling",34,6);
        ResizeShell(boss,"Main_Floor","Main_Ceiling",22,6.2f);
        foreach(var area in new[]{arena,boss})
        {
            bool first=area==arena;float height=first?6:6.2f,spread=first?34f/26:22f/18;
            foreach(Transform pillar in area.Find("Pillar"))
            {var p=baseline[PathOf(pillar,room.transform)];pillar.localPosition=V(p.position.x*spread,height/2,p.position.z);pillar.localScale=V(p.scale.x,height,p.scale.z);}
            var details=area.Find(first?"BasementArenaDetails":"BasementBossDetails");
            foreach(Transform t in details)
            {
                var p=baseline[PathOf(t,room.transform)];t.localPosition=p.position;t.localScale=p.scale;
                if(first)
                {
                    if(t.name.StartsWith("Pillar iron collar")){t.localPosition=V(p.position.x*spread,0,p.position.z);t.localScale=V(1,1.5f,1);}
                    else if(t.name.StartsWith("Ceiling joist")||t.name=="Overhead cable trays"){t.localPosition+=Vector3.up*2;t.localScale=V(spread,1,1);}
                    else if(t.name.StartsWith("Hanging industrial fixture"))t.localPosition+=Vector3.up*2;
                    else if(t.name.StartsWith("Arena wall trim")){t.localPosition+=Vector3.right*Mathf.Sign(p.position.x)*4;t.localScale=V(1,1.5f,1);}
                    else if(Mathf.Abs(p.position.x)>10)t.localPosition+=Vector3.right*Mathf.Sign(p.position.x)*4;
                }
                else
                {
                    if(t.name=="Boss sanctum masonry")t.localScale=V(spread,6.2f/5,1);
                    else if(t.name.StartsWith("Boss column reinforcement")){t.localPosition=V(p.position.x*spread,0,p.position.z);t.localScale=V(1,6.2f/5,1);}
                    else if(Mathf.Abs(p.position.x)>8)t.localPosition+=Vector3.right*Mathf.Sign(p.position.x)*2;
                }
            }
            if(first)foreach(Transform t in area.Find("Floor"))
                if(t.name.StartsWith("Plataform")){var p=baseline[PathOf(t,room.transform)];t.localPosition=p.position+Vector3.right*Mathf.Sign(p.position.x)*4;}
        }
        var encounter=room.GetComponentInChildren<BasementArenaEncounter>();encounter.arenaCenter=V(0,3,0);encounter.arenaSize=V(33.4f,6,29.4f);
        foreach(var point in encounter.spawnPoints){var p=baseline[PathOf(point,room.transform)];point.localPosition=p.position;if(Mathf.Abs(p.position.x)>8)point.localPosition+=Vector3.right*Mathf.Sign(p.position.x)*2;}
        room.GetComponentInChildren<BasementGateLayout>().Fit();Physics.SyncTransforms();
    }
    static void ResizeShell(Transform area,string floorName,string ceilingName,float width,float height)
    {
        var floor=area.Find("Floor/"+floorName);var ceiling=area.Find("Walls/"+ceilingName);
        floor.localScale=V(width,floor.localScale.y,floor.localScale.z);
        ceiling.localScale=V(width,ceiling.localScale.y,ceiling.localScale.z);ceiling.localPosition=V(ceiling.localPosition.x,height,ceiling.localPosition.z);
        foreach(Transform wall in area.Find("Walls"))
        {
            if(wall.name==ceilingName||wall.name.StartsWith("Hallway",StringComparison.OrdinalIgnoreCase))continue;
            var p=wall.localPosition;var s=wall.localScale;
            if(wall.name.EndsWith("Header")){float bottom=p.y-s.y/2;wall.localPosition=V(p.x,(height+bottom)/2,p.z);wall.localScale=V(s.x,height-bottom,s.z);}
            else if(wall.name=="West_Wall"||wall.name=="East_Wall"){wall.localPosition=V(Mathf.Sign(p.x)*width/2,height/2,p.z);wall.localScale=V(s.x,height,s.z);}
            else if(wall.name.EndsWith("Left")||wall.name.EndsWith("Right"))
            {float inner=p.x-Mathf.Sign(p.x)*s.x/2;float outer=Mathf.Sign(p.x)*width/2;wall.localPosition=V((inner+outer)/2,height/2,p.z);wall.localScale=V(Mathf.Abs(outer-inner),height,s.z);}
            else if(wall.name=="Back_Wall"){wall.localPosition=V(p.x,height/2,p.z);wall.localScale=V(width,height,s.z);}
        }
    }

    public static void Verify()
    {
        // Existing meaningful checks: actual player movement, gate lock, encounter and classroom door.
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&t.parent==null).gameObject;
        CheckSize(map,"MAP");VerifyTraversal(map,"MAP");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Basement));room.transform.position=Vector3.zero;
        CheckSize(room,"PREFAB");VerifyTraversal(room,"PREFAB");
        BasementBuilder.VerifyAndRender();ClassRoomBuilder.VerifyAndRender();
        Debug.Log("ROOM_STYLE_VERIFIED");
    }
    static void VerifyTraversal(GameObject room,string label)=>typeof(BasementPassageBuilder).GetMethod("VerifyRoom",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{room,label});
    static void CheckSize(GameObject room,string label)
    {
        Physics.SyncTransforms();var area=room.transform.Find("PreBossArea");var floor=area.Find("Floor/Hall_Floor").GetComponent<BoxCollider>();var ceiling=area.Find("Walls/Hall_Ceiling").GetComponent<BoxCollider>();
        float floorTop=area.InverseTransformPoint(floor.transform.TransformPoint(floor.center+Vector3.up*floor.size.y/2)).y;
        float ceilingBottom=area.InverseTransformPoint(ceiling.transform.TransformPoint(ceiling.center-Vector3.up*ceiling.size.y/2)).y;
        float clear=(ceilingBottom-floorTop)*area.TransformVector(Vector3.up).magnitude;
        float width=floor.transform.TransformVector(Vector3.right*floor.size.x).magnitude;
        if(clear<5||width<19)throw new Exception(label+": enlarged arena dimensions incorrect, width="+width+" height="+clear);
        Debug.Log("ROOM_STYLE_"+label+"_SIZE width="+width+" clearHeight="+clear);
    }
}
