using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampusParkingBenchFountainSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/CampusExterior/CampusExterior.unity";
    private const string BenchPath = "Assets/_Project/Scenes/CampusExterior/Prefabs/CampusConcreteBench.prefab";
    private const string FountainPath = "Assets/_Project/Scenes/CampusExterior/Prefabs/CampusOrnateFountain.prefab";
    private const string ParkingMeshPath = "Assets/_Project/Scenes/CampusExterior/Meshes/StreetParkingBays.asset";
    private const string StonePath = "Assets/_Project/Scenes/CampusExterior/Materials/Aged campus bench stone.mat";
    private const string SteelPath = "Assets/_Project/Scenes/CampusExterior/Materials/Weathered campus steel.mat";
    private const string WaterPath = "Assets/_Project/Scenes/CampusExterior/Materials/Dusty dark campus glazing.mat";

    [MenuItem("Tools/UniDoom/Campus/Apply Parking, Concrete Benches and Fountain")]
    public static void Apply()
    {
        var stone = RequireAsset<Material>(StonePath);
        var steel = RequireAsset<Material>(SteelPath);
        var water = RequireAsset<Material>(WaterPath);

        BuildBenchPrefab(stone, steel);
        BuildFountainPrefab(stone, water);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var parking = FindSceneTransform(scene, "Street Parking Bays - 12")
            ?? throw new InvalidOperationException("Street Parking Bays - 12 was not found.");
        var parkingFilter = parking.GetComponent<MeshFilter>() ?? parking.gameObject.AddComponent<MeshFilter>();
        parkingFilter.sharedMesh = BuildParkingMesh();

        var landscaping = FindSceneTransform(scene, "Campus Landscaping Additions")
            ?? throw new InvalidOperationException("Campus Landscaping Additions was not found.");
        var westCourtyard = FindDescendant(landscaping, "West social courtyard")
            ?? throw new InvalidOperationException("West social courtyard was not found.");

        var simpleFountain = FindDescendant(westCourtyard, "Courtyard fountain");
        if (simpleFountain != null)
        {
            simpleFountain.gameObject.SetActive(false);
        }

        var previous = FindDescendant(landscaping, "Ornate fountain - Law School and Library");
        if (previous != null)
        {
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        }

        var fountainPrefab = RequireAsset<GameObject>(FountainPath);
        var fountain = (GameObject)PrefabUtility.InstantiatePrefab(fountainPrefab, scene);
        fountain.name = "Ornate fountain - Law School and Library";
        fountain.transform.SetParent(landscaping, true);
        fountain.transform.position = new Vector3(-58f, 0f, 16f);
        fountain.transform.rotation = Quaternion.identity;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Validate(scene);
        Selection.activeGameObject = fountain;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("Campus parking/bench/fountain setup applied and saved.");
    }

    private static Mesh BuildParkingMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        AddQuad(vertices, triangles, uvs, -92f, -1.85f, -70f, -1.75f, 0.055f);
        foreach (var x in new[] { -92f, -86.5f, -81f, -75.5f, -70f })
            AddQuad(vertices, triangles, uvs, x - 0.05f, -1.8f, x + 0.05f, 0.2f, 0.055f);

        AddQuad(vertices, triangles, uvs, 15.75f, 52f, 15.85f, 74f, 0.06f);
        foreach (var z in new[] { 52f, 57.5f, 63f, 68.5f, 74f })
            AddQuad(vertices, triangles, uvs, 15.8f, z - 0.05f, 17.8f, z + 0.05f, 0.06f);

        AddQuad(vertices, triangles, uvs, 76.15f, -57f, 76.25f, -35f, 0.06f);
        foreach (var z in new[] { -57f, -51.5f, -46f, -40.5f, -35f })
            AddQuad(vertices, triangles, uvs, 73.7f, z - 0.05f, 76.2f, z + 0.05f, 0.06f);

        var generated = new Mesh { name = "Street Parking Bays - 12" };
        generated.SetVertices(vertices);
        generated.SetTriangles(triangles, 0);
        generated.SetUVs(0, uvs);
        generated.RecalculateNormals();
        generated.RecalculateBounds();

        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(ParkingMeshPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(generated, ParkingMeshPath);
            return generated;
        }

        EditorUtility.CopySerialized(generated, existing);
        UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
        float minX, float minZ, float maxX, float maxZ, float y)
    {
        var start = vertices.Count;
        vertices.Add(new Vector3(minX, y, minZ));
        vertices.Add(new Vector3(minX, y, maxZ));
        vertices.Add(new Vector3(maxX, y, maxZ));
        vertices.Add(new Vector3(maxX, y, minZ));
        triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        uvs.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
    }

    [MenuItem("Tools/UniDoom/Campus/Validate Parking, Concrete Benches and Fountain")]
    public static void ValidateCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Validate(scene);
    }

    private static void BuildBenchPrefab(Material stone, Material steel)
    {
        var root = new GameObject("Campus concrete bench");
        try
        {
            root.isStatic = true;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.58f, 0.03f);
            collider.size = new Vector3(2.18f, 1.16f, 0.78f);

            CreatePrimitive(root.transform, "Light concrete seat", PrimitiveType.Cube,
                new Vector3(0f, 0.5f, -0.04f), new Vector3(2.18f, 0.16f, 0.62f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Light concrete backrest", PrimitiveType.Cube,
                new Vector3(0f, 0.91f, 0.27f), new Vector3(2.18f, 0.58f, 0.13f),
                Quaternion.Euler(-7f, 0f, 0f), stone);

            foreach (var x in new[] { -0.73f, 0.73f })
            {
                CreatePrimitive(root.transform, x < 0f ? "Dark left pedestal" : "Dark right pedestal",
                    PrimitiveType.Cube, new Vector3(x, 0.25f, 0.02f), new Vector3(0.2f, 0.5f, 0.48f),
                    Quaternion.Euler(-4f, 0f, 0f), steel);
                CreatePrimitive(root.transform, x < 0f ? "Dark left foot" : "Dark right foot",
                    PrimitiveType.Cube, new Vector3(x, 0.07f, 0.05f), new Vector3(0.42f, 0.14f, 0.58f),
                    Quaternion.identity, steel);
            }

            PrefabUtility.SaveAsPrefabAsset(root, BenchPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildFountainPrefab(Material stone, Material water)
    {
        var root = new GameObject("Campus ornate fountain");
        try
        {
            root.isStatic = true;
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 2.7f;
            collider.height = 0.62f;
            collider.center = new Vector3(0f, 0.31f, 0f);

            CreatePrimitive(root.transform, "Wide carved stone pool", PrimitiveType.Cylinder,
                new Vector3(0f, 0.27f, 0f), new Vector3(2.7f, 0.27f, 2.7f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Lower pool water", PrimitiveType.Cylinder,
                new Vector3(0f, 0.56f, 0f), new Vector3(2.35f, 0.025f, 2.35f), Quaternion.identity, water);
            CreatePrimitive(root.transform, "Square ornamental plinth", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.92f, 0.7f, 0.92f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Lower carved column", PrimitiveType.Cylinder,
                new Vector3(0f, 0.98f, 0f), new Vector3(0.48f, 0.58f, 0.48f), Quaternion.identity, stone);

            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var headPosition = radial * 1.05f + Vector3.up * 0.72f;
                CreatePrimitive(root.transform, $"Horse head {i + 1}", PrimitiveType.Sphere,
                    headPosition, new Vector3(0.42f, 0.36f, 0.68f), Quaternion.Euler(0f, angle, -12f), stone);
                CreatePrimitive(root.transform, $"Horse neck {i + 1}", PrimitiveType.Capsule,
                    radial * 0.75f + Vector3.up * 0.62f, new Vector3(0.25f, 0.42f, 0.25f),
                    Quaternion.Euler(18f, angle, 0f), stone);
            }

            CreatePrimitive(root.transform, "Lower scalloped tier", PrimitiveType.Cylinder,
                new Vector3(0f, 1.43f, 0f), new Vector3(1.5f, 0.16f, 1.5f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Lower tier water", PrimitiveType.Cylinder,
                new Vector3(0f, 1.61f, 0f), new Vector3(1.35f, 0.018f, 1.35f), Quaternion.identity, water);
            CreatePrimitive(root.transform, "Middle carved column", PrimitiveType.Cylinder,
                new Vector3(0f, 1.88f, 0f), new Vector3(0.35f, 0.42f, 0.35f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Upper scalloped tier", PrimitiveType.Cylinder,
                new Vector3(0f, 2.18f, 0f), new Vector3(0.98f, 0.13f, 0.98f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Upper tier water", PrimitiveType.Cylinder,
                new Vector3(0f, 2.33f, 0f), new Vector3(0.86f, 0.016f, 0.86f), Quaternion.identity, water);
            CreatePrimitive(root.transform, "Figure pedestal", PrimitiveType.Cylinder,
                new Vector3(0f, 2.52f, 0f), new Vector3(0.28f, 0.3f, 0.28f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Classical figure torso", PrimitiveType.Capsule,
                new Vector3(0f, 2.87f, 0f), new Vector3(0.28f, 0.4f, 0.24f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Classical figure head", PrimitiveType.Sphere,
                new Vector3(0f, 3.28f, 0f), new Vector3(0.23f, 0.27f, 0.23f), Quaternion.identity, stone);
            CreatePrimitive(root.transform, "Figure left arm", PrimitiveType.Capsule,
                new Vector3(-0.25f, 2.93f, 0f), new Vector3(0.1f, 0.32f, 0.1f), Quaternion.Euler(0f, 0f, 35f), stone);
            CreatePrimitive(root.transform, "Figure right arm", PrimitiveType.Capsule,
                new Vector3(0.25f, 2.93f, 0f), new Vector3(0.1f, 0.32f, 0.1f), Quaternion.Euler(0f, 0f, -35f), stone);

            CreateWaterJet(root.transform, "Top water crown", new Vector3(0f, 3.62f, 0f),
                new Vector3(0f, 0.72f, 0f), 0.07f, water);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * 45f;
                var radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                CreateWaterJet(root.transform, $"Cascading jet {i + 1}",
                    radial * 1.28f + Vector3.up * 1.33f, radial * 0.72f + Vector3.down * 0.72f, 0.035f, water);
            }

            PrefabUtility.SaveAsPrefabAsset(root, FountainPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        go.isStatic = true;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void CreateWaterJet(Transform parent, string name, Vector3 localPosition,
        Vector3 localDirection, float radius, Material water)
    {
        var length = localDirection.magnitude;
        var jet = CreatePrimitive(parent, name, PrimitiveType.Cylinder,
            localPosition + localDirection * 0.5f, new Vector3(radius, length * 0.5f, radius),
            Quaternion.FromToRotation(Vector3.up, localDirection.normalized), water);
        jet.isStatic = false;
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidOperationException($"Required asset is missing: {path}");
    }

    private static Transform FindSceneTransform(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == name);
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transform => transform.name == name);
    }

    private static void Validate(Scene scene)
    {
        var parking = FindSceneTransform(scene, "Street Parking Bays - 12")
            ?? throw new InvalidOperationException("The 12 parking-bay markings are missing.");
        if (parking.GetComponent<MeshRenderer>() == null || parking.GetComponent<MeshFilter>()?.sharedMesh == null)
        {
            throw new InvalidOperationException("The parking-bay markings are incomplete.");
        }

        var fountain = FindSceneTransform(scene, "Ornate fountain - Law School and Library")
            ?? throw new InvalidOperationException("The ornate fountain instance is missing.");
        if (fountain.transform.position != new Vector3(-58f, 0f, 16f))
        {
            throw new InvalidOperationException("The ornate fountain is not at the intended courtyard position.");
        }

        var bench = RequireAsset<GameObject>(BenchPath);
        if (bench.GetComponentsInChildren<MeshRenderer>(true).Length < 6)
        {
            throw new InvalidOperationException("The concrete bench prefab is incomplete.");
        }

        var courtyardBenches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name.StartsWith("Garden bench", StringComparison.Ordinal))
            .ToArray();
        if (courtyardBenches.Length != 12 || courtyardBenches.Any(item =>
                item.GetComponentsInChildren<MeshRenderer>(true).Length < 6))
        {
            throw new InvalidOperationException(
                $"Expected 12 resolved concrete courtyard benches, found {courtyardBenches.Length}.");
        }

        Debug.Log($"Campus validation passed: parking='{parking.name}', bench renderers=" +
                  $"{bench.GetComponentsInChildren<MeshRenderer>(true).Length}, bench instances=" +
                  $"{courtyardBenches.Length}, fountain renderers=" +
                  $"{fountain.GetComponentsInChildren<MeshRenderer>(true).Length}.");
    }
}
