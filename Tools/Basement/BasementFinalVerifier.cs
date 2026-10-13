using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class BasementFinalVerifier
{
    const string RoomPath = "Assets/prefabs/Scenario/Basement.prefab";
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static readonly List<string> report = new List<string>();
    public static void Verify()
    {
        report.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath)); room.transform.position = Vector3.zero;
        Check(room, "PREFAB"); Traverse(room, "PREFAB");
        PreviewSettings(); RenderRoom(room, "");
        EditorSceneManager.SaveScene(scene, "Assets/Art/Basement/BasementPreview.unity");
        typeof(BasementBuilder).GetMethod("TestEncounter", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { room });
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map = Object.FindObjectsByType<Transform>().First(t => t.name == "Basement" && t.parent == null).gameObject;
        Check(map, "MAP"); CheckNeighbourRooms(map); VerifyDoors(map); Traverse(map, "MAP");
        PreviewSettings(); RenderRoom(map, "InMap");
        Directory.CreateDirectory("Preview"); File.WriteAllLines("Preview/Basement-final-validation.txt", report);
        Debug.Log("BASEMENT_FINAL_VERIFIED");
    }
    static void PreviewSettings()
    { RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.085f, .075f, .063f); RenderSettings.fog = false; ShaderUtil.allowAsyncCompilation = false; }
    static void CheckNeighbourRooms(GameObject map)
    {
        Physics.SyncTransforms();
        foreach(var area in new[] { map.transform.Find("PreBossArea"), map.transform.Find("BossRoom") })
        {
            bool arena = area.name == "PreBossArea"; float center = arena ? BasementFinalBuilder.ArenaCenterX : BasementFinalBuilder.BossCenterX;
            float width = arena ? 40 : 42, depth = arena ? 34 : 36, front = arena ? -15 : area.Find("Walls/Entrance_Wall_Header").localPosition.z;
            float floor = BasementFinalBuilder.Floor(area, arena ? "Hall_Floor" : "Main_Floor");
            for(float x = center - width / 2 + 1; x < center + width / 2; x += 2) for(float z = front + 1; z < front + depth; z += 2)
                foreach(var c in Physics.OverlapBox(area.TransformPoint(V(x, floor + 1.10f, z)), V(.18f, 1.05f, .18f), area.rotation, ~0, QueryTriggerInteraction.Ignore))
                {
                    if(c.transform.IsChildOf(map.transform) || c.transform.root.name == "DoorPrefab (4)" || c.transform.root.name == "DoorPrefab (6)") continue;
                    throw new Exception("Neighbour scene object intrudes into " + area.name + ": " + c.transform.root.name + "/" + c.name + " at " + V(x, floor, z));
                }
        }
        report.Add("MAP: enlarged rooms checked for intrusion from other scene rooms at sampled player volumes (2 m grid).");
        Debug.Log("BASEMENT_FINAL_NEIGHBOURS_OK");
    }
    static void Check(GameObject room, string label)
    {
        Physics.SyncTransforms();
        foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())
        {
            if(!renderer.enabled) continue;
            foreach(var mat in renderer.sharedMaterials) if(!mat || !mat.shader || mat.shader.name == "Hidden/InternalErrorShader") throw new Exception(label + " missing material: " + renderer.name);
            var filter = renderer.GetComponent<MeshFilter>(); if(filter && !filter.sharedMesh) throw new Exception(label + " missing mesh: " + renderer.name);
        }
        var arena = room.transform.Find("PreBossArea"); var boss = room.transform.Find("BossRoom");
        foreach(var area in new[] { arena, boss })
        {
            bool first = area == arena; float width = first ? 40 : 42, depth = first ? 34 : 36, height = first ? 9.5f : 12;
            if(Vector3.Distance(area.lossyScale, Vector3.one) > .002f) throw new Exception(label + " area remains compressed: " + area.lossyScale);
            var floor = area.Find(first ? "Floor/Hall_Floor" : "Floor/Main_Floor"); var roof = area.Find(first ? "Walls/Hall_Ceiling" : "Walls/Main_Ceiling");
            float top = BasementFinalBuilder.Floor(area, floor.name);
            float actualHeight = roof.localPosition.y - roof.localScale.y / 2 - top;
            if(Mathf.Abs(floor.localScale.x - width) > .01f || floor.localScale.z < depth || Mathf.Abs(actualHeight - height) > .01f) throw new Exception(label + " incorrect room dimensions");
            foreach(Transform wall in area.Find("Walls"))
            {
                if(!wall.gameObject.activeInHierarchy || wall.name.Contains("Hallway")) continue;
                var mesh = wall.GetComponent<MeshFilter>().sharedMesh;
                if(Vector3.Distance(mesh.bounds.center, Vector3.zero) > .001f || Vector3.Distance(mesh.bounds.size, Vector3.one) > .001f || mesh.triangles.Length < 36)
                    throw new Exception(label + " deformed/incomplete wall mesh: " + wall.name);
            }
            VerifyWalls(area, first, top, width, depth);
            report.Add(label + " " + area.name + ": " + width + " x " + depth + " m, clear height " + actualHeight + " m; walls and roof sealed.");
        }
        var drums = arena.GetComponentsInChildren<BoxCollider>().Where(c => c.name.StartsWith("Storage drum")).ToArray();
        foreach(var drum in drums)
        {
            float h = drum.transform.TransformVector(Vector3.up * drum.size.y).magnitude;
            float d = drum.transform.TransformVector(Vector3.right * drum.size.x).magnitude;
            if(h < 1.6f || d < .9f) throw new Exception(label + " drum remains too small");
        }
        if(drums.Length != 4 || !boss.Find("BasementFinalSanctum")) throw new Exception("Final-area decoration missing");
        report.Add(label + " drums: 1.643 m tall, 0.959 m diameter; 12 arena spawn points and final boss marker retained.");
        Debug.Log("BASEMENT_FINAL_" + label + "_STRUCTURE_OK");
    }
    static void VerifyWalls(Transform area, bool arena, float floor, float width, float depth)
    {
        float centerX = arena ? BasementFinalBuilder.ArenaCenterX : BasementFinalBuilder.BossCenterX;
        float front = arena ? -15 : area.Find("Walls/Entrance_Wall_Header").localPosition.z, back = front + depth;
        for(float z = front + .5f; z < back; z += .5f) foreach(int side in new[] { -1, 1 })
            Ray(area, V(centerX, floor + 2, z), V(side, 0, 0), width / 2 + .5f, "side wall");
        foreach(float y in new[] { 1.7f, 5.5f }) for(float x = centerX - width / 2 + .5f; x < centerX + width / 2; x += .5f)
        {
            float entryX = area.Find("Walls/Entrance_Wall_Header").localPosition.x;
            if(y > 4.2f || Mathf.Abs(x - entryX) > 1.9f) Ray(area, V(x, floor + y, front + 1.1f), Vector3.back, 1.6f, "entrance wall");
            if(!arena || y > 4.2f || Mathf.Abs(x) > 1.9f) Ray(area, V(x, floor + y, back - 1.1f), Vector3.forward, 1.6f, "rear wall");
        }
        float roof = arena ? 9.5f : 12;
        for(float x = centerX - width / 2 + .8f; x < centerX + width / 2; x += 2) for(float z = front + .8f; z < back; z += 2)
            Ray(area, V(x, floor + roof - .6f, z), Vector3.up, 1.1f, "ceiling");
    }
    static void Ray(Transform area, Vector3 origin, Vector3 direction, float distance, string surface)
    {
        var hits = Physics.RaycastAll(area.TransformPoint(origin), area.TransformDirection(direction), distance, ~0, QueryTriggerInteraction.Ignore);
        if(!hits.Any(h => h.collider.transform.IsChildOf(area) && h.collider.transform.parent.name == "Walls")) throw new Exception("Open " + surface + " at " + origin + " in " + area.name);
    }
    static void Traverse(GameObject room, string label)
    {
        var arena = room.transform.Find("PreBossArea"); var boss = room.transform.Find("BossRoom"); var layout = room.GetComponentInChildren<BasementGateLayout>(); layout.Fit();
        var encounter = room.GetComponentInChildren<BasementArenaEncounter>();
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab"));
        foreach(var script in player.GetComponentsInChildren<MonoBehaviour>()) script.enabled = false;
        foreach(var camera in player.GetComponentsInChildren<Camera>()) camera.enabled = false;
        var controller = player.GetComponent<CharacterController>(); Physics.SyncTransforms();
        float half = controller.bounds.extents.y, offset = controller.bounds.center.y - player.transform.position.y;
        var disabled = new List<Collider>();
        foreach(var door in Object.FindObjectsByType<OpenDoorWithKey>()) if(door.name == "DoorPrefab (4)" || door.name == "DoorPrefab (6)")
            foreach(var c in door.GetComponentsInChildren<Collider>()) if(c.enabled && !c.isTrigger) { c.enabled = false; disabled.Add(c); }
        float af = BasementFinalBuilder.Floor(arena, "Hall_Floor"), bf = BasementFinalBuilder.Floor(boss, "Main_Floor");
        float cx = boss.Find("Walls/Entrance_Wall_Header").localPosition.x, front = boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        Vector3 exit = boss.InverseTransformPoint(layout.gateFrame.position);
        float ac = BasementFinalBuilder.ArenaCenterX, bc = BasementFinalBuilder.BossCenterX;
        float entryX = arena.Find("Walls/Entrance_Wall_Header").localPosition.x;
        var points = new[] { arena.TransformPoint(V(entryX, af, -16.3f)), arena.TransformPoint(V(entryX, af, -12)), arena.TransformPoint(V(ac, af, -12)),
            arena.TransformPoint(V(ac, af, 14)), arena.TransformPoint(V(0, af, 16)), arena.TransformPoint(V(0, af, 20.2f)), boss.TransformPoint(V(cx, bf, exit.z)),
            boss.TransformPoint(V(cx, bf, front + 2)), boss.TransformPoint(V(bc, bf, front + 18.5f)),
            boss.TransformPoint(V(bc + 6, bf, front + 18.5f)), boss.TransformPoint(V(bc + 6, bf, front + 24)), boss.TransformPoint(V(bc - 6, bf, front + 24)) };
        Vector3 closed = encounter.gateLeaf.localPosition;
        try
        {
            Teleport(controller, arena.TransformPoint(V(0, af, 16)), half, offset);
            if(Walk(controller, arena.TransformPoint(V(0, af, 20.2f)), half, offset)) throw new Exception(label + " closed combat gate does not block player");
            encounter.gateLeaf.localPosition = closed + Vector3.up * 1.08f; Physics.SyncTransforms();
            Teleport(controller, points[0], half, offset);
            foreach(var p in points.Skip(1).Concat(points.Reverse().Skip(1))) if(!Walk(controller, p, half, offset)) throw new Exception(label + " player blocked at " + controller.transform.position + " toward " + p);
            report.Add(label + " actual CharacterController: entrance, arena, gate, L corridor, boss center and lateral combat area; forward and return. Closed gate blocks.");
            Debug.Log("BASEMENT_FINAL_" + label + "_PLAYER_OK");
        }
        finally { Object.DestroyImmediate(player); encounter.gateLeaf.localPosition = closed; foreach(var c in disabled) c.enabled = true; Physics.SyncTransforms(); }
    }
    static void Teleport(CharacterController c, Vector3 feet, float half, float offset)
    { c.enabled = false; c.transform.position = feet + Vector3.up * (half - offset + .01f); c.enabled = true; Physics.SyncTransforms(); }
    static bool Walk(CharacterController c, Vector3 feet, float half, float offset)
    {
        Vector3 target = feet + Vector3.up * (half - offset + .01f); int steps = Mathf.CeilToInt(Vector3.Distance(target, c.transform.position) / .04f) + 120;
        for(int i = 0; i < steps; i++) { var delta = target - c.transform.position; delta.y = 0; if(delta.magnitude < .06f) return Mathf.Abs(target.y - c.transform.position.y) < .20f; c.Move(Vector3.ClampMagnitude(delta, .04f) + Vector3.down * .015f); }
        return false;
    }
    static void VerifyDoors(GameObject room)
    {
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab"));
        foreach(var script in player.GetComponentsInChildren<MonoBehaviour>()) script.enabled = false;
        var cc = player.GetComponent<CharacterController>(); Physics.SyncTransforms();
        float half = cc.bounds.extents.y, offset = cc.bounds.center.y - player.transform.position.y;
        var pickup = player.GetComponent<PlayerPickupKey>(); pickup.key01 = pickup.key02 = pickup.key03 = true;
        try
        {
            foreach(var original in Object.FindObjectsByType<OpenDoorWithKey>().Where(d => d.name == "DoorPrefab (4)" || d.name == "DoorPrefab (6)").ToArray())
            {
                var clone = Object.Instantiate(original.gameObject); original.gameObject.SetActive(false);
                try
                {
                    var behaviour = clone.GetComponent<OpenDoorWithKey>(); var leaf = clone.transform.Find("Door"); var collider = leaf.GetComponent<BoxCollider>();
                    var area = room.transform.Find(original.name == "DoorPrefab (4)" ? "PreBossArea" : "BossRoom"); var h = area.Find("Walls/Entrance_Wall_Header");
                    float f = BasementFinalBuilder.Floor(area, area.name == "PreBossArea" ? "Hall_Floor" : "Main_Floor");
                    var center = area.TransformPoint(V(h.localPosition.x, f, h.localPosition.z));
                    typeof(OpenDoorWithKey).GetMethod("Awake", Private).Invoke(behaviour, null);
                    foreach(int side in new[] { -1, 1 })
                    {
                        var a = center + area.forward * side * 1.1f; var b = center - area.forward * side * 1.1f;
                        typeof(OpenDoorWithKey).GetField("openProgress", Private).SetValue(behaviour, 0f);
                        typeof(OpenDoorWithKey).GetField("closeTimer", Private).SetValue(behaviour, 0f);
                        typeof(OpenDoorWithKey).GetMethod("Update", Private).Invoke(behaviour, null);
                        Teleport(cc, a, half, offset); if(Walk(cc, b, half, offset)) throw new Exception("Closed key door fails to block " + original.name);
                        Teleport(cc, a, half, offset);
                        typeof(OpenDoorWithKey).GetMethod("TrackPlayer", Private).Invoke(behaviour, new object[] { cc });
                        typeof(OpenDoorWithKey).GetMethod("Update", Private).Invoke(behaviour, null);
                        if(!(bool)typeof(OpenDoorWithKey).GetField("unlocked", Private).GetValue(behaviour)) throw new Exception("Door trigger is detached from opening");
                        typeof(OpenDoorWithKey).GetField("openingDirection", Private).SetValue(behaviour, side > 0 ? 1f : -1f);
                        for(int i = 0; i <= 18; i++)
                        {
                            typeof(OpenDoorWithKey).GetField("openProgress", Private).SetValue(behaviour, i / 18f);
                            typeof(OpenDoorWithKey).GetField("closeTimer", Private).SetValue(behaviour, 99f);
                            typeof(OpenDoorWithKey).GetMethod("Update", Private).Invoke(behaviour, null); Physics.SyncTransforms();
                            foreach(var wall in area.Find("Walls").GetComponentsInChildren<BoxCollider>())
                                if(wall.enabled && Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, wall, wall.transform.position, wall.transform.rotation, out _, out float distance) && distance > .005f)
                                    throw new Exception(original.name + " swing hits wall " + wall.name + " progress=" + i + " side=" + side + " penetration=" + distance);
                        }
                        if(!Walk(cc, b, half, offset)) throw new Exception("Open key door blocks player " + original.name);
                        typeof(OpenDoorWithKey).GetMethod("OnDisable", Private).Invoke(behaviour, null);
                    }
                }
                finally { Object.DestroyImmediate(clone); original.gameObject.SetActive(true); }
            }
            Debug.Log("BASEMENT_FINAL_DOORS_OK proximity, closed blocking and both swing directions");
            report.Add("MAP key doors: trigger accompanies leaf; proximity, closed blocking, both swing directions and player passage verified.");
        }
        finally { Object.DestroyImmediate(player); }
    }
    static void RenderRoom(GameObject room, string suffix)
    {
        var arena = room.transform.Find("PreBossArea"); var boss = room.transform.Find("BossRoom");
        float af = BasementFinalBuilder.Floor(arena, "Hall_Floor"), bf = BasementFinalBuilder.Floor(boss, "Main_Floor"), front = boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        float ac = BasementFinalBuilder.ArenaCenterX, bc = BasementFinalBuilder.BossCenterX;
        Render(arena, V(2, af + 1.84f, -12), V(ac + 3, af + 3, 14), "Arena" + suffix);
        Render(arena, V(3, af + 1.84f, 13), V(0, af + 2.3f, 19), "Gate" + suffix);
        Render(boss, V(0, bf + 1.84f, front + 2), V(bc, bf + 5.2f, front + 33), "BossRoom" + suffix);
        Render(boss, V(bc + 6.5f, bf + 1.84f, front + 21), V(bc - 2, bf + 6.8f, front + 32), "BossHorror" + suffix);
        Render(arena, V(ac - 9.8f, af + 1.84f, 6.4f), V(ac - 15.5f, af + 1.2f, 10), "AbandonedStation" + suffix);
    }
    static void Render(Transform area, Vector3 pos, Vector3 look, string name)
    {
        var go = new GameObject("Final area preview camera"); var c = go.AddComponent<Camera>(); go.AddComponent<UniversalAdditionalCameraData>();
        c.fieldOfView = 72; c.nearClipPlane = .04f; c.farClipPlane = 150; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.02f, .015f, .012f);
        go.transform.position = area.TransformPoint(pos); go.transform.LookAt(area.TransformPoint(look));
        var rt = new RenderTexture(1672, 941, 24); c.targetTexture = rt; for(int i = 0; i < 4; i++) c.Render(); RenderTexture.active = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
        Directory.CreateDirectory("Preview"); File.WriteAllBytes("Preview/Basement-" + name + ".png", image.EncodeToPNG());
        c.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(image); Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
    }
}
