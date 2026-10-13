// Read the current rooms; update only Cylinder's stale material copies and light colors.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ArtStyleAudit
{
    const string Cylinder="Assets/prefabs/Scenario/Cylinder.prefab";
    const string Output="Preview/ArtStyle";
    static readonly string[] Copies={"Concrete:Basement/Stone","Plaster:ClassRoom/Plaster","Iron:Basement/Iron","Rust:Basement/Rust","Roof:Basement/Steel","Amber:Basement/Amber","DoorActive:ClassRoom/Door","Rubble:ClassRoom/Rubble","BandDark:Basement/Iron"};
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Material Mat(string area,string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/"+area+"/Materials/"+name+".mat");

    public static void AlignAndCompare()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/"))throw new Exception("Use the isolated project");
        foreach(string pair in Copies)
        {
            string[] names=pair.Split(':');string[] source=names[1].Split('/');var target=Mat("Cylinder",names[0]);var reference=Mat(source[0],source[1]);
            var tiling=target.GetTextureScale("_BaseMap");EditorUtility.CopySerialized(reference,target);target.name=names[0];target.SetTextureScale("_BaseMap",tiling);EditorUtility.SetDirty(target);
        }
        foreach(string path in Directory.GetFiles("Assets/Art/Cylinder/Textures","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.anisoLevel=1;importer.maxTextureSize=128;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        var room=PrefabUtility.LoadPrefabContents(Cylinder);
        foreach(var light in room.GetComponentsInChildren<Light>())
            if(light.enabled&&light.color.r<=light.color.g*2)light.color=new Color(1,.78f,.49f);
        PrefabUtility.SaveAsPrefabAsset(room,Cylinder);PrefabUtility.UnloadPrefabContents(room);AssetDatabase.SaveAssets();
        CylinderBuilder.Verify();Compare();Debug.Log("ART_STYLE_ALIGNMENT_OK");
    }

    public static void Compare()
    {
        Directory.CreateDirectory(Output);var report=new StringBuilder("# Comparação de estilo artístico\n\nReferência: sala do diretor.\n\n");
        foreach(string pair in Copies)
        {
            var n=pair.Split(':');var s=n[1].Split('/');var a=Mat("Cylinder",n[0]);var b=Mat(s[0],s[1]);
            if(a.mainTexture!=b.mainTexture||a.shader!=b.shader||a.GetColor("_BaseColor")!=b.GetColor("_BaseColor")||a.GetFloat("_Smoothness")!=b.GetFloat("_Smoothness")||a.GetFloat("_Metallic")!=b.GetFloat("_Metallic"))throw new Exception("Stale cylinder material: "+pair);
        }
        foreach(var pair in new[]{"Basement/Brick:Plaster","Basement/Stone:Plaster","Basement/Floor:Floor","Basement/Ceiling:Ceiling","Basement/Steel:Metal","Basement/Amber:LampGlow","ClassRoom/Plaster:Plaster","ClassRoom/Floor:Floor","ClassRoom/Ceiling:Ceiling","ClassRoom/Wood:Walnut","ClassRoom/Door:DarkWood","ClassRoom/Steel:Metal","ClassRoom/Tube:LampGlow"})
        {
            var n=pair.Split(':');var p=n[0].Split('/');var a=Mat(p[0],p[1]);var b=Mat("DirectorOffice",n[1]);
            if(a.mainTexture!=b.mainTexture||a.shader!=b.shader)throw new Exception("Room does not use office finish: "+pair);
        }
        Capture("Director","DirectorRoom","HauntedOfficeInterior",V(2.45f,1.85f,-1.95f),V(-.25f,1.35f,.85f),report);
        Capture("Basement","Basement","PreBossArea",V(2,1.85f,-12.8f),V(-2,1.5f,5),report);
        Capture("ClassRoom","ClassRoom","AbandonedClassRoomInterior",V(10.8f,1.85f,-9.9f),V(.3f,1.65f,3.5f),report);
        Capture("Cylinder","Cylinder","Abandoned circular atrium",V(8,1.8f,17.3f),V(0,2.8f,-1),report);
        report.Append("\nAcabamentos principais compartilham as texturas da sala do diretor. As cópias do cilindro também coincidem em shader, cor, metal e brilho com os materiais atuais do porão e da sala de aula.\n\n");
        report.Append("Variações intencionais: mosaico e concreto rachado do cilindro; manchas e poças transparentes; quadro, mapa e placas da sala de aula; ferrugem e selo ritual do porão. Todos usam amostragem Point. Luz vermelha do cilindro somente nas três portas funcionais.\n\n");
        report.Append("As quatro imagens usam ambiente (0.17, 0.15, 0.11), preenchimento direcional neutro de intensidade 0.3, FOV 72° e a mesma configuração de câmera, sem pós-processamento. As instâncias do mapa incluem seus overrides e escalas; na ausência de uma instância, usa-se o prefab. As luzes locais próprias de cada ambiente permanecem. A comparação não salva a cena de jogo.\n");
        File.WriteAllText(Output+"/Comparison.md",report.ToString());Debug.Log("ART_STYLE_COMPARISON_OK");
    }
    static void Capture(string label,string prefab,string anchorName,Vector3 position,Vector3 target,StringBuilder report)
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==prefab||g.name.StartsWith(prefab+" ("));
        bool inMap=room;
        if(inMap){foreach(var g in scene.GetRootGameObjects())if(g!=room)g.SetActive(false);}
        else {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Scenario/"+prefab+".prefab"));room.transform.position=Vector3.zero;}
        var anchor=room.transform.Find(anchorName);if(!anchor)throw new Exception("Missing anchor: "+label);
        report.Append("## "+label+"\n\nOrigem: "+(inMap?"instância na SampleScene":"prefab")+".\n\n| Material | Textura | Resolução | Filtro | Brilho |\n|---|---|---|---|---|\n");
        var materials=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.GetComponent<TextMesh>()).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().OrderBy(m=>m.name);
        foreach(var material in materials)
        {
            var texture=material.mainTexture;
            if(material.shader.name!="Universal Render Pipeline/Lit")throw new Exception(label+": unexpected surface shader "+material.name);
            if(texture&&(texture.filterMode!=FilterMode.Point||Mathf.Max(texture.width,texture.height)>128))throw new Exception(label+": inconsistent sampling "+material.name+" "+texture.width+"x"+texture.height+" "+texture.filterMode);
            report.Append("| "+material.name+" | "+(texture?texture.name:"Cor sólida")+" | "+(texture?texture.width+" × "+texture.height:"—")+" | "+(texture?texture.filterMode.ToString():"—")+" | "+material.GetFloat("_Smoothness").ToString("F2")+" |\n");
        }
        if(label=="Cylinder")
        {
            int red=room.GetComponentsInChildren<Light>().Count(l=>l.enabled&&l.color.r>l.color.g*2);
            if(red!=3||room.GetComponentsInChildren<OpenDoorWithKey>().Length!=3)throw new Exception("Cylinder must retain three red interactive doors");
        }
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.17f,.15f,.11f);RenderSettings.reflectionIntensity=.12f;RenderSettings.fog=false;
        var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(RenderSettings.ambientLight);RenderSettings.ambientProbe=ambient;
        var fill=new GameObject("Inspection fill").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.78f,.76f,.69f);fill.intensity=.3f;fill.transform.rotation=Quaternion.Euler(48,-35,0);fill.shadows=LightShadows.Soft;
        var camera=new GameObject("Style camera").AddComponent<Camera>();camera.gameObject.AddComponent<UniversalAdditionalCameraData>();camera.fieldOfView=72;camera.nearClipPlane=.05f;camera.farClipPlane=160;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.045f,.078f);
        camera.transform.position=anchor.TransformPoint(position);camera.transform.LookAt(anchor.TransformPoint(target));
        var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;for(int i=0;i<4;i++)camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Output+"/"+label+".png",image.EncodeToPNG());
        report.Append("\n!["+label+"]("+label+".png)\n\n");RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }
}
