using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Read-only inspection: never saves scenes, prefabs or importer settings.
public static class ProjectOrganizationAudit
{
    const string Output = "Tools/ProjectOrganization/";
    [Serializable] public class Entry
    {
        public string guid, path, type;
        public string[] dependencies;
        public int objects, components, missingScripts, missingReferences, nullMaterials, nullMeshes;
        public string[] bindings;
        public string error;
    }
    [Serializable] public class Snapshot { public Entry[] entries; public string unity; }
    [Serializable] public class Move { public string source, destination; }
    [Serializable] public class Plan { public Move[] moves; }

    public static void Before() => Capture("unity-before.json");
    public static void After() => Capture("unity-after.json");
    public static void ValidateAll() { After(); ValidateBuild(); }

    static void Inspect(IEnumerable<GameObject> roots, Entry e)
    {
        var objects = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        e.objects = objects.Length;
        var bindings = new List<string>();
        foreach (var transform in objects)
        {
            var go = transform.gameObject;
            e.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            foreach (var component in go.GetComponents<Component>())
            {
                if (!component) continue;
                e.components++;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    if (!reference && !property.objectReferenceEntityIdValue.Equals(default(EntityId))) e.missingReferences++;
                    if (reference && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out string guid, out long id))
                        bindings.Add(component.GetType().FullName + ":" + property.propertyPath + ":" + guid + ":" + id);
                }
                if (component is Renderer renderer) e.nullMaterials += renderer.sharedMaterials.Count(m => !m);
                if (component is MeshFilter filter && !filter.sharedMesh) e.nullMeshes++;
            }
        }
        bindings.Sort(StringComparer.Ordinal);
        e.bindings = bindings.ToArray();
    }

    static void Capture(string filename)
    {
        if (EditorApplication.isCompiling) throw new Exception("Scripts are still compiling");
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
        var entries = new List<Entry>();
        foreach (var path in paths)
        {
            var e = new Entry { path = path, guid = AssetDatabase.AssetPathToGUID(path), type = AssetDatabase.GetMainAssetTypeAtPath(path)?.FullName };
            e.dependencies = AssetDatabase.GetDependencies(path, true).Select(AssetDatabase.AssetPathToGUID).Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g).ToArray();
            try
            {
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    Inspect(scene.GetRootGameObjects(), e);
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try { Inspect(new[] { root }, e); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
            }
            catch (Exception ex) { e.error = ex.ToString(); Debug.LogException(ex); }
            entries.Add(e);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + filename, JsonUtility.ToJson(new Snapshot { unity = Application.unityVersion, entries = entries.ToArray() }, true));
        if (entries.Any(e => !string.IsNullOrEmpty(e.error))) throw new Exception("Some assets could not be inspected; see " + filename);
        Debug.Log("ORGANIZATION_CAPTURE_OK " + filename + " " + entries.Count);
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new Exception("Cannot create folder " + path);
    }

    public static void Apply()
    {
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Output + "move-plan.json"));
        // Validate the complete manifest before the first move.
        foreach (var move in plan.moves)
        {
            if (!move.source.StartsWith("Assets/", StringComparison.Ordinal) || !move.destination.StartsWith("Assets/_Project/", StringComparison.Ordinal)) throw new Exception("Invalid scope");
            if (!File.Exists(move.source) && !Directory.Exists(move.source)) throw new Exception("Missing source " + move.source);
            if (File.Exists(move.destination) || Directory.Exists(move.destination)) throw new Exception("Destination exists " + move.destination);
        }
        var journal = new List<Move>();
        bool editing = false;
        try
        {
            foreach (var move in plan.moves)
            {
                if (!Directory.Exists(move.source) && !editing)
                {
                    // Create destination folders before batching file moves: folders
                    // created during StartAssetEditing are not registered yet.
                    foreach (var remaining in plan.moves.Where(m => File.Exists(m.source)))
                        EnsureFolder(Path.GetDirectoryName(remaining.destination));
                    AssetDatabase.StartAssetEditing();
                    editing = true;
                }
                if (!editing) EnsureFolder(Path.GetDirectoryName(move.destination));
                string error = AssetDatabase.MoveAsset(move.source, move.destination);
                if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                journal.Add(move);
                File.WriteAllText(Output + "move-journal.json", JsonUtility.ToJson(new Plan { moves = journal.ToArray() }, true));
            }
            // This operation only changes paths. Do not save incidental ExecuteAlways changes.
            Debug.Log("ORGANIZATION_MOVES_OK " + journal.Count);
        }
        catch
        {
            if (editing) { AssetDatabase.StopAssetEditing(); editing = false; }
            foreach (var move in journal.AsEnumerable().Reverse())
            {
                EnsureFolder(Path.GetDirectoryName(move.source));
                string error = AssetDatabase.MoveAsset(move.destination, move.source);
                if (!string.IsNullOrEmpty(error)) Debug.LogError("ROLLBACK: " + error);
            }
            throw;
        }
        finally { if (editing) AssetDatabase.StopAssetEditing(); }
    }

    public static void ValidateBuild()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = ".utmp/ProjectOrganization/Build/UniDoom.exe",
            options = BuildOptions.Development
        });
        File.WriteAllText(Output + "build-result.json", "{\"result\":\"" + report.summary.result + "\",\"errors\":" + report.summary.totalErrors + ",\"warnings\":" + report.summary.totalWarnings + "}");
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Organization build failed");
        Debug.Log("ORGANIZATION_BUILD_OK");
    }
}
