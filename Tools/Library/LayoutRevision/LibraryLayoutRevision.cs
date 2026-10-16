using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class LibraryLayoutRevision
{
    const string Art="Assets/_Project/Scenes/Library";
    const string Materials=Art+"/Materials/LayoutRevision";
    const string Profiles=Art+"/Profiles/LayoutRevision";
    const string Reflections=Art+"/Reflections/LayoutRevision";
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Material doorPaint,metal,wall;
    static Transform Group(Transform p,string name,Vector3 pos){var g=new GameObject(name).transform;g.SetParent(p,false);g.localPosition=pos;return g;}
    static Transform Box(Transform p,string name,Vector3 pos,Vector3 size,Material material,bool solid=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;var t=go.transform;t.SetParent(p,false);t.localPosition=pos;t.localScale=size;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(go.GetComponent<BoxCollider>());return t;
    }
    static Material Clone(string path,string name)
    {
        var m=new Material(AssetDatabase.LoadAssetAtPath<Material>(path)){name=name};string dest=Materials+"/"+name+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(dest);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,dest);return m;
    }
    public static void BuildAndVerify()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use the isolated project");
        foreach(var path in new[]{Materials,Profiles,Reflections,"Evidence/Layout"})Directory.CreateDirectory(path);AssetDatabase.Refresh();
        wall=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/CampusFinish/UniversityPaintedPlaster.mat");
        metal=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/DecayRevision/Iron.mat");
        doorPaint=Clone(AssetDatabase.GetAssetPath(wall),"SimpleUniversityDoorPaint");doorPaint.SetColor("_BaseColor",new Color(.23f,.27f,.25f));doorPaint.SetFloat("_BumpScale",.12f);
        var sky=Clone(Art+"/Materials/DecayRevision/Sky.mat","DuskCourtyard");sky.SetColor("_EmissionColor",new Color(.10f,.12f,.14f));
        string profilePath=Profiles+"/ClassroomStyleLibrary.asset";
        if(!AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath))AssetDatabase.CopyAsset("Assets/_Project/Scenes/ClassRoom/Lighting/ClassRoomVisibility.asset",profilePath);
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        var classroom=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Scenes/ClassRoom/Prefabs/AbandonedClassRoomInterior.prefab").GetComponentInChildren<TCRegionAtmosphere>(true);
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            var art=root.transform.Find("Abandoned Library Interior");
            var desks=art.Cast<Transform>().Where(t=>t.name.StartsWith("Abandoned reading desk ",StringComparison.Ordinal)).ToArray();
            if(desks.Length!=11)throw new Exception("Expected eleven reading desks");
            foreach(var desk in desks)
            {
                var table=desk.Find("Worn reading table");table.localScale=Vector3.Scale(table.localScale,V(1.30f,1.04f,1.30f));
                foreach(Transform child in desk)
                {
                    if(child==table)continue;
                    if(child.name=="Pulled out chair"||child.name=="Discarded reader chair")child.localPosition=Vector3.Scale(child.localPosition,V(1.23f,1,1.23f));
                    else child.localPosition+=Vector3.up*.0316f;
                }
            }
            var region=root.GetComponentInChildren<TCRegionAtmosphere>();region.ambient=classroom.ambient;region.fogColor=classroom.fogColor;region.fogDensity=classroom.fogDensity;region.theme="abandoned university library";
            root.GetComponentInChildren<Volume>().sharedProfile=profile;
            foreach(var light in root.GetComponentsInChildren<Light>())
            {
                if(light.name=="Flicker scarred cold light"){light.intensity=2;light.range=9;light.color=new Color(.71f,.84f,.81f);}
                else if(light.name=="Diffuse overcast window light"){light.intensity=.75f;light.range=7;light.color=new Color(.57f,.68f,.78f);}
                else if(light.name=="Weak amber desk light"){light.intensity=1.35f;light.range=4.2f;light.color=new Color(1,.73f,.45f);}
            }
            foreach(var r in art.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name=="Sky"?sky:m).ToArray();
            var barricade=art.Find("Barricaded archive door");if(!barricade)throw new Exception("Archive door missing");barricade.gameObject.SetActive(false);
            SimpleDoor(art);FinishEntrance(art);PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();BakeReflections();AssetDatabase.SaveAssets();Debug.Log("LIBRARY_LAYOUT_BUILD_PASS");
        LayoutVerifier.VerifyAndRender();LayoutPlayCheck.Run();
    }
    static void SimpleDoor(Transform art)
    {
        var old=art.Find("Simple library entrance");if(old)throw new Exception("Door revision already applied");
        var entry=Group(art,"Simple library entrance",V(10.1f,0,19.67f));
        Box(entry,"Left plaster reveal",V(-1.275f,1.475f,0),V(.65f,2.95f,.20f),wall,true);
        Box(entry,"Right plaster reveal",V(1.425f,1.475f,0),V(.95f,2.95f,.20f),wall,true);
        Box(entry,"Plaster above plain door",V(0,2.83f,0),V(1.90f,.26f,.20f),wall,true);
        foreach(float x in new[]{-.875f,.875f})Box(entry,"Painted door jamb",V(x,1.34f,0),V(.15f,2.68f,.24f),doorPaint,true);
        Box(entry,"Plain door header",V(0,2.67f,0),V(1.90f,.12f,.24f),doorPaint,true);
        var mechanism=Group(entry,"Unlatched swing door",V(0,1.30f,0));mechanism.localRotation=Quaternion.Euler(0,90,0);
        var trigger=mechanism.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=V(4.6f,3.4f,2.5f);
        var leaf=Box(mechanism,"Door",Vector3.zero,V(.12f,2.60f,1.55f),doorPaint,true);
        // Handles are expressed in the leaf's unscaled local coordinates and follow its hinge.
        foreach(float face in new[]{-.5f,.5f})
        {
            Box(leaf,"Simple handle plate",V(face,.0f,.32f),V(.09f,.08f,.055f),metal);
            Box(leaf,"Plain lever handle",V(face*1.22f,-.003f,.28f),V(.30f,.018f,.12f),metal);
        }
        var door=mechanism.gameObject.AddComponent<OpenDoorWithKey>();var so=new SerializedObject(door);so.FindProperty("door").objectReferenceValue=leaf;
        foreach(string key in new[]{"requiresKey01","requiresKey02","requiresKey03"})so.FindProperty(key).boolValue=false;
        so.FindProperty("hingePoint").vector3Value=V(0,0,-.5f);so.FindProperty("openAngle").floatValue=95;so.FindProperty("animationDuration").floatValue=.6f;so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void PolishAndVerify()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        wall=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/CampusFinish/UniversityPaintedPlaster.mat");doorPaint=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/SimpleUniversityDoorPaint.mat");
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try{FinishEntrance(root.transform.Find("Abandoned Library Interior"));PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();BakeReflections();AssetDatabase.SaveAssets();Debug.Log("LIBRARY_LAYOUT_BUILD_PASS");LayoutVerifier.VerifyAndRender();LayoutPlayCheck.Run();
    }
    static void FinishEntrance(Transform art)
    {
        var entry=art.Find("Simple library entrance");if(entry.Find("Finished exterior"))throw new Exception("Entrance finish already applied");
        var face=Group(entry,"Finished exterior",Vector3.zero);
        var left=Clone(AssetDatabase.GetAssetPath(wall),"EntranceWidePlaster");left.SetTextureScale("_BaseMap",new Vector2(11.56f,2.2f));
        var right=Clone(AssetDatabase.GetAssetPath(wall),"EntranceNarrowPlaster");right.SetTextureScale("_BaseMap",new Vector2(1.47f,2.2f));
        Box(face,"Exterior left wall finish",V(-12.44f,2.2f,.89f),V(23.12f,4.4f,.12f),left);
        Box(face,"Exterior right wall finish",V(2.335f,2.2f,.89f),V(2.93f,4.4f,.12f),right);
        Box(face,"Exterior door lintel finish",V(0,3.565f,.89f),V(1.90f,1.67f,.12f),wall);
        foreach(float x in new[]{-.875f,.875f})Box(face,"Plain frame exterior return",V(x,1.34f,.48f),V(.15f,2.68f,1.20f),doorPaint);
        var original=art.Find("Damaged library entrance sign");var outside=Object.Instantiate(original.gameObject,art).transform;outside.name="Exterior library entrance sign";outside.localPosition=V(10.1f,3.05f,20.66f);outside.localRotation=Quaternion.identity;
        LibraryDepthSigns.Bake(art);
    }
    public static void FixSceneAndVerify()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");var report=new System.Text.StringBuilder();
        foreach(var root in scene.GetRootGameObjects().Where(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)==LibraryBuilder.Prefab))
        foreach(Transform child in root.transform)
        {
            var r=child.GetComponent<MeshRenderer>();if(!r)continue;
            report.AppendLine(root.name+"/"+child.name+": "+string.Join(", ",r.sharedMaterials.Select(m=>m?m.name+" / "+m.shader.name:"NULL")));
            string path=child.name=="Floor"?Art+"/Materials/DecayRevision/Floor.mat":child.name=="Roof"?Art+"/Materials/CampusFinish/MineralCeilingPanels.mat":Art+"/Materials/CampusFinish/UniversityPaintedPlaster.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);r.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        var library=GameObject.Find("Library").transform.Find("Abandoned Library Interior");var entrance=library.TransformPoint(V(10.1f,1.5f,20.7f));
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.bounds.SqrDistance(entrance)<16))
        {
            var materials=r.sharedMaterials;bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                var m=materials[i];if(!m)continue;
                var color=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):m.HasProperty("_Color")?m.GetColor("_Color"):Color.white;
                report.AppendLine("NEAR "+r.name+" material="+m.name+" color="+color+" bounds="+r.bounds);
                if(color.g>.65f&&color.g>color.r*2&&color.b<color.g*.65f){materials[i]=wall?wall:AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/CampusFinish/UniversityPaintedPlaster.mat");changed=true;}
            }
            if(changed){r.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(r);report.AppendLine("CORRECTED GREEN "+r.name);}
        }
        File.WriteAllText("Evidence/Layout/main-surface-correction.txt",report.ToString());EditorSceneManager.SaveScene(scene);Debug.Log("LIBRARY_LAYOUT_BUILD_PASS");LayoutVerifier.VerifyAndRender();LayoutPlayCheck.Run();
    }
    static void BakeReflections()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));
        room.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);var region=room.GetComponentInChildren<TCRegionAtmosphere>();
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;RenderSettings.fog=true;RenderSettings.fogColor=region.fogColor;RenderSettings.fogDensity=region.fogDensity;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/LayoutReflections.unity");ShaderUtil.allowAsyncCompilation=false;
        int i=0;foreach(var probe in room.GetComponentsInChildren<ReflectionProbe>()){probe.mode=ReflectionProbeMode.Baked;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=region.fogColor;if(!Lightmapping.BakeReflectionProbe(probe,Reflections+"/Library_"+(i++)+".exr"))throw new Exception("Reflection bake failed");}
        AssetDatabase.Refresh();var prefab=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try{i=0;foreach(var probe in prefab.GetComponentsInChildren<ReflectionProbe>())probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(Reflections+"/Library_"+(i++)+".exr");PrefabUtility.SaveAsPrefabAsset(prefab,LibraryBuilder.Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
    }
}
