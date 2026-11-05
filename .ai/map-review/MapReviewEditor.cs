// One-shot audit, temporarily compiled under Assets/__MapReviewTemporary/Editor.
// Opens isolated preview scenes and never saves project scenes or assets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class MapReviewEditor
{
    const string DirectoryPath = ".ai/map-review";
    const string Key = "UniDoom.MapReview.20261020.final.v4";
    [Serializable] public class Move { public string source, destination; }
    [Serializable] public class MovePlan { public List<Move> moves = new List<Move>(); }
    [Serializable] public class Finding { public string kind, path, detail; }
    [Serializable] public class SceneResult
    {
        public string path, status;
        public int objects, renderers, colliders, lights, realtimeShadowLights, missingScripts;
        public List<string> roots = new List<string>();
        public List<string> excludedSubtrees = new List<string>();
        public List<string> materials = new List<string>();
        public List<string> dependencies = new List<string>();
        public List<Finding> findings = new List<Finding>();
    }
    [Serializable] public class Result
    {
        public string unityVersion, pipeline, status;
        public bool playMode, compilationFailed;
        public List<SceneResult> scenes = new List<SceneResult>();
        public List<string> messages = new List<string>();
    }
    static MapReviewEditor() { EditorApplication.delayCall += Run; }
    static string Hierarchy(Transform t) { return t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name; }
    static bool IsDirectorSubtree(Transform t)
    {
        for (var current = t; current; current = current.parent)
            if (current.name.StartsWith("DirectorRoom", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    static void Run()
    {
        if (SessionState.GetBool(Key, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Run; return; }
        var result = new Result { unityVersion = Application.unityVersion,
            pipeline = GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.name : "Built-in",
            playMode = EditorApplication.isPlayingOrWillChangePlaymode,
            compilationFailed = EditorUtility.scriptCompilationFailed };
        if (result.playMode)
        {
            result.status = "BLOCKED_PLAY_MODE";
            File.WriteAllText(DirectoryPath + "/unity-validation.json", JsonUtility.ToJson(result, true));
            SessionState.SetBool(Key, true);
            return;
        }
        SessionState.SetBool(Key, true);
        if (File.Exists(DirectoryPath + "/editor-move-plan.json") && !File.Exists(DirectoryPath + "/editor-moves-completed.json"))
        {
            var plan = JsonUtility.FromJson<MovePlan>(File.ReadAllText(DirectoryPath + "/editor-move-plan.json"));
            var completed = new List<Move>();
            try
            {
                foreach (var move in plan.moves)
                {
                    if (!move.source.StartsWith("Assets/_Project/Scenes/ClassRoom/Lighting/Textures/") ||
                        !move.destination.StartsWith("Assets/_Project/Global/Textures/MapSurfaces/"))
                        throw new InvalidOperationException("Move outside the explicit texture scope.");
                    string error = AssetDatabase.MoveAsset(move.source, move.destination);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    completed.Add(move);
                }
                File.WriteAllText(DirectoryPath + "/editor-moves-completed.json", JsonUtility.ToJson(plan, true));
            }
            catch (Exception)
            {
                completed.Reverse();
                foreach (var move in completed) AssetDatabase.MoveAsset(move.destination, move.source);
                throw;
            }
            return;
        }
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var active = SceneManager.GetActiveScene();
        Application.LogCallback callback = (message, stack, type) => {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Warning) && result.messages.Count < 100)
                result.messages.Add(type + ": " + message);
        };
        Application.logMessageReceived += callback;
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                if (path.Contains("/DirectorRoom/")) continue;
                var entry = new SceneResult { path = path, status = "pending" };
                result.scenes.Add(entry);
                Scene preview = default;
                try
                {
                    preview = EditorSceneManager.OpenPreviewScene(path);
                    var roots = preview.GetRootGameObjects();
                    entry.roots.AddRange(roots.Select(r => r.name));
                    var transforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                    entry.excludedSubtrees.AddRange(transforms.Where(t => t.name.StartsWith("DirectorRoom", StringComparison.OrdinalIgnoreCase)).Select(Hierarchy));
                    var objects = transforms.Where(t => !IsDirectorSubtree(t)).Select(t => t.gameObject).ToArray();
                    entry.objects = objects.Length;
                    var mats = new HashSet<Material>();
                    foreach (var go in objects)
                    {
                        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                        entry.missingScripts += missing;
                        if (missing > 0) entry.findings.Add(new Finding { kind = "missing_script", path = Hierarchy(go.transform), detail = missing.ToString() });
                        foreach (var renderer in go.GetComponents<Renderer>())
                        {
                            entry.renderers++;
                            foreach (var mat in renderer.sharedMaterials)
                                if (mat) mats.Add(mat);
                                else entry.findings.Add(new Finding { kind = "null_material", path = Hierarchy(go.transform), detail = renderer.GetType().Name });
                        }
                        entry.colliders += go.GetComponents<Collider>().Length;
                        foreach (var light in go.GetComponents<Light>())
                        {
                            entry.lights++;
                            if (light.enabled && go.activeInHierarchy && light.shadows != LightShadows.None && light.lightmapBakeType != LightmapBakeType.Baked) entry.realtimeShadowLights++;
                        }
                        foreach (var component in go.GetComponents<Component>())
                        {
                            if (!component) continue;
                            if (component is MeshFilter filter && !filter.sharedMesh)
                                entry.findings.Add(new Finding { kind = "null_mesh", path = Hierarchy(go.transform), detail = "MeshFilter; active=" + go.activeInHierarchy + "; renderers=" + go.GetComponents<Renderer>().Length + "; child renderers=" + go.GetComponentsInChildren<Renderer>(true).Length });
                            using (var so = new SerializedObject(component))
                            {
                                var property = so.GetIterator();
                                while (property.Next(true))
                                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && !property.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                                        entry.findings.Add(new Finding { kind = "broken_object_reference", path = Hierarchy(go.transform), detail = component.GetType().Name + "." + property.propertyPath });
                            }
                        }
                    }
                    foreach (var mat in mats)
                    {
                        string matPath = AssetDatabase.GetAssetPath(mat);
                        entry.materials.Add(matPath);
                        if (!mat.shader || !mat.shader.isSupported)
                        {
                            entry.findings.Add(new Finding { kind = "unsupported_shader", path = matPath, detail = mat.shader ? mat.shader.name : "null" });
                            foreach (var renderer in objects.SelectMany(go => go.GetComponents<Renderer>()).Where(r => r.sharedMaterials.Contains(mat)))
                            {
                                var filter = renderer.GetComponent<MeshFilter>();
                                entry.findings.Add(new Finding { kind = "unsupported_shader_usage", path = Hierarchy(renderer.transform), detail = "enabled=" + renderer.enabled + "; active=" + renderer.gameObject.activeInHierarchy + "; mesh=" + (filter && filter.sharedMesh ? AssetDatabase.GetAssetPath(filter.sharedMesh) : "null") });
                            }
                        }
                        if (mat.shader)
                            foreach (var message in ShaderUtil.GetShaderMessages(mat.shader))
                                if (message.severity.ToString() == "Error")
                                    entry.findings.Add(new Finding { kind = "shader_error", path = matPath, detail = message.message });
                    }
                    entry.dependencies.AddRange(AssetDatabase.GetDependencies(path, true));
                    entry.status = "loaded_and_inspected";
                }
                catch (Exception ex) { entry.status = "failed"; entry.findings.Add(new Finding { kind = "exception", path = path, detail = ex.ToString() }); }
                finally { if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview); }
            }
            result.status = result.scenes.All(s => s.status == "loaded_and_inspected") ? "completed" : "partial";
        }
        finally
        {
            Application.logMessageReceived -= callback;
            if (active.IsValid() && active.isLoaded && SceneManager.GetActiveScene() != active) SceneManager.SetActiveScene(active);
            var after = EditorSceneManager.GetSceneManagerSetup();
            if (setup.Length != after.Length || setup.Where((s, i) => i < after.Length && (s.path != after[i].path || s.isLoaded != after[i].isLoaded || s.isActive != after[i].isActive)).Any())
                result.messages.Add("Loaded scene setup changed during inspection; no user scene was saved.");
            File.WriteAllText(DirectoryPath + "/unity-validation.json.tmp", JsonUtility.ToJson(result, true));
            File.Copy(DirectoryPath + "/unity-validation.json.tmp", DirectoryPath + "/unity-validation.json", true);
            File.Delete(DirectoryPath + "/unity-validation.json.tmp");
        }
    }
}
