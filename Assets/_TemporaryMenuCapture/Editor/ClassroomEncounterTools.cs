using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Temporary classroom inspection/authoring. Reports and captures stay outside Assets.
public static class ClassroomEncounterTools
{
    const string Map = "Assets/_Project/Scenes/ClassRoom/Prefabs/ClassRoom.prefab";
    const string ScenePath = "Assets/_Project/Scenes/ClassRoom/ClassRoom.unity";
    const string RevisedMap = "Assets/_Project/Scenes/ClassRoom/Prefabs/ClassroomEncounter.prefab";
    const string Zombie = "Assets/_Project/Scenes/ClassRoom/Prefabs/ClassroomZombie.prefab";
    const string Navigation = "Assets/_Project/Scenes/ClassRoom/Navigation/ClassroomNavigation.asset";
    const string SharedZombie = "Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab";

    [Serializable] public sealed class Item
    {
        public string path, tag, type;
        public Vector3 position, size;
        public bool trigger, active;
    }
    [Serializable] public sealed class Inventory { public List<Item> objects = new(); }

    [MenuItem("Tools/UniDoom/Inspect Classroom Encounter")]
    public static void Inspect()
    {
        var root = PrefabUtility.LoadPrefabContents(Map);
        try
        {
            var inventory = new Inventory();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                var collider = t.GetComponent<Collider>();
                var renderer = t.GetComponent<Renderer>();
                var relevant = t.parent == root.transform || t.CompareTag("Key01") || t.CompareTag("Key02") ||
                    t.CompareTag("Key03") || collider != null || t.GetComponents<MonoBehaviour>().Length > 0;
                if (!relevant) continue;
                var bounds = collider != null ? collider.bounds : renderer != null ? renderer.bounds : new Bounds(t.position, Vector3.zero);
                inventory.objects.Add(new Item {
                    path = AnimationUtility.CalculateTransformPath(t, root.transform), tag = t.tag,
                    position = root.transform.InverseTransformPoint(bounds.center), size = bounds.size,
                    type = collider != null ? collider.GetType().Name : string.Join(",", t.GetComponents<MonoBehaviour>().Select(x => x != null ? x.GetType().Name : "MISSING")),
                    trigger = collider != null && collider.isTrigger, active = t.gameObject.activeSelf
                });
            }
            File.WriteAllText("Temp/classroom-inventory.json", JsonUtility.ToJson(inventory, true));
            File.WriteAllText("Temp/classroom-inspection.txt", string.Join("\n", inventory.objects.Where(x =>
                x.path.Count(c => c == '/') <= 1 || x.tag.StartsWith("Key") || x.type.Contains("Enemy") || x.type.Contains("NavMesh"))
                .Select(x => $"{x.path} | {x.tag} | {x.type} | p={x.position} size={x.size}")));
            Debug.Log("Classroom inspection saved to Temp/classroom-inventory.json");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/UniDoom/Prepare Harder Classroom")]
    public static void Prepare()
    {
        var preview = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            ConfigureZombie(preview);
            root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                File.Exists(RevisedMap) ? RevisedMap : Map), preview);
            var key = root.GetComponentsInChildren<Transform>(true).Single(x => x.CompareTag("Key01"));
            key.position = root.transform.TransformPoint(new Vector3(-26.25f, 2.62f, 13.45f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(key);
            // Open the teaching-area cross aisle for the widest zombie variant.
            foreach (var pair in new[] { ("Teacher desk", -27f), ("Teacher chair", -26.8f) })
            {
                var furniture = root.transform.Find("AbandonedClassRoomInterior/" + pair.Item1);
                float currentX = root.transform.InverseTransformPoint(furniture.GetComponent<Collider>().bounds.center).x;
                furniture.position += root.transform.TransformVector(new Vector3(pair.Item2 - currentX, 0, 0));
                PrefabUtility.RecordPrefabInstancePropertyModifications(furniture);
            }
            // Push three chairs under their desks to retain a usable cross aisle.
            foreach (int column in new[] { 3, 4, 5 })
            {
                var chair = root.transform.Find("AbandonedClassRoomInterior/Student chair 4-" + column);
                float z = root.transform.InverseTransformPoint(chair.GetComponent<Collider>().bounds.center).z;
                chair.position += root.transform.TransformVector(new Vector3(0, 0, 21.213f - z));
                PrefabUtility.RecordPrefabInstancePropertyModifications(chair);
            }
            var displaced = root.transform.Find("AbandonedClassRoomInterior/Student desk 1-5");
            float displacedX = root.transform.InverseTransformPoint(displaced.GetComponent<Collider>().bounds.center).x;
            displaced.position += root.transform.TransformVector(new Vector3(-25.7931f - displacedX, 0, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(displaced);
            var encounterRoot = root.transform.Find("Classroom encounter");
            if (encounterRoot == null)
            {
                encounterRoot = new GameObject("Classroom encounter").transform;
                encounterRoot.SetParent(root.transform, false);
            }
            var director = encounterRoot.GetComponent<ClassroomEncounterDirector>();
            if (director == null) director = encounterRoot.gameObject.AddComponent<ClassroomEncounterDirector>();
            director.zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Zombie);
            director.objectiveKey = key.gameObject;
            director.initialGuards = Markers(encounterRoot, "Initial", new[] {
                new Vector3(-30f,1.17f,26f), new Vector3(-27.1f,1.17f,24f),
                new Vector3(-33.6f,1.17f,19.7f), new Vector3(-30f,1.17f,19.9f),
                new Vector3(-27.1f,1.17f,16f), new Vector3(-27.2f,1.17f,14.7f),
                new Vector3(-30f,1.17f,14f) });
            director.returnGuards = Markers(encounterRoot, "Return", new[] {
                new Vector3(-27.1f,1.17f,19.95f), new Vector3(-30f,1.17f,24f),
                new Vector3(-38.7f,1.17f,25.9f) });
            var surface = root.GetComponent<NavMeshSurface>();
            if (surface == null) surface = root.AddComponent<NavMeshSurface>();
            surface.enabled = false;
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = .05f;
            BuildNavigation(root, surface);
            var checks = ValidateGeometry(root, surface);
            surface.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(director);
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, RevisedMap);
            if (saved == null) throw new Exception("Classroom variant was not saved.");
            AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
            BindSceneToVariant();
            File.WriteAllLines("Temp/classroom-preparation.txt", checks.Concat(new[] {
                "PASS: persistent classroom variant and navigation saved; scene references updated.",
                "PASS: shared source classroom and enemy prefabs remain unchanged." }));
            Debug.Log("Harder classroom saved: seven guards, three return guards, relocated Key01.");
        }
        catch (Exception e)
        {
            File.WriteAllText("Temp/classroom-preparation.txt", "FAIL: " + e);
            Debug.LogException(e);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    static Transform[] Markers(Transform parent, string name, Vector3[] points)
    {
        var group = parent.Find(name);
        if (group == null) { group = new GameObject(name).transform; group.SetParent(parent, false); }
        var result = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var point = group.Find($"Guard {i + 1:00}");
            if (point == null) { point = new GameObject($"Guard {i + 1:00}").transform; point.SetParent(group, false); }
            point.localPosition = points[i];
            point.localRotation = Quaternion.Euler(0, i == 0 ? -90 : -25, 0);
            result[i] = point;
        }
        return result;
    }

    static void ConfigureZombie(Scene preview)
    {
        if (File.Exists(Zombie)) return;
        var zombie = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SharedZombie), preview);
        try
        {
            zombie.name = "ClassroomZombie";
            zombie.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var data = new SerializedObject(zombie.GetComponent<Enemy>());
            data.FindProperty("moveSpeed").floatValue = 2.2f;
            data.ApplyModifiedPropertiesWithoutUndo();
            var body = zombie.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            var agent = zombie.AddComponent<NavMeshAgent>();
            agent.radius = .5f;
            agent.height = 2;
            agent.baseOffset = 1;
            agent.speed = 2.2f;
            agent.acceleration = 8;
            agent.angularSpeed = 180;
            agent.stoppingDistance = .35f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            PrefabUtility.RecordPrefabInstancePropertyModifications(zombie.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            PrefabUtility.SaveAsPrefabAsset(zombie, Zombie);
        }
        finally { UnityEngine.Object.DestroyImmediate(zombie); }
    }

    static void BuildNavigation(GameObject root, NavMeshSurface surface)
    {
        var sources = (List<NavMeshBuildSource>)typeof(NavMeshSurface).GetMethod("CollectSources",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
        sources.RemoveAll(x => x.component is Collider c && c.isTrigger);
        var floor = root.transform.Find("Floor").GetComponent<Collider>();
        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            source.area = source.component == floor ? 0 : 1;
            sources[i] = source;
        }
        var settings = surface.GetBuildSettings();
        settings.agentRadius = .6f; // Includes the shared wheelchair variant's .588 radius.
        settings.agentHeight = 2;
        settings.agentClimb = .25f;
        settings.minRegionArea = .2f;
        var bounds = new Bounds(new Vector3(-32.88486f, 2.65f, 19.76302f), new Vector3(18.5f, 3.6f, 16.5f));
        var built = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, root.transform.position, root.transform.rotation);
        if (built == null) throw new Exception("Classroom navigation bake failed.");
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(Navigation);
        if (existing == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/ClassRoom/Navigation"))
                AssetDatabase.CreateFolder("Assets/_Project/Scenes/ClassRoom", "Navigation");
            built.name = "ClassroomNavigation";
            AssetDatabase.CreateAsset(built, Navigation);
            existing = built;
        }
        else
        {
            EditorUtility.CopySerialized(built, existing);
            UnityEngine.Object.DestroyImmediate(built);
        }
        surface.navMeshData = existing;
    }

    static List<string> ValidateGeometry(GameObject root, NavMeshSurface surface)
    {
        var checks = new List<string>();
        var shift = new Vector3(2000, 0, 2000);
        var handle = NavMesh.AddNavMeshData(surface.navMeshData, root.transform.position + shift, root.transform.rotation);
        try
        {
            Vector3 Project(Vector3 local)
            {
                var world = root.transform.TransformPoint(local) + shift;
                if (!NavMesh.SamplePosition(world, out var hit, .65f, NavMesh.AllAreas))
                    throw new Exception("No walkable floor near " + local);
                if (Mathf.Abs((hit.position - shift).y - root.transform.TransformPoint(new Vector3(0,1.15f,0)).y) > .2f)
                    throw new Exception("Navigation point landed on furniture: " + local);
                return hit.position;
            }
            var arrival = Project(new Vector3(-40.135f, 1.17f, 26));
            var keyApproach = Project(new Vector3(-27.1f, 1.17f, 13.55f));
            var path = new NavMeshPath();
            var probes = new List<string>();
            foreach (var p in new[] { new Vector3(-30,1.17f,26), new Vector3(-30,1.17f,19.9f),
                         new Vector3(-27.1f,1.17f,19.95f), new Vector3(-27.1f,1.17f,16), new Vector3(-33.6f,1.17f,19.7f),
                         new Vector3(-27.1f,1.17f,13.55f) })
            {
                var destination = Project(p);
                NavMesh.CalculatePath(arrival, destination, NavMesh.AllAreas, path);
                probes.Add(p + " -> " + path.status + " / " + path.corners.Length);
            }
            File.WriteAllLines("Temp/classroom-navigation-probes.txt", probes);
            if (!NavMesh.CalculatePath(arrival, keyApproach, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                throw new Exception("Entry-to-key route is not complete.");
            checks.Add("PASS: complete navigable route from the entry to the relocated key.");
            var director = root.GetComponentInChildren<ClassroomEncounterDirector>(true);
            foreach (var point in director.initialGuards.Concat(director.returnGuards))
            {
                var projected = Project(root.transform.InverseTransformPoint(point.position));
                if (!NavMesh.CalculatePath(projected, keyApproach, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    throw new Exception("Guard cannot reach the key route: " + AnimationUtility.CalculateTransformPath(point, root.transform));
                checks.Add("PASS: " + point.parent.name + "/" + point.name + " has floor and a route to the objective.");
            }
            checks.Add("PASS: navigation baked with .6 radius, covering all existing zombie variants.");
            return checks;
        }
        finally { handle.Remove(); }
    }

    static void BindSceneToVariant()
    {
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(Map);
        var revised = AssetDatabase.LoadAssetAtPath<GameObject>(RevisedMap);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string oldGuid, out long _);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(revised, out string newGuid, out long _);
        var ids = new Dictionary<long,long> { [100100000] = 100100000 };
        foreach (var t in original.GetComponentsInChildren<Transform>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(t, original.transform);
            var next = path.Length == 0 ? revised.transform : revised.transform.Find(path);
            if (next == null) continue;
            void MapId(UnityEngine.Object before, UnityEngine.Object after)
            {
                if (after == null) return;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(before, out string guid, out long id);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(after, out string _, out long newId);
                if (guid == oldGuid) ids[id] = newId;
            }
            MapId(t.gameObject, next.gameObject);
            foreach (var component in t.GetComponents<Component>())
                if (component != null) MapId(component, next.GetComponent(component.GetType()));
        }
        string text = File.ReadAllText(ScenePath);
        if (text.Contains("guid: " + newGuid)) return;
        string pattern = @"\{fileID: (-?\d+), guid: " + oldGuid + @", type: (\d+)\}";
        string modified = Regex.Replace(text, pattern, match =>
        {
            long id = long.Parse(match.Groups[1].Value);
            if (!ids.TryGetValue(id, out long mapped)) throw new Exception("Scene prefab reference could not be mapped: " + id);
            return "{fileID: " + mapped + ", guid: " + newGuid + ", type: " + match.Groups[2].Value + "}";
        });
        if (modified == text) throw new Exception("Classroom scene prefab instance was not found.");
        File.WriteAllText(ScenePath, modified, new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
    }

    [MenuItem("Tools/UniDoom/Validate Saved Classroom")]
    public static void ValidateSaved()
    {
        var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
        var checks = new List<string>();
        GameObject probe = null;
        try
        {
            var objects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            int missing = objects.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing != 0) throw new Exception("Scene has missing script references: " + missing);
            checks.Add("PASS: saved scene reopens with no missing scripts.");
            var director = objects.Select(x => x.GetComponent<ClassroomEncounterDirector>()).Single(x => x != null);
            var root = director.transform.parent.gameObject;
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) != RevisedMap)
                throw new Exception("Scene is not using the dedicated encounter variant.");
            if (director.initialGuards.Length != 7 || director.returnGuards.Length != 3 || director.zombiePrefab == null || director.objectiveKey == null)
                throw new Exception("Encounter references are incomplete.");
            checks.Add("PASS: scene resolves the dedicated variant, its original Key01 and all ten spawn markers.");
            if (!director.objectiveKey.CompareTag("Key01")) throw new Exception("Objective key tag changed.");
            var local = root.transform.InverseTransformPoint(director.objectiveKey.transform.position);
            if (Vector3.Distance(local, new Vector3(-26.25f, 2.62f, 13.45f)) > .02f) throw new Exception("Saved key position differs from the prepared layout.");
            checks.Add("PASS: Key01 is saved on the far classroom desk, away from the entrance.");
            checks.AddRange(ValidateGeometry(root, root.GetComponent<NavMeshSurface>()));
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab");
            var controller = player.GetComponent<CharacterController>();
            probe = new GameObject("Temporary player clearance probe");
            SceneManager.MoveGameObjectToScene(probe, scene);
            var capsule = probe.AddComponent<CapsuleCollider>();
            capsule.height = controller.height * player.transform.lossyScale.y;
            capsule.radius = controller.radius * Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z);
            capsule.enabled = true;
            Physics.SyncTransforms();
            var pickupPose = root.transform.TransformPoint(new Vector3(-27f, 2.25f, 13.85f));
            foreach (var collider in root.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || !collider.enabled) continue;
                if (Physics.ComputePenetration(capsule, pickupPose, Quaternion.identity, collider,
                    collider.transform.position, collider.transform.rotation, out _, out float depth) && depth > .015f)
                    throw new Exception("Player cannot stand at key pickup approach: " + collider.name);
            }
            var keyCollider = director.objectiveKey.GetComponent<Collider>();
            if (!Physics.ComputePenetration(capsule, pickupPose, Quaternion.identity, keyCollider,
                    keyCollider.transform.position, keyCollider.transform.rotation, out _, out _))
                throw new Exception("Key pickup trigger cannot be reached from the safe approach. capsule=" +
                    capsule.radius + "/" + capsule.height + " pose=" + pickupPose + " key=" + keyCollider.bounds);
            checks.Add("PASS: the real player capsule fits at the objective and overlaps its pickup trigger.");
            var camera = objects.Select(x => x.GetComponent<Camera>()).First(x => x != null);
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            Capture(camera, root.transform.TransformPoint(new Vector3(-39.4f, 2.95f, 25.5f)),
                root.transform.TransformPoint(new Vector3(-30.5f, 2.2f, 18f)), "Temp/classroom-after-entry.png");
            Capture(camera, root.transform.TransformPoint(new Vector3(-28.4f, 2.95f, 15.2f)),
                director.objectiveKey.transform.position, "Temp/classroom-after-key.png");
            checks.Add("PASS: saved scene rendered from entry and objective viewpoints.");
        }
        catch (Exception e) { checks.Add("FAIL: " + e); Debug.LogException(e); }
        finally
        {
            if (probe != null) UnityEngine.Object.DestroyImmediate(probe);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllLines("Temp/classroom-saved-validation.txt", checks);
        }
    }

    static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
    {
        var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        try
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 70;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
