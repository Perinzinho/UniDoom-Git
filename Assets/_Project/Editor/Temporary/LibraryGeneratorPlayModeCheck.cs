using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UniDoom.CampusLibrary;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LibraryGeneratorPlayModeCheck
{
    const string RequestPath = "Temp/LibraryGeneratorPlayModeCheck.request";
    const string LogPath = "Temp/LibraryGeneratorPlayModeCheck.log";
    static int frames;

    static LibraryGeneratorPlayModeCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (File.Exists(RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !File.Exists(RequestPath)) return;
        frames = 0;
        EditorApplication.update += WaitForScene;
    }

    static void WaitForScene()
    {
        if (++frames < 12) return;
        EditorApplication.update -= WaitForScene;
        bool success = false;
        try
        {
            RunChecks();
            File.WriteAllText(LogPath, "SUCCESS\nPersistent prefab variants: 4\nRuntime states verified: 0 -> 1 -> 2 -> 3 -> 0\nLegacy generator renderers disabled\nAll imported meshes valid\n");
            success = true;
        }
        catch (Exception exception)
        {
            File.WriteAllText(LogPath, "FAILED\n" + exception);
            Debug.LogException(exception);
        }
        finally
        {
            File.Delete(RequestPath);
            LibraryMissionProgress.ResetSession();
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                int exitCode = success ? 0 : 1;
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        }
    }

    static void RunChecks()
    {
        LibraryGeneratorModelVariants controller = UnityEngine.Object.FindObjectsByType<LibraryGeneratorModelVariants>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (!controller) throw new InvalidOperationException("LibraryGeneratorModelVariants was not found in Play Mode.");

        FieldInfo field = typeof(LibraryGeneratorModelVariants).GetField("variants", BindingFlags.Instance | BindingFlags.NonPublic);
        var variants = field?.GetValue(controller) as GameObject[];
        if (variants == null || variants.Length != 4 || variants.Any(item => !item))
            throw new InvalidOperationException("Four persistent model variants were not resolved.");
        VerifyVariant(variants, 0);

        Type progress = typeof(LibraryMissionProgress);
        Invoke(progress, "Discover");
        for (int i = 0; i < 3; i++) Invoke(progress, "Collect", i);
        for (int inserted = 1; inserted <= 3; inserted++)
        {
            object result = Invoke(progress, "Install");
            if (result is not true) throw new InvalidOperationException("Fuse insertion failed at step " + inserted);
            VerifyVariant(variants, inserted);
        }
        if (LibraryMissionProgress.Stage != LibraryMissionStage.FusesInstalled)
            throw new InvalidOperationException("Third fuse did not unlock the generator puzzle.");

        foreach (GameObject variant in variants)
        {
            MeshFilter filter = variant.GetComponent<MeshFilter>();
            MeshRenderer renderer = variant.GetComponent<MeshRenderer>();
            if (!filter || !filter.sharedMesh || filter.sharedMesh.vertexCount < 1000 || !renderer || !renderer.sharedMaterial)
                throw new InvalidOperationException("Imported generator mesh/material is incomplete: " + variant.name);
        }

        string[] legacyNames =
        {
            "Library backup generator", "Generator worn blue cover", "Generator steel support",
            "Generator ventilation grille", "Generator ignition control pedestal"
        };
        foreach (Renderer renderer in controller.transform.root.GetComponentsInChildren<Renderer>(true))
            if (legacyNames.Contains(renderer.gameObject.name) && renderer.enabled)
                throw new InvalidOperationException("Legacy renderer is still enabled: " + renderer.gameObject.name);

        LibraryMissionProgress.ResetSession();
        VerifyVariant(variants, 0);
    }

    static void VerifyVariant(GameObject[] variants, int expected)
    {
        for (int i = 0; i < variants.Length; i++)
            if (variants[i].activeSelf != (i == expected))
                throw new InvalidOperationException($"Variant state mismatch: expected {expected}, checked {i}.");
    }

    static object Invoke(Type type, string method, params object[] arguments)
    {
        MethodInfo info = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        if (info == null) throw new MissingMethodException(type.FullName, method);
        return info.Invoke(null, arguments);
    }
}
