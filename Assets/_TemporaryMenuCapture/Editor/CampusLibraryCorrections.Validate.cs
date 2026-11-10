using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UniDoom.CampusLibrary;
using TMPro;

public static partial class CampusLibraryCorrections
{
    public static void ValidateAndPlay() { Validate(); CampusLibraryCorrectionPlayChecks.Begin(); }
    public static void RuntimeVisuals()
    {
        Capture(new Vector3(-22.3f, 1.85f, 40.4f), new Vector3(-22.3f, 1.85f, 43.9f), "runtime-panel-horizontal");
        Capture(new Vector3(-25.9f, 1.85f, 35.7f), new Vector3(-25.9f, 1.85f, 34.2f), "runtime-note-horizontal");
    }
    public static void Validate()
    {
        var passes = new List<string>();
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); passes.Add(message); }
        Prefab(root =>
        {
            Check(root.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) == 0, "No Missing Script in CampusLibrary prefab");
            var mission = root.GetComponentInChildren<LibraryMissionController>(true);
            var targets = root.GetComponentsInChildren<LibraryMissionTarget>(true);
            Check(targets.All(t => t.Mission == mission), "All mission targets reference the same controller");
            var fuses = targets.Where(t => t.Kind == LibraryTargetKind.Fuse).ToArray();
            Check(fuses.Length == 3 && fuses.Select(t => t.Id).Distinct().Count() == 3 && fuses.All(t => t.GetComponentInChildren<SpriteRenderer>(true).sprite && t.GetComponentsInChildren<MeshRenderer>(true).All(r => !r.enabled)), "Three unique fuses with sprite references and disabled old meshes");
            Check(targets.Count(t => t.Kind == LibraryTargetKind.Clue && t.Document) == 3, "Three physical paper clues with document textures");
            Check(targets.Where(t => t.Kind == LibraryTargetKind.Button || t.Kind == LibraryTargetKind.FuseSocket || t.Kind == LibraryTargetKind.Generator).All(t => Mathf.Abs(t.transform.position.y - 1.85f) < .01f), "All panel and generator controls at horizontal eye height");
            var labels = root.GetComponentsInChildren<TMP_Text>(true);
            Check(labels.Count(t => t.enabled) == 1 && labels.Single(t => t.enabled).name == "Generator mission status", "Only generator screen uses active world TMP");
            Check(!labels.Any(t => t.text.Contains("3 → 1")), "No numeric answer stored on world labels");
            var shelves = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("full bookcase")).ToArray();
            Check(shelves.Length == 74, "All 74 shelves retained");
            int overlaps = 0;
            for (int i = 0; i < shelves.Length; i++) for (int j = i + 1; j < shelves.Length; j++)
            { var a = BoundsOf(shelves[i]); var b = BoundsOf(shelves[j]); var extent = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min); if (extent.x > .03f && extent.z > .03f && extent.y > .5f) overlaps++; }
            Check(overlaps == 0, "No shelf volume intersections above 3cm");
            var safety = root.GetComponentInChildren<LibraryMaintenanceSafety>(true);
            Check(safety.Contains(new Vector3(-21.5f, 1, 36.5f)) && safety.Contains(new Vector3(-24, 1, 42)) && safety.Contains(new Vector3(-15.8f, 1, 41.5f)) && !safety.Contains(safety.RecoveryPosition), "Hall, generator room and workshop protected; recovery outside");
            var surface = root.GetComponentInChildren<NavMeshSurface>(true); var navigation = NavMesh.AddNavMeshData(surface.navMeshData, surface.transform.position, surface.transform.rotation);
            try
            {
                Check(NavMesh.SamplePosition(new Vector3(0, .64f, -8.5f), out var from, 2, NavMesh.AllAreas) && NavMesh.SamplePosition(new Vector3(18.5f, 0, 33), out _, 1.5f, NavMesh.AllAreas), "Navigation present at entrance and annex");
                NavMesh.SamplePosition(new Vector3(18.5f, 0, 33), out var to, 1.5f, NavMesh.AllAreas); var path = new NavMeshPath();
                Check(NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Public entrance remains connected to annex on navigation");
                Check(!NavMesh.SamplePosition(new Vector3(-21.5f, 0, 36.5f), out _, .3f, NavMesh.AllAreas), "Expanded staff hall excluded from enemy navigation");
            }
            finally { navigation.Remove(); }
            foreach (var material in root.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct()) Check(material.shader && !ShaderUtil.ShaderHasError(material.shader), "Valid material shader: " + material.name);
        }, false);
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single); Physics.SyncTransforms();
        foreach (var door in new[] { new Vector3(-21.5f, 1.07f, 33), new Vector3(-22.5f, 1.07f, 37.8f), new Vector3(-15.8f, 1.07f, 37.8f) })
        {
            var hits = Physics.CapsuleCastAll(door + Vector3.up * .82f, door - Vector3.up * .82f, .24f, Vector3.forward, 2, ~0, QueryTriggerInteraction.Ignore);
            Check(!hits.Any(h => h.collider.name != "Enemy-only maintenance barrier"), "Player capsule clears staff doorway at " + door);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllLines(Log + "validation.txt", passes); Debug.Log("CampusLibrary correction validation passed: " + passes.Count);
    }
}
