#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Rebuilds the restaurant from the supplied OBJ models. Run only from the menu or batch mode.
public static class BuildUniversityRestaurant
{
    const string SceneFolder = "Assets/_Project/Scenes/CampusExterior";
    const string ModelFolder = SceneFolder + "/Models/UniversityRestaurant";
    const string MaterialFolder = SceneFolder + "/Materials/FoodCourt";
    const string PrefabPath = SceneFolder + "/Prefabs/UniversityRestaurant.prefab";
    const string ScenePath = SceneFolder + "/CampusExterior.unity";
    const string RootName = "University restaurant - left field";

    static Material Material(string name)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");
        if (material == null) throw new InvalidOperationException("Missing restaurant material: " + name);
        return material;
    }

    static Transform Group(Transform parent, string name)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    static GameObject Add(Transform parent, string model, string name, Vector3 position,
        Material material, float yaw = 0, bool collision = true)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + model + ".obj");
        if (source == null) throw new InvalidOperationException("Missing restaurant model: " + model);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterial = material;
        if (collision)
        {
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }
        return instance;
    }

    static void Light(Transform parent, Vector3 position)
    {
        var go = new GameObject("Restaurant ceiling light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 7;
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;
        light.color = new Color(1f, 0.91f, 0.76f);
    }

    [MenuItem("Tools/UniDoom/Campus/Rebuild University Restaurant")]
    public static void Build()
    {
        var root = new GameObject(RootName);
        try
        {
            var structure = Group(root.transform, "Structure");
            Add(structure, "Piso", "Ceramic floor", Vector3.zero, Material("Food court tile"));
            Add(structure, "Cobertura", "Metal roof", Vector3.zero, Material("Food court steel"));
            for (int i = 0; i < 2; i++)
                foreach (float x in new[] { -7.55f, -3.8f, 0f, 3.8f, 7.55f })
                    Add(structure, "Pilar_Azul", "Blue support", new Vector3(x, 0, i == 0 ? -4.6f : 4.6f), Material("Food court blue"));
            foreach (float z in new[] { -4.55f, 0f, 4.55f })
                Add(structure, "Trelica", "Roof truss", new Vector3(0, 0, z), Material("Food court steel"));

            var seating = Group(root.transform, "Dining area");
            int number = 0;
            foreach (float x in new[] { -4.8f, 0f, 4.8f })
                foreach (float z in new[] { -2.4f, 0.55f })
                {
                    var set = Group(seating, "Table " + (++number).ToString("00"));
                    Add(set, "Mesa", "Table", new Vector3(x, 0, z), Material("Food court wood"));
                    Add(set, "Cadeira", "Chair left", new Vector3(x - 0.95f, 0, z), Material("Food court chair"), 90);
                    Add(set, "Cadeira", "Chair right", new Vector3(x + 0.95f, 0, z), Material("Food court chair"), -90);
                    Add(set, "Cadeira", "Chair front", new Vector3(x, 0, z - 0.82f), Material("Food court chair"));
                    Add(set, "Cadeira", "Chair back", new Vector3(x, 0, z + 0.82f), Material("Food court chair"), 180);
                }

            var service = Group(root.transform, "Service counter");
            Add(service, "Balcao", "Serving counter", new Vector3(0, 0, 3.92f), Material("Food court wood"));
            Add(service, "Vitrine", "Display left", new Vector3(-1.9f, 1.1f, 3.92f), Material("Food court glass"));
            Add(service, "Vitrine", "Display right", new Vector3(1.3f, 1.1f, 3.92f), Material("Food court glass"));
            Add(service, "Maquina_Cafe", "Coffee machine", new Vector3(-3.25f, 1.1f, 3.92f), Material("Food court steel"));
            Add(service, "Geladeira", "Drinks fridge", new Vector3(5.5f, 0, 4.18f), Material("Food court steel"));
            Add(service, "Geladeira", "Water fridge", new Vector3(-5.45f, 0, 4.18f), Material("Food court steel"));
            Add(service, "Fachada_Azul", "UNIMAR facade", new Vector3(0, 3.35f, 4.65f), Material("unimar_sign"), 0, false);
            string[] menus = { "menu_salgados", "menu_lanches", "menu_bebidas" };
            for (int i = 0; i < menus.Length; i++)
                Add(service, "Painel_Menu", "Menu " + (i + 1), new Vector3(-2 + i * 2, 2.75f, 4.45f), Material(menus[i]), 0, false);

            var decor = Group(root.transform, "Plants and amenities");
            foreach (float x in new[] { -7.1f, 7.1f })
                foreach (float z in new[] { -3.7f, 2.7f })
                    Add(decor, "Floreira", "Planter", new Vector3(x, 0, z), Material("Food court plant"));
            foreach (float x in new[] { -6.5f, 6.5f })
                Add(decor, "Banco", "Bench", new Vector3(x, 0, -4.2f), Material("Food court wood"));
            foreach (float x in new[] { -6.8f, 6.8f })
                Add(decor, "Lixeira", "Waste bin", new Vector3(x, 0, -2.1f), Material("Food court steel"));
            foreach (float x in new[] { -5.7f, 5.7f })
                Add(decor, "Vaso_Pendente", "Hanging plant", new Vector3(x, 4.25f, 1.4f), Material("Food court plant"), 0, false);

            var lighting = Group(root.transform, "Lighting");
            foreach (float x in new[] { -5f, 0f, 5f })
            {
                Add(lighting, "Luminaria", "Ceiling fixture", new Vector3(x, 4.49f, 0), Material("Food court steel"), 0, false);
                Light(lighting, new Vector3(x, 4.3f, 0));
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var sceneRoot in scene.GetRootGameObjects())
                if (sceneRoot.name == RootName) UnityEngine.Object.DestroyImmediate(sceneRoot);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            placed.transform.position = new Vector3(58, 0, -40);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("University restaurant built and placed in CampusExterior at (58, 0, -40).");
        }
        catch
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            throw;
        }
    }
}
#endif
