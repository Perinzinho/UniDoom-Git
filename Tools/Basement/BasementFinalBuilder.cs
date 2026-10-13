using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Apply on top of the current basement in an isolated Unity project.
public static class BasementFinalBuilder
{
    const string RoomPath = "Assets/prefabs/Scenario/Basement.prefab";
    const string Art = "Assets/Art/Basement";
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Mesh cube;
    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/" + name + ".mat");
    public const float ArenaWidth = 40, ArenaDepth = 34, ArenaHeight = 9.5f;
    public const float BossWidth = 42, BossDepth = 36, BossHeight = 12, DoorHeight = 4.2f;
    public const float ArenaCenterX = 10, BossCenterX = -10;

    public static void Build()
    {
        if(!Application.dataPath.Replace('\\', '/').Contains("/.utmp/")) throw new Exception("Use an isolated Unity project");
        string scenePath = "Assets/Scenes/SampleScene.unity";
        string sceneText = File.ReadAllText(scenePath);
        string removedWall = "    - {fileID: 7914120514677191133, guid: 29573391a8065934c96646bc09b52bcc, type: 3}";
        sceneText = sceneText.Replace(removedWall + "\r\n", "").Replace(removedWall + "\n", "");
        File.WriteAllText(scenePath, sceneText);
        AssetDatabase.ImportAsset(scenePath);
        CaptureAnchors();
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = primitive.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(primitive);
        BasementFinalArt.Build();
        var room = PrefabUtility.LoadPrefabContents(RoomPath);
        try { Resize(room, false); PrefabUtility.SaveAsPrefabAsset(room, RoomPath); }
        finally { PrefabUtility.UnloadPrefabContents(room); }
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map = Object.FindObjectsByType<Transform>().First(t => t.name == "Basement" && t.parent == null).gameObject;
        Resize(map, true); FitDoors(map);
        foreach(var t in map.GetComponentsInChildren<Transform>(true))
        {
            if(!PrefabUtility.IsPartOfPrefabInstance(t)) continue;
            foreach(var c in t.GetComponents<Component>()) if(c) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        EditorSceneManager.SaveScene(map.scene);
        AssetDatabase.SaveAssets(); BasementFinalVerifier.Verify();
        Debug.Log("BASEMENT_FINAL_BUILD_OK");
    }

    static float Axis(Transform t, Vector3 axis) => t.TransformVector(axis).magnitude;
    public static float Floor(Transform area, string name)
    {
        var c = area.Find("Floor/" + name).GetComponent<BoxCollider>();
        return area.InverseTransformPoint(c.transform.TransformPoint(c.center + Vector3.up * c.size.y / 2)).y;
    }
    static void Pose(Transform t, Vector3 position, Vector3 scale)
    { t.localPosition = position; t.localScale = scale; }

    static void Solid(Transform t, Vector3 position, Vector3 size, string material)
    {
        t.gameObject.SetActive(true); t.localRotation = Quaternion.identity; Pose(t, position, size);
        // ProBuilder Awake rebuilds edited blockout meshes even when its behaviour is disabled.
        // Remove only structural ProBuilder components; the before snapshot keeps their source data.
        foreach(var behaviour in t.GetComponents<MonoBehaviour>())
            if(behaviour.GetType().Namespace == "UnityEngine.ProBuilder") Object.DestroyImmediate(behaviour);
        t.GetComponent<MeshFilter>().sharedMesh = cube;
        var renderer = t.GetComponent<MeshRenderer>(); renderer.enabled = true; renderer.sharedMaterial = Mat(material);
        var collider = t.GetComponent<BoxCollider>() ?? t.gameObject.AddComponent<BoxCollider>();
        collider.center = Vector3.zero; collider.size = Vector3.one; collider.enabled = true; collider.isTrigger = false;
        var meshCollider = t.GetComponent<MeshCollider>(); if(meshCollider) { meshCollider.sharedMesh = cube; meshCollider.enabled = false; }
    }
    static void Opening(Transform area, string prefix, float width, float height, float x, float z, float floor, float clearHeight, float roomWidth, float centerX)
    {
        float left = x - width / 2, right = x + width / 2;
        float outerLeft = centerX - roomWidth / 2, outerRight = centerX + roomWidth / 2;
        Solid(area.Find("Walls/" + prefix + "Left"), V((outerLeft + left) / 2, floor + clearHeight / 2, z), V(left - outerLeft, clearHeight + .5f, .5f), "Brick");
        Solid(area.Find("Walls/" + prefix + "Right"), V((outerRight + right) / 2, floor + clearHeight / 2, z), V(outerRight - right, clearHeight + .5f, .5f), "Brick");
        Solid(area.Find("Walls/" + prefix + "Header"), V(x, floor + height + (clearHeight - height) / 2 + .125f, z), V(width, clearHeight - height + .25f, .5f), "Brick");
    }
    static void Resize(GameObject room, bool map)
    {
        var arena = room.transform.Find("PreBossArea"); var boss = room.transform.Find("BossRoom");
        // Use original snapshots in both passes, so map compression can be cancelled without moving access points.
        var entry = arena.Find("Walls/Entrance_Wall_Header");
        var bossEntry = boss.Find("Walls/Entrance_Wall_Header");
        Vector3 entryWorld = entry.position, bossEntryWorld = bossEntry.position;
        Vector3 arenaFloorWorld = arena.TransformPoint(V(0, Floor(arena, "Hall_Floor"), 0));
        Vector3 bossFloorWorld = boss.TransformPoint(V(0, Floor(boss, "Main_Floor"), 0));
        if(map)
        {
            // The source prefab has already been enlarged. Access anchors come from the before snapshot.
            string data = File.ReadAllText("Preview/Basement-final-anchors.json");
            var anchors = JsonUtility.FromJson<Anchors>(data);
            entryWorld = anchors.entry; bossEntryWorld = anchors.bossEntry;
            arenaFloorWorld = anchors.arenaFloor; bossFloorWorld = anchors.bossFloor;
            foreach(var area in new[] { arena, boss })
            {
                area.localScale = Vector3.one;
                area.localScale = V(1 / Axis(area, Vector3.right), 1 / Axis(area, Vector3.up), 1 / Axis(area, Vector3.forward));
            }
        }
        float af = arena.InverseTransformPoint(arenaFloorWorld).y, bf = boss.InverseTransformPoint(bossFloorWorld).y;
        float entryX = arena.InverseTransformPoint(entryWorld).x;
        float front = boss.InverseTransformPoint(bossEntryWorld).z;
        float bossX = boss.InverseTransformPoint(bossEntryWorld).x;
        Solid(arena.Find("Floor/Hall_Floor"), V(ArenaCenterX, af - .25f, 2), V(ArenaWidth, .5f, ArenaDepth + .5f), "Floor");
        Solid(arena.Find("Walls/Hall_Ceiling"), V(ArenaCenterX, af + ArenaHeight + .25f, 2), V(ArenaWidth + .5f, .5f, ArenaDepth + .5f), "Ceiling");
        foreach(int side in new[] { -1, 1 })
            Solid(arena.Find("Walls/" + (side < 0 ? "West_Wall" : "East_Wall")), V(ArenaCenterX + side * ArenaWidth / 2, af + ArenaHeight / 2, 2), V(.5f, ArenaHeight + .5f, ArenaDepth + .5f), "Brick");
        Opening(arena, "Entrance_Wall_", 3.6f, DoorHeight, entryX, -15, af, ArenaHeight, ArenaWidth, ArenaCenterX);
        Opening(arena, "Exit_Wall_", 3.6f, DoorHeight, 0, 19, af, ArenaHeight, ArenaWidth, ArenaCenterX);
        Solid(arena.Find("Player entrance threshold"), V(entryX, af - .15f, -15.65f), V(3.6f, .3f, 2), "Floor");
        foreach(var extra in arena.Find("Walls").Cast<Transform>())
            if(extra.name.Contains("(1)")) extra.gameObject.SetActive(false);
        Solid(boss.Find("Floor/Main_Floor"), V(BossCenterX, bf - .25f, front + BossDepth / 2), V(BossWidth, .5f, BossDepth + .5f), "Floor");
        Solid(boss.Find("Walls/Main_Ceiling"), V(BossCenterX, bf + BossHeight + .25f, front + BossDepth / 2), V(BossWidth + .5f, .5f, BossDepth + .5f), "Ceiling");
        foreach(int side in new[] { -1, 1 })
            Solid(boss.Find("Walls/" + (side < 0 ? "West_Wall" : "East_Wall")), V(BossCenterX + side * BossWidth / 2, bf + BossHeight / 2, front + BossDepth / 2), V(.5f, BossHeight + .5f, BossDepth + .5f), "Stone");
        Solid(boss.Find("Walls/Back_Wall"), V(BossCenterX, bf + BossHeight / 2, front + BossDepth), V(BossWidth + .5f, BossHeight + .5f, .5f), "Stone");
        Opening(boss, "Entrance_Wall_", 3.6f, DoorHeight, bossX, front, bf, BossHeight, BossWidth, BossCenterX);
        ArenaFurniture(arena, af); BossFurniture(boss, bf, front);
        InstallDetails(arena, "BasementFinalArena", V(ArenaCenterX, af, 0));
        InstallDetails(boss, "BasementFinalSanctum", V(BossCenterX, bf, front));
        var encounter = room.GetComponentInChildren<BasementArenaEncounter>();
        encounter.arenaCenter = V(ArenaCenterX, af + ArenaHeight / 2, 2); encounter.arenaSize = V(ArenaWidth - .6f, ArenaHeight, ArenaDepth - .6f);
        for(int i = 0; i < encounter.spawnPoints.Length; i++)
            encounter.spawnPoints[i].localPosition = V(ArenaCenterX + (i % 2 == 0 ? -12 : 12), af + 1, -11 + (i / 2) * 4.4f);
        room.GetComponentInChildren<BasementGateLayout>().Fit();
        Corridor(arena, boss, af, bf, front, bossX);
    }
    [Serializable] class Anchors { public Vector3 entry, bossEntry, arenaFloor, bossFloor; }
    public static void CaptureAnchors()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room = Object.FindObjectsByType<Transform>().First(t => t.name == "Basement" && t.parent == null);
        var arena = room.Find("PreBossArea"); var boss = room.Find("BossRoom");
        var a = new Anchors { entry = arena.Find("Walls/Entrance_Wall_Header").position, bossEntry = boss.Find("Walls/Entrance_Wall_Header").position,
            arenaFloor = arena.TransformPoint(V(0, Floor(arena, "Hall_Floor"), 0)), bossFloor = boss.TransformPoint(V(0, Floor(boss, "Main_Floor"), 0)) };
        Directory.CreateDirectory("Preview"); File.WriteAllText("Preview/Basement-final-anchors.json", JsonUtility.ToJson(a));
    }
    static void InstallDetails(Transform area, string name, Vector3 position)
    {
        var t = area.Find(name);
        if(!t) { var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/" + name + ".prefab"), area.gameObject.scene); t = go.transform; t.SetParent(area, false); }
        Pose(t, position, Vector3.one);
    }
    static void ArenaFurniture(Transform arena, float floor)
    {
        var details = arena.Find("BasementArenaDetails");
        foreach(Transform t in details)
        {
            float side = Mathf.Sign(t.localPosition.x);
            bool adjusted = true;
            if(t.name.StartsWith("Storage drum"))
            { float x = t.name.Contains("11.40") ? 18 : 16.5f; Pose(t, V(side * x, floor, side > 0 ? -4 : 7), V(1.35f, 1.55f, 1.35f)); }
            else if(t.name.StartsWith("Arena wall trim")) Pose(t, V(side * 19.7f, floor - .25f, 2), V(1, ArenaHeight / 4, ArenaDepth / 30));
            else if(t.name.StartsWith("Steam feed")) Pose(t, V(side * 19.3f, floor + 6.7f, 2), V(1, 1, ArenaDepth / 29));
            else if(t.name.StartsWith("Riveted pressure boiler")) Pose(t, V(side * 17.7f, floor, side > 0 ? -10 : 14), Vector3.one * 1.35f);
            else if(t.name.StartsWith("Electrical control cabinet")) Pose(t, V(side * 18.7f, floor, side > 0 ? 14 : -11), Vector3.one * 1.45f);
            else if(t.name.StartsWith("Ceiling joist")) Pose(t, V(0, floor + 8.95f, t.localPosition.z), V(ArenaWidth / 25.5f, 1, 1));
            else if(t.name.StartsWith("Hanging industrial fixture")) Pose(t, V(0, floor + 8.6f, t.localPosition.z), Vector3.one * 1.25f);
            else if(t.name == "Overhead cable trays") Pose(t, V(0, floor + 8.85f, 2), V(1.6f, 1, ArenaDepth / 29));
            else if(t.name.StartsWith("Arena drainage")) Pose(t, V(t.name.EndsWith("-1") ? -4.8f : 4.8f, floor + .014f, 2), V(1.4f, 1, ArenaDepth / 28));
            else if(t.name.StartsWith("Amber caged wall light")) Pose(t, V(side * 19.3f, floor + 3.2f, t.localPosition.z), Vector3.one * 1.3f);
            else if(t.name == "Arena warning") Pose(t, V(-3.5f, floor + 2.9f, 18.69f), Vector3.one * 1.3f);
            else adjusted = false;
            if(adjusted) t.localPosition += Vector3.right * ArenaCenterX;
        }
        var pillars = arena.Find("Pillar").Cast<Transform>().ToArray();
        for(int i = 0; i < pillars.Length; i++)
        {
            float side = Mathf.Sign(pillars[i].localPosition.x), z = pillars[i].localPosition.z;
            Pose(pillars[i], V(ArenaCenterX + side * 11, floor + ArenaHeight / 2, z), V(.65f, ArenaHeight, .65f));
            Solid(pillars[i], pillars[i].localPosition, pillars[i].localScale, "Stone");
        }
        foreach(Transform t in details) if(t.name.StartsWith("Pillar iron collar")) Pose(t, V(ArenaCenterX + Mathf.Sign(t.localPosition.x) * 11, floor - .25f, t.localPosition.z), V(1, ArenaHeight / 4, 1));
        foreach(Transform t in arena.Find("Floor")) if(t.name.StartsWith("Plataform")) Pose(t, V(ArenaCenterX + Mathf.Sign(t.localPosition.x) * 16.5f, floor + .5f, t.localPosition.z), V(5, 1, 8));
    }
    static void BossFurniture(Transform boss, float floor, float front)
    {
        var details = boss.Find("BasementBossDetails");
        foreach(Transform t in details)
        {
            float side = Mathf.Sign(t.localPosition.x);
            bool adjusted = true;
            if(t.name == "Boss sanctum masonry") Pose(t, V(0, floor - .25f, front + 18), V(BossWidth / 18, BossHeight / 5, BossDepth / 18));
            else if(t.name == "Boss altar and infernal engine") Pose(t, V(0, floor + 1, front + 31.5f), V(2.0f, 1.6f, 1.8f));
            else if(t.name == "Infernal seal wall") Pose(t, V(0, floor + 6.2f, front + 35.64f), V(3, 2.9f, 1));
            else if(t.name == "Boss ritual floor") Pose(t, V(0, floor + .024f, front + 18.5f), V(2.25f, 1, 2.25f));
            else if(t.name == "FinalBossSpawn") Pose(t, V(0, floor + 1.10f, front + 18.5f), Vector3.one);
            else if(t.name.StartsWith("Boss overhead pipe")) Pose(t, V(side * 19.3f, floor + 10.7f, front + 18), V(1.4f, 1.4f, BossDepth / 17));
            else if(t.name.StartsWith("Electrical control cabinet")) Pose(t, V(side * 18.5f, floor, front + 30), Vector3.one * 1.5f);
            else if(t.name.StartsWith("Red caged wall light")) Pose(t, V(side * 20.2f, floor + 4.2f, front + (t.name.Contains("-5.00") ? 6 : t.name.Contains("0.00") ? 18 : 30)), Vector3.one * 1.5f);
            else if(t.name == "Final chamber") Pose(t, V(4.0f, floor + 3.1f, front + .33f), Vector3.one * 1.4f);
            else if(t.name == "Local light") Pose(t, V(0, floor + 8, front + 18), Vector3.one);
            else adjusted = false;
            if(adjusted) t.localPosition += Vector3.right * BossCenterX;
        }
        var pillars = boss.Find("Pillar").Cast<Transform>().ToArray();
        for(int i = 0; i < pillars.Length; i++)
        {
            float x = BossCenterX + Mathf.Sign(pillars[i].localPosition.x) * 12, z = front + (pillars[i].name == "Pillar02" || pillars[i].name == "Pillar03" ? 10 : 25);
            Solid(pillars[i], V(x, floor + BossHeight / 2, z), V(.95f, BossHeight, .95f), "Stone");
        }
        foreach(Transform t in details) if(t.name.StartsWith("Boss column reinforcement")) Pose(t, V(BossCenterX + Mathf.Sign(t.localPosition.x) * 12, floor - .25f, front + (t.name.Replace(',', '.').EndsWith("-2.88") ? 10 : 25)), V(1.15f, BossHeight / 5, 1.15f));
        Solid(boss.Find("Floor/Plataform"), V(BossCenterX, floor + .5f, front + 31), V(12, 1, 7), "Stone");
    }
    static void Corridor(Transform arena, Transform boss, float af, float bf, float front, float cx)
    {
        var gate = arena.GetComponentInChildren<BasementGateLayout>();
        Vector3 exit = boss.InverseTransformPoint(gate.gateFrame.position);
        var root = boss.Find("Player sized corridor");
        foreach(var t in boss.GetComponentsInChildren<Transform>(true)) if(t.name.StartsWith("Hallway", StringComparison.OrdinalIgnoreCase)) t.gameObject.SetActive(false);
        float w = 3.6f, lowZ = exit.z - w / 2, highZ = front + .3f, leftX = cx - w / 2, rightX = exit.x + .5f;
        float h = 5.2f;
        Solid(root.Find("Passage Floor X"), V((leftX + rightX) / 2, bf - .15f, exit.z), V(rightX - leftX, .3f, w), "Floor");
        Solid(root.Find("Passage Floor Z"), V(cx, bf - .15f, (lowZ + highZ) / 2), V(w, .3f, highZ - lowZ), "Floor");
        Solid(root.Find("Passage Ceiling X"), V((leftX + rightX) / 2, bf + h + .15f, exit.z), V(rightX - leftX, .3f, w), "Ceiling");
        Solid(root.Find("Passage Ceiling Z"), V(cx, bf + h + .15f, (lowZ + highZ) / 2), V(w, .3f, highZ - lowZ), "Ceiling");
        Solid(root.Find("Passage South wall"), V((leftX + rightX) / 2, bf + h / 2, lowZ - .15f), V(rightX - leftX, h, .3f), "Brick");
        Solid(root.Find("Passage West wall"), V(leftX - .15f, bf + h / 2, (lowZ + highZ) / 2), V(.3f, h, highZ - lowZ), "Brick");
        float innerX = cx + w / 2, innerZ = exit.z + w / 2;
        Solid(root.Find("Passage North wall"), V((innerX + rightX) / 2, bf + h / 2, innerZ + .15f), V(rightX - innerX, h, .3f), "Brick");
        Solid(root.Find("Passage East wall"), V(innerX + .15f, bf + h / 2, (innerZ + highZ) / 2), V(.3f, h, highZ - innerZ), "Brick");
    }
    static void FitDoors(GameObject room)
    {
        foreach(var door in Object.FindObjectsByType<OpenDoorWithKey>())
        {
            if(door.name != "DoorPrefab (4)" && door.name != "DoorPrefab (6)") continue;
            var area = room.transform.Find(door.name == "DoorPrefab (4)" ? "PreBossArea" : "BossRoom");
            var header = area.Find("Walls/Entrance_Wall_Header"); var leaf = door.transform.Find("Door");
            var collider = leaf.GetComponent<BoxCollider>();
            var center = area.TransformPoint(V(header.localPosition.x, Floor(area, area.name == "BossRoom" ? "Main_Floor" : "Hall_Floor") + DoorHeight / 2, header.localPosition.z));
            door.transform.position = center; door.transform.rotation = area.rotation * Quaternion.Euler(0, -90, 0); door.transform.localScale = Vector3.one;
            leaf.localPosition = Vector3.zero; leaf.localRotation = Quaternion.identity; leaf.localScale = V(.16f, DoorHeight - .025f, 3.6f - .055f);
            collider.center = Vector3.zero; collider.size = Vector3.one;
            var trigger = door.GetComponent<BoxCollider>(); trigger.center = Vector3.zero; trigger.size = V(3.2f, DoorHeight, 3.6f);
            var renderer = door.GetComponent<MeshRenderer>(); if(renderer) renderer.enabled = false;
            leaf.GetComponent<MeshRenderer>().sharedMaterial = Mat("Steel");
            var settings = new SerializedObject(door);
            settings.FindProperty("hingePoint").vector3Value = V(0, 0, -.5f + .15f / (3.6f - .055f));
            settings.ApplyModifiedPropertiesWithoutUndo();
            foreach(var t in door.GetComponentsInChildren<Transform>())
            {
                foreach(var c in t.GetComponents<Component>()) if(c) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
        }
    }

    public static void FixDecorativeOffsetsAndVerify()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map = Object.FindObjectsByType<Transform>().First(t => t.name == "Basement" && t.parent == null);
        var arena = map.Find("PreBossArea"); var boss = map.Find("BossRoom");
        foreach(Transform t in arena.Find("Walls")) if(t.name.Contains("(1)")) t.gameObject.SetActive(false);
        foreach(Transform t in arena.Find("BasementArenaDetails")) if(t.name.StartsWith("Arena drainage"))
        {
            t.localPosition = V(ArenaCenterX + (t.name.EndsWith("-1") ? -4.8f : 4.8f), Floor(arena, "Hall_Floor") + .014f, 2);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        foreach(Transform t in boss.Find("BasementBossDetails")) if(t.name.StartsWith("Red caged wall light"))
        {
            t.localPosition = V(t.localPosition.x, t.localPosition.y, boss.Find("Walls/Entrance_Wall_Header").localPosition.z + (t.name.Contains("-5.00") ? 6 : t.name.Contains("0.00") ? 18 : 30));
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        EditorSceneManager.SaveScene(map.gameObject.scene); BasementFinalVerifier.Verify();
    }

    public static void Audit()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
        Report(room, "PREFAB");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Report(Object.FindObjectsByType<Transform>().First(t => t.name == "Basement" && t.parent == null).gameObject, "MAP");
    }
    static void Report(GameObject room, string label)
    {
        Physics.SyncTransforms();
        foreach(var area in new[] { room.transform.Find("PreBossArea"), room.transform.Find("BossRoom") })
        {
            Debug.Log("FINAL_AUDIT " + label + " " + area.name + " worldScale=" + area.lossyScale + " origin=" + area.position);
            foreach(var c in area.GetComponentsInChildren<BoxCollider>())
            {
                if(c.transform.parent.name != "Walls" && c.transform.parent.name != "Floor" && !c.name.StartsWith("Storage drum")) continue;
                var mesh = c.GetComponent<MeshFilter>();
                Debug.Log("FINAL_AUDIT " + label + " " + area.name + "/" + c.name + " pos=" + c.transform.localPosition + " scale=" + c.transform.localScale + " collider=" + c.center + "/" + c.size + " mesh=" + (mesh && mesh.sharedMesh ? mesh.sharedMesh.bounds.ToString() : "missing"));
            }
        }
        if(label == "MAP") foreach(var root in room.scene.GetRootGameObjects())
        {
            var colliders = root.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled).ToArray();
            if(colliders.Length == 0) continue;
            var bounds = colliders[0].bounds; foreach(var c in colliders.Skip(1)) bounds.Encapsulate(c.bounds);
            Debug.Log("FINAL_MAP_ROOT " + root.name + " " + bounds);
        }
    }
}
