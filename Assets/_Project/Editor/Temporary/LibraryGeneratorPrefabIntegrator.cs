using System;
using System.IO;
using UniDoom.CampusLibrary;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class LibraryGeneratorPrefabIntegrator
{
    const string PrefabPath = "Assets/_Project/Scenes/CampusLibrary/Prefabs/CampusLibrary.prefab";
    const string RequestPath = "Temp/IntegrateLibraryGenerator.request";
    const string LogPath = "Temp/LibraryGeneratorPrefabIntegration.log";
    static readonly string[] Models =
    {
        "Assets/_Project/Scenes/CampusLibrary/Resources/Generator/Gerador_UniDoom_0_fusiveis.glb",
        "Assets/_Project/Scenes/CampusLibrary/Resources/Generator/Gerador_UniDoom_1_fusiveis.glb",
        "Assets/_Project/Scenes/CampusLibrary/Resources/Generator/Gerador_UniDoom_2_fusiveis.glb",
        "Assets/_Project/Scenes/CampusLibrary/Resources/Generator/Gerador_UniDoom_3_fusiveis.glb"
    };
    static int attempts;

    static LibraryGeneratorPrefabIntegrator()
    {
        EditorApplication.delayCall += RunRequested;
    }

    [MenuItem("Tools/UniDoom/CampusLibrary/Integrate Generator GLB Models")]
    public static void IntegrateFromMenu() => Integrate();

    static void RunRequested()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequested;
            return;
        }

        try
        {
            Integrate();
            File.Delete(RequestPath);
        }
        catch (Exception exception)
        {
            if (++attempts < 8)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                EditorApplication.delayCall += RunRequested;
                return;
            }
            File.WriteAllText(LogPath, "FAILED\n" + exception);
            File.Delete(RequestPath);
            Debug.LogException(exception);
        }
    }

    public static void Integrate()
    {
        var sources = new GameObject[Models.Length];
        for (int i = 0; i < Models.Length; i++)
        {
            AssetDatabase.ImportAsset(Models[i], ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            sources[i] = AssetDatabase.LoadAssetAtPath<GameObject>(Models[i]);
            if (!sources[i]) throw new InvalidOperationException("GLB generator model was not imported: " + Models[i]);
        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            LibraryMissionController mission = prefab.GetComponentInChildren<LibraryMissionController>(true);
            if (!mission) throw new InvalidOperationException("LibraryMissionController was not found in CampusLibrary.prefab.");

            Transform previous = Find(mission.transform, "Generator GLB model variants");
            if (previous) Object.DestroyImmediate(previous.gameObject);

            var holder = new GameObject("Generator GLB model variants").transform;
            holder.SetParent(mission.transform, false);
            holder.position = new Vector3(-24.7f, 0f, 42.3f);

            var instances = new GameObject[4];
            for (int i = 0; i < sources.Length; i++)
            {
                instances[i] = (GameObject)PrefabUtility.InstantiatePrefab(sources[i], holder);
                instances[i].name = $"Generator model - {i} fuses";
                instances[i].transform.localPosition = Vector3.zero;
                instances[i].transform.localRotation = Quaternion.identity;
                instances[i].transform.localScale = Vector3.one;
                instances[i].SetActive(i == 0);
            }

            DisableLegacyVisuals(prefab.transform);

            LibraryGeneratorModelVariants variants = mission.GetComponent<LibraryGeneratorModelVariants>();
            if (!variants) variants = mission.gameObject.AddComponent<LibraryGeneratorModelVariants>();
            var serialized = new SerializedObject(variants);
            serialized.FindProperty("variantsRoot").objectReferenceValue = holder;
            SerializedProperty array = serialized.FindProperty("variants");
            array.arraySize = instances.Length;
            for (int i = 0; i < instances.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = instances[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        ValidateSavedPrefab();
        AssetDatabase.SaveAssets();
        File.WriteAllText(LogPath, "SUCCESS\nPersistent generator variants: 4\nActive prefab variant: 0 fuses\nPosition: (-24.7, 0, 42.3)\n");
        Debug.Log("CampusLibrary: four GLB generator variants integrated into the persistent prefab.");
    }

    static void DisableLegacyVisuals(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            string objectName = renderer.gameObject.name;
            if (objectName == "Library backup generator" || objectName == "Generator worn blue cover" ||
                objectName == "Generator steel support" || objectName == "Generator ventilation grille" ||
                objectName == "Generator ignition control pedestal" || objectName == "Generator instructions" ||
                objectName == "Physical plate - Generator instructions" || objectName.StartsWith("Installed fuse socket "))
                renderer.enabled = false;
        }
    }

    static void ValidateSavedPrefab()
    {
        GameObject check = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform holder = Find(check.transform, "Generator GLB model variants");
            if (!holder || holder.childCount != 4) throw new InvalidOperationException("Saved prefab does not contain four generator variants.");
            for (int i = 0; i < 4; i++)
            {
                Transform child = holder.Find($"Generator model - {i} fuses");
                if (!child || !child.GetComponent<MeshFilter>() || !child.GetComponent<MeshRenderer>())
                    throw new InvalidOperationException("Saved generator variant is incomplete: " + i);
                if (child.gameObject.activeSelf != (i == 0)) throw new InvalidOperationException("Unexpected active generator variant: " + i);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(check);
        }
    }

    static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }
}
