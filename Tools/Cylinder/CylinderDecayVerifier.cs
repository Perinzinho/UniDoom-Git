using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class CylinderDecayVerifier
{
    const string RoomPath = "Assets/prefabs/Scenario/Cylinder.prefab", Art = "Assets/Art/Cylinder";
    static readonly BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    public static void Verify()
    {
        // Exercise the existing actual-player routes, three moving doors, counter support and complete roof.
        CylinderBuilder.Verify();
        var room = Object.FindObjectsByType<Transform>().First(t => t.name == "Cylinder" && t.parent == null).gameObject;
        var atrium = room.transform.Find("Abandoned circular atrium");
        Check(atrium, "PREFAB"); Settings(); RenderStories(atrium, "");
        EditorSceneManager.SaveScene(room.scene, Art + "/CylinderPreview.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var source = Object.FindObjectsByType<Transform>().First(t => t.name == "Cylinder" && t.parent == null).gameObject;
        // Preserve the map transform and neighbouring geometry; animate a disposable copy of its doors.
        var instance = Object.Instantiate(source); instance.name = "Cylinder map verification";
        if(PrefabUtility.IsPartOfPrefabInstance(instance)) PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        source.SetActive(false);
        atrium = instance.transform.Find("Abandoned circular atrium"); Check(atrium, "MAP");
        typeof(CylinderBuilder).GetField("root", Private).SetValue(null, atrium);
        typeof(CylinderBuilder).GetMethod("Validate", Private).Invoke(null, new object[] { instance });
        // Keep neighbouring collision and lighting during verification; hide their renderers for asset previews.
        var hidden = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && !r.transform.IsChildOf(instance.transform)).ToArray();
        foreach(var renderer in hidden) renderer.enabled = false;
        Settings(); RenderStories(atrium, "InMap");
        foreach(var renderer in hidden) renderer.enabled = true;
        source.SetActive(true); Object.DestroyImmediate(instance);
        File.WriteAllText("Preview/Cylinder-decay-validation.txt", "Cylinder decay verified\n43 static doors with one finish, no cross boards or handles.\n3 original proximity doors remain functional; closed blocking and open passage checked.\nActual CharacterController traverses both full circulation loops and south entrance in prefab and map transform.\n80 original kiosk props supported; roof sealed with 384 upward rays.\nRadio, campus map, missing notices, vending machine, benches, abandoned food and luggage.\nClaw tracks, eggs, creature molt, damp, peeling walls and hanging roof damage.\nAll new closed-door materials are non-emissive; the three usable doors keep their red indicators.\nGeometry and textures retain the pixel style; new textures use Point and max 128 px.\nMain scene and other room assets are not saved by this revision.\nEdit-mode validation; combat and discovery pacing still need playtesting.\n");
        Debug.Log("CYLINDER_DECAY_MAP_OK"); Debug.Log("CYLINDER_DECAY_VERIFIED");
    }
    static void Settings()
    {
        ShaderUtil.allowAsyncCompilation = false; RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.13f, .12f, .10f); RenderSettings.fog = false;
        var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = probe;
    }
    static void Check(Transform atrium, string label)
    {
        if(Vector3.Distance(atrium.lossyScale, Vector3.one) > .005f) throw new Exception(label + " atrium scale changed");
        var images = atrium.GetComponentsInChildren<MeshRenderer>().Where(r => r.sharedMaterial && r.sharedMaterial.name.StartsWith("SealedDoor")).ToArray();
        if(images.Length != 43 || images.Select(r => r.sharedMaterial.name).Distinct().Count() != 1) throw new Exception("Expected 43 quiet doors in one standard finish");
        foreach(var image in images)
        {
            if(!image.GetComponent<MeshFilter>().sharedMesh || image.GetComponent<Collider>() || image.GetComponentInParent<OpenDoorWithKey>()) throw new Exception("Sealed door geometry or static behaviour changed");
            if(image.sharedMaterial.IsKeywordEnabled("_EMISSION")) throw new Exception("Closed doors must not show active light cues");
        }
        if(atrium.GetComponentsInChildren<OpenDoorWithKey>().Length != 3) throw new Exception("Interactive door count changed");
        var details = atrium.Find("Timeworn atrium stories");
        if(!details || !details.Find("Creature molt nest hidden behind the upper gallery") || !details.Find("Abandoned emergency radio and campus map")) throw new Exception("Exploration details missing");
        foreach(var renderer in atrium.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.GetComponent<MeshFilter>() && !renderer.GetComponent<MeshFilter>().sharedMesh) throw new Exception("Missing mesh: " + renderer.name);
            foreach(var mat in renderer.sharedMaterials) if(!mat || !mat.shader || mat.shader.name == "Hidden/InternalErrorShader") throw new Exception("Missing material: " + renderer.name);
        }
        foreach(var path in Directory.GetFiles(Art + "/Textures", "*.png").Where(p => Path.GetFileName(p).StartsWith("Decay") || Path.GetFileName(p).StartsWith("SealedDoor")))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            if(importer.filterMode != FilterMode.Point || importer.maxTextureSize != 128) throw new Exception("Texture pixel sampling changed");
        }
        Debug.Log("CYLINDER_DECAY_" + label + "_DETAILS_OK 43 quiet standard doors and 3 usable doors");
    }
    static void RenderStories(Transform atrium, string suffix)
    {
        Render(atrium, V(8, 1.84f, 17.3f), V(0, 2.8f, -1), "Player" + suffix);
        Render(atrium, V(21.5f, 1.84f, 5), V(0, 2, 0), "Walkway" + suffix);
        var bay = atrium.Find("Service bay 7");
        Render(atrium, bay.TransformPoint(V(1.0f, 1.84f, -3.9f)), bay.TransformPoint(V(0, 1.9f, -.25f)), "Radio" + suffix, true);
        var nest = atrium.Find("Service bay 30");
        Render(atrium, nest.TransformPoint(V(.7f, 1.84f, -4.6f)), nest.TransformPoint(V(0, 4.6f, -.3f)), "CreatureTraces" + suffix, true);
        var door = atrium.Find("Service bay 4");
        Render(atrium, door.TransformPoint(V(0, 1.65f, -2.4f)), door.TransformPoint(V(0, 1.52f, -.25f)), "SealedDoor" + suffix, true);
    }
    static void Render(Transform atrium, Vector3 pos, Vector3 look, string name, bool world = false)
    {
        var go = new GameObject("Cylinder decay preview camera"); var c = go.AddComponent<Camera>(); go.AddComponent<UniversalAdditionalCameraData>();
        c.fieldOfView = 72; c.nearClipPlane = .04f; c.farClipPlane = 140; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.025f, .035f, .048f);
        go.transform.position = world ? pos : atrium.TransformPoint(pos); go.transform.LookAt(world ? look : atrium.TransformPoint(look));
        var rt = new RenderTexture(1672, 941, 24); c.targetTexture = rt; for(int i = 0; i < 4; i++) c.Render(); RenderTexture.active = rt;
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
        Directory.CreateDirectory("Preview"); File.WriteAllBytes("Preview/Cylinder-" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = null; c.targetTexture = null; Object.DestroyImmediate(texture); Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
    }
}
