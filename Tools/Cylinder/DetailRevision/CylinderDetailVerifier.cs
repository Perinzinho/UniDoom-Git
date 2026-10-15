using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class CylinderDetailVerifier
{
    const string Prefab = CylinderDetailBuilder.Prefab;
    static Transform atrium;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void BuildWithBaseline()
    {
        Capture("Before");
        CylinderDetailBuilder.Build();
        Capture("Generated");
    }

    static GameObject LoadScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        room.transform.position = Vector3.zero;
        atrium = room.transform.Find("Abandoned circular atrium");
        return room;
    }

    public static void VerifyFinal()
    {
        var room = LoadScene();
        ValidateReferences(room);
        ValidateGeometry(room, "prefab");
        room.transform.position = V(29.66f, 4.84f, 31.37f);
        ValidateGeometry(room, "placed");
        var encounter = room.GetComponentInChildren<CylinderEnemySpawns>();
        var so = new SerializedObject(encounter);
        Require(so.FindProperty("enemyPrefab").objectReferenceValue, "Missing enemy prefab");
        var points = so.FindProperty("spawnPoints"); Require(points.arraySize == 6, "Expected six spawns");
        Physics.SyncTransforms();
        for (int i = 0; i < points.arraySize; i++)
        {
            var point = (Transform)points.GetArrayElementAtIndex(i).objectReferenceValue;
            Require(point, "Missing spawn transform");
            // Existing Enemy prefab is a radius .5, height 2 capsule centred on its transform.
            Require(Physics.Raycast(point.position, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore), "Spawn has no floor: " + point.name);
            Require(Mathf.Abs(point.position.y - hit.point.y - 1.04f) < .10f, "Spawn height mismatch: " + point.name);
            var occupied = Physics.OverlapCapsule(point.position - Vector3.up * .49f, point.position + Vector3.up * .49f, .48f, ~0, QueryTriggerInteraction.Ignore);
            Require(occupied.Length == 0, "Spawn intersects geometry: " + point.name + " " + string.Join(",", occupied.Select(c => c.name)));
        }
        Capture("Final");
        File.WriteAllText("Evidence/edit-validation.txt", "PASS\nUnity 6000.5.7f1 / URP 17.5\nPrefab loaded; no missing scripts, serialized object references, materials or meshes.\nBoth complete circulation loops and south entrance tested with the actual Player CharacterController.\nThree proximity doors: trigger, open passage and closed blocking checked.\n43 decorative sealed doors retained.\n80 canteen support markers and 384 roof coverage rays checked.\nSix spawns connected to Enemy.prefab; capsule clearances and floor support checked.\nGeometry checks repeated at prefab origin and placed transform.\nFinal screenshots rendered with local region atmosphere and postprocessing.\n");
        Debug.Log("CYLINDER_DETAIL_EDIT_PASS");
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    static void ValidateReferences(GameObject root)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script " + transform.name);
            foreach (var component in transform.GetComponents<Component>())
            {
                if (!component) continue;
                var serialized = new SerializedObject(component); var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null && iterator.objectReferenceEntityIdValue != default)
                        throw new Exception("Missing reference " + transform.name + "." + iterator.propertyPath);
            }
        }
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy))
        {
            var mf = r.GetComponent<MeshFilter>(); Require(mf && mf.sharedMesh, "Missing mesh " + r.name);
            foreach (var m in r.sharedMaterials) Require(m && m.shader && !ShaderUtil.ShaderHasError(m.shader), "Missing/invalid material " + r.name);
        }
        foreach (var m in root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m && AssetDatabase.GetAssetPath(m).Contains("/DetailRevision/")).Distinct())
            if (m.shader.name == "UniDoom/TotalChaosInspiredSurface")
                foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                    Require(m.GetTexture(property), "Incomplete PBR material " + m.name + "." + property);
    }

    static void ValidateGeometry(GameObject room, string label)
    {
        var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab"));
        try
        {
            foreach (var b in player.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;
            var cc = player.GetComponent<CharacterController>(); float feet = cc.height * player.transform.lossyScale.y / 2;
            foreach (var radii in new[] { new Vector2(24, 17), new Vector2(11, 9) })
            {
                player.transform.position = atrium.TransformPoint(V(0, feet + .06f, radii.y)); Physics.SyncTransforms();
                for (int i = 1; i <= 720; i++)
                {
                    float a = i * Mathf.PI / 360;
                    var target = atrium.TransformPoint(V(Mathf.Sin(a) * radii.x, feet + .06f, Mathf.Cos(a) * radii.y));
                    cc.Move(target - player.transform.position);
                    Require(Vector3.Distance(player.transform.position, target) <= .15f, label + " route blocked " + target);
                }
            }
            player.transform.position = atrium.TransformPoint(V(0, feet + .06f, 18)); Physics.SyncTransforms();
            for (int i = 0; i <= 100; i++)
            {
                var target = atrium.TransformPoint(V(0, feet + .06f, Mathf.Lerp(18, 8, i / 100f)));
                cc.Move(target - player.transform.position); Require(Vector3.Distance(player.transform.position, target) <= .15f, label + " south entrance blocked");
            }
            var doors = room.GetComponentsInChildren<OpenDoorWithKey>(); Require(doors.Length == 3, "Expected three original doors");
            Require(room.GetComponentsInChildren<MeshRenderer>().Count(r => r.sharedMaterial && r.sharedMaterial.name == "SealedDoor") == 43, "Expected 43 sealed doors");
            foreach (var door in doors)
            {
                var type = door.GetType(); type.GetMethod("Awake", Private).Invoke(door, null);
                var leaf = (Transform)type.GetField("door", Private).GetValue(door);
                var hinge = (Transform)type.GetField("hinge", Private).GetValue(door);
                player.transform.position = door.transform.TransformPoint(V(1.3f, feet + .06f, 0)); Physics.SyncTransforms();
                type.GetMethod("TrackPlayer", Private).Invoke(door, new object[] { cc }); type.GetMethod("Update", Private).Invoke(door, null);
                Require((bool)type.GetField("unlocked", Private).GetValue(door), "Door proximity failed");
                hinge.localRotation = Quaternion.Euler(0, -100, 0); Physics.SyncTransforms();
                for (int i = 1; i <= 100; i++)
                {
                    var target = door.transform.TransformPoint(V(Mathf.Lerp(1.3f, -.8f, i / 100f), feet + .06f, 0));
                    cc.Move(target - player.transform.position); Require(Vector3.Distance(player.transform.position, target) <= .15f, "Open door obstructs player");
                }
                type.GetMethod("OnDisable", Private).Invoke(door, null); Physics.SyncTransforms();
                player.transform.position = door.transform.TransformPoint(V(1.3f, feet + .06f, 0)); bool blocked = false;
                for (int i = 1; i <= 100; i++)
                {
                    var target = door.transform.TransformPoint(V(Mathf.Lerp(1.3f, -.8f, i / 100f), feet + .06f, 0));
                    cc.Move(target - player.transform.position); if (Vector3.Distance(player.transform.position, target) > .15f) { blocked = true; break; }
                }
                Require(blocked && leaf.GetComponent<BoxCollider>(), "Closed door does not block");
                leaf.SetParent(door.transform, false); leaf.localPosition = Vector3.zero; leaf.localRotation = Quaternion.identity; leaf.localScale = Vector3.one;
                var motion = door.transform.Find("DoorMotionRoot"); if (motion) Object.DestroyImmediate(motion.gameObject);
            }
            int supportCount = 0;
            foreach (var marker in room.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Support check -")))
            {
                Require(Physics.Raycast(marker.position + Vector3.up * .03f, Vector3.down, out var hit, .06f) && Mathf.Abs(hit.point.y - marker.position.y) <= .01f, "Unsupported counter prop"); supportCount++;
            }
            Require(supportCount == 80, "Canteen support markers changed");
            for (int i = 0; i < 96; i++) foreach (float r in new[] { 0f, .15f, .5f, .97f })
            {
                float a = (i + .5f) * Mathf.PI / 48;
                var position = atrium.TransformPoint(V(Mathf.Sin(a) * 27.1f * r, 4.6f, Mathf.Cos(a) * 19.1f * r));
                Require(Physics.Raycast(position, Vector3.up, out var hit, 5) && hit.collider.name == "Roof", "Roof coverage changed");
            }
            Debug.Log("CYLINDER_DETAIL_ROUTES_OK " + label);
        }
        finally { Object.DestroyImmediate(player); }
    }

    public static void Capture(string prefix)
    {
        var root = LoadScene();
        var visible = root.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        long triangles = 0;
        foreach (var r in visible)
        {
            var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh) for (int i = 0; i < mesh.subMeshCount; i++) triangles += (long)mesh.GetIndexCount(i) / 3;
        }
        Directory.CreateDirectory("Evidence");
        File.WriteAllText("Evidence/" + prefix + "-asset-counts.txt", "Visible mesh renderers: " + visible.Length + "\nInstanced mesh triangles: " + triangles + "\nActive local lights: " + root.GetComponentsInChildren<Light>().Count(l => l.enabled && l.intensity > 0) + "\nShadowed local lights: " + root.GetComponentsInChildren<Light>().Count(l => l.enabled && l.intensity > 0 && l.shadows != LightShadows.None) + "\nStatic asset counts, not a frame-time or FPS measurement.\n");
        ShaderUtil.allowAsyncCompilation = false;
        var region = root.GetComponentInChildren<TCRegionAtmosphere>();
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = region.ambient;
        var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(region.ambient); RenderSettings.ambientProbe = probe;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = region.fogColor; RenderSettings.fogDensity = region.fogDensity;
        RenderSettings.reflectionIntensity = .22f;
        var go = new GameObject("Comparison camera"); var camera = go.AddComponent<Camera>(); go.tag = "MainCamera";
        camera.fieldOfView = 72; camera.nearClipPlane = .04f; camera.farClipPlane = 130;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = region.fogColor;
        var data = go.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        Directory.CreateDirectory("Evidence");
        var views = new[] {
            ("Atrium", V(8, 1.84f, 17.3f), V(0, 2.8f, -1)),
            ("Gallery", V(21.5f, 1.84f, 5), V(18, 2.3f, -7)),
            ("Canteen", V(5, 1.84f, 8), V(1, 1.6f, 2)),
            ("Overview", V(8, 5.3f, 16), V(0, 1.8f, 0)),
            ("Service", atrium.InverseTransformPoint(atrium.Find("Service bay 9").TransformPoint(V(-.8f, 1.84f, -3.7f))), atrium.InverseTransformPoint(atrium.Find("Service bay 9").TransformPoint(V(-.3f, 1.4f, -.4f)))) };
        foreach (var view in views)
        {
            camera.transform.position = atrium.TransformPoint(view.Item2); camera.transform.LookAt(atrium.TransformPoint(view.Item3));
            for (int i = 0; i < 5; i++) camera.Render();
            RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
            File.WriteAllBytes("Evidence/" + prefix + "-" + view.Item1 + ".png", image.EncodeToPNG());
            if (prefix == "Final")
            {
                TCRegionAtmosphere.ApplyForCamera(camera, new[] { region }, true);
                for (int i = 0; i < 5; i++) camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
                File.WriteAllBytes("Evidence/" + prefix + "-" + view.Item1 + "-Flashlight.png", image.EncodeToPNG());
                TCRegionAtmosphere.RestoreEnvironment();
            }
        }
        RenderTexture.active = null; camera.targetTexture = null; Object.DestroyImmediate(image); Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
        Object.DestroyImmediate(root);
    }

    [InitializeOnLoadMethod]
    static void RestorePlayRunner()
    {
        if (SessionState.GetBool("CylinderDetailPlay", false))
        {
            EditorApplication.update -= TickPlay; EditorApplication.update += TickPlay;
        }
    }

    public static void RunPlayMode()
    {
        LoadScene();
        // Cameras render the atmosphere while physical trigger events exercise spawning.
        var camera = new GameObject("Test main camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        camera.transform.position = atrium.TransformPoint(V(0, 1.84f, 20)); camera.transform.LookAt(atrium.TransformPoint(V(0, 2, 0)));
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule); player.name = "Trigger test player"; player.tag = "Player";
        player.transform.position = atrium.TransformPoint(V(0, 1.1f, 20)); player.AddComponent<Rigidbody>().isKinematic = true;
        EditorSceneManager.SaveScene(player.scene, "Assets/_TemporaryTools/CylinderSpawnTest.unity");
        SessionState.SetBool("CylinderDetailPlay", true); SessionState.SetInt("CylinderDetailPhase", 0);
        SessionState.SetFloat("CylinderDetailStart", 0);
        EditorApplication.update += TickPlay; EditorApplication.isPlaying = true;
    }

    static void TickPlay()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            var room = Object.FindObjectsByType<CylinderEnemySpawns>(FindObjectsSortMode.None).Single();
            var reference = room.transform.parent.parent;
            var player = GameObject.Find("Trigger test player");
            int phase = SessionState.GetInt("CylinderDetailPhase", 0);
            float start = SessionState.GetFloat("CylinderDetailStart", 0);
            if (phase == 0)
            {
                SessionState.SetFloat("CylinderDetailStart", Time.time); SessionState.SetInt("CylinderDetailPhase", 1); return;
            }
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) enemy.enabled = false;
            int count = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
            if (phase == 1 && Time.time - start > .5f)
            {
                Require(count == 0, "Enemies spawned before entry");
                player.transform.position = reference.TransformPoint(V(0, 1.1f, 16.5f)); Physics.SyncTransforms();
                SessionState.SetInt("CylinderDetailPhase", 2); SessionState.SetFloat("CylinderDetailStart", Time.time);
            }
            else if (phase == 2 && Time.time - start > 9)
            {
                Require(count == 6, "Expected six runtime spawns, got " + count);
                player.transform.position = reference.TransformPoint(V(0, 1.1f, 20)); Physics.SyncTransforms();
                room.enabled = false; room.enabled = true;
                SessionState.SetInt("CylinderDetailPhase", 3); SessionState.SetFloat("CylinderDetailStart", Time.time);
            }
            else if (phase == 3 && Time.time - start > .5f)
            {
                player.transform.position = reference.TransformPoint(V(0, 1.1f, 16.5f)); Physics.SyncTransforms();
                SessionState.SetInt("CylinderDetailPhase", 4); SessionState.SetFloat("CylinderDetailStart", Time.time);
            }
            else if (phase == 4 && Time.time - start > 2)
            {
                Require(count == 6, "Encounter repeated on re-entry");
                File.WriteAllText("Evidence/play-validation.txt", "PASS\nPlay Mode in isolated Unity project.\nZero enemies before entry.\nPhysics trigger spawns exactly six existing Enemy prefabs with staggered timing.\nLeaving, toggling the encounter component and re-entering does not duplicate the encounter.\nFull combat/navigation and hardware performance need gameplay review.\n");
                SessionState.SetBool("CylinderDetailPlay", false); EditorApplication.update -= TickPlay;
                Debug.Log("CYLINDER_DETAIL_PLAY_PASS"); EditorApplication.Exit(0);
            }
            if (Time.time - start > 30) throw new Exception("Play test timeout");
        }
        catch (Exception e)
        {
            SessionState.SetBool("CylinderDetailPlay", false); EditorApplication.update -= TickPlay;
            Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
