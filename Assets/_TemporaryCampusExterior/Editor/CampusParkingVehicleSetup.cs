using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Explicit editor migration. Never regenerate user assets on import/domain reload.
public static class CampusParkingVehicleSetup
{
    const string Base = "Assets/_Project/Scenes/CampusExterior/";
    const string ScenePath = Base + "CampusExterior.unity";
    const string CampusPath = Base + "Prefabs/CampusExterior.prefab";
    const string LayoutPath = Base + "Prefabs/CampusParkedVehicles.prefab";
    const string MeshPath = Base + "Meshes/StreetParkingBays.asset";
    const string AppliedLabel = "CampusParkingVehiclesRevision1Applied";
    const string GroupName = "CampusParkedVehicles";
    static bool applying;

    [Serializable] public class MaterialSpec
    {
        public string name, texture;
        public float metallic, smoothness;
        public bool glass, paint;
    }
    [Serializable] public class ModelSpec
    {
        public string name;
        public Vector3 size;
        public MaterialSpec[] materials;
        public int[] triangles;
    }
    readonly struct Bay
    {
        public readonly Vector3 center;
        public readonly float yaw;
        public Bay(float x, float z, float yaw) { center = new Vector3(x, 0, z); this.yaw = yaw; }
    }
    static readonly Bay[] Bays = Enumerable.Range(0, 4).Select(i => new Bay(-91 + 6 * i, -1.55f, 90))
        .Concat(Enumerable.Range(0, 4).Select(i => new Bay(16.35f, 55 + 6 * i, 0)))
        .Concat(Enumerable.Range(0, 4).Select(i => new Bay(75.15f, -56 + 6 * i, 0))).ToArray();

    static bool HasDirtyScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) return true;
        return false;
    }

    [MenuItem("Tools/UniDoom/Campus/Apply Parking Vehicles")]
    public static void Apply()
    {
        if (applying) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || HasDirtyScenes() || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Save your scene changes and leave Play Mode/Prefab Stage before applying parking.");
        applying = true;
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        GameObject prefabRoot = null;
        try
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            var carSpec = ReadSpec("Hatchback");
            var bikeSpec = ReadSpec("Motorcycle");
            EnsureFolder(Base + "Meshes/ParkedVehicles");
            EnsureFolder(Base + "Materials/ParkedVehicles");
            var carMeshes = ReadMeshes(carSpec);
            var bikeMeshes = ReadMeshes(bikeSpec);
            var grey = BuildVehicle(carSpec, carMeshes, shader, "Grey", new Color(.58f, .56f, .51f));
            var bike = BuildVehicle(bikeSpec, bikeMeshes, shader, "Teal", Color.white);
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var campus = Find(scene, "Campus Exterior - Part 2") ?? throw new InvalidOperationException("Campus root is missing.");
            var road = FindRoad(campus);
            // Preflight all marking positions before replacing the saved marking mesh.
            foreach (Bay bay in Bays)
                foreach (float x in new[] { -1.3f, 0f, 1.3f })
                    foreach (float z in new[] { -3.05f, 0f, 3.05f })
                        RoadHit(road, bay.center + Quaternion.Euler(0, bay.yaw, 0) * new Vector3(x, 0, z));

            var mesh = BuildParkingMesh(campus);
            SetMarkings(campus, mesh);
            var layout = BuildLayout(road, grey, bike);
            PlaceLayout(campus, layout);
            Validate(campus);

            // The scene contains a serialized campus root, so update the reference prefab too.
            prefabRoot = PrefabUtility.LoadPrefabContents(CampusPath);
            SetMarkings(prefabRoot.transform, mesh);
            PlaceLayout(prefabRoot.transform, layout);
            Validate(prefabRoot.transform);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CampusPath);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save CampusExterior scene.");
            AssetDatabase.SaveAssets();
            AssetDatabase.SetLabels(layout, AssetDatabase.GetLabels(layout).Append(AppliedLabel).Distinct().ToArray());
            AssetDatabase.SaveAssets();
            Debug.Log("Campus parking saved: 12 parallel bays (6 x 2.5 m), 6 cars and 3 motorcycles in 8 occupied bays. " +
                "Scene and reference prefab updated. Play Mode and visual review are still required.");
        }
        finally
        {
            if (prefabRoot != null) PrefabUtility.UnloadPrefabContents(prefabRoot);
            if (opened && scene.IsValid() && scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            applying = false;
        }
    }

    static ModelSpec ReadSpec(string name)
    {
        var spec = JsonUtility.FromJson<ModelSpec>(File.ReadAllText(Base + "Models/ParkedVehicles/" + name + ".json"));
        if (spec == null || spec.name != name || spec.materials == null || spec.materials.Length == 0)
            throw new InvalidDataException("Invalid vehicle material manifest: " + name);
        return spec;
    }

    static Mesh[][] ReadMeshes(ModelSpec spec)
    {
        var lods = new Mesh[3][];
        for (int lod = 0; lod < 3; lod++)
        {
            using var reader = new BinaryReader(File.OpenRead(Base + "Models/ParkedVehicles/" + spec.name + "_LOD" + lod + ".bytes"));
            if (reader.ReadInt32() != 0x55445631) throw new InvalidDataException("Invalid vehicle mesh header.");
            int count = reader.ReadInt32();
            if (count != spec.materials.Length) throw new InvalidDataException("Vehicle material count mismatch.");
            lods[lod] = new Mesh[count];
            int triangleCount = 0;
            for (int part = 0; part < count; part++)
            {
                int material = reader.ReadInt32(), vc = reader.ReadInt32(), ic = reader.ReadInt32();
                if (material != part || vc < 3 || vc > 1000000 || ic < 3 || ic > 3000000 || ic % 3 != 0)
                    throw new InvalidDataException("Invalid vehicle geometry dimensions.");
                var vertices = new Vector3[vc]; var normals = new Vector3[vc]; var uv = new Vector2[vc];
                for (int i = 0; i < vc; i++)
                {
                    vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }
                var indices = new int[ic];
                for (int i = 0; i < ic; i++)
                {
                    indices[i] = reader.ReadInt32();
                    if (indices[i] < 0 || indices[i] >= vc) throw new InvalidDataException("Vehicle index out of bounds.");
                }
                var mesh = new Mesh { name = spec.name + "_LOD" + lod + "_" + part,
                    indexFormat = vc > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = indices;
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                lods[lod][part] = SaveAsset(mesh, Base + "Meshes/ParkedVehicles/" + mesh.name + ".asset");
                triangleCount += ic / 3;
            }
            if (reader.BaseStream.Position != reader.BaseStream.Length || triangleCount != spec.triangles[lod])
                throw new InvalidDataException("Vehicle geometry manifest mismatch.");
        }
        return lods;
    }

    static GameObject BuildVehicle(ModelSpec spec, Mesh[][] meshes, Shader shader, string variant, Color paint)
    {
        var materials = new Material[spec.materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            var source = spec.materials[i];
            var importer = AssetImporter.GetAtPath(source.texture) as TextureImporter;
            if (importer == null) throw new InvalidDataException("Vehicle texture is missing: " + source.texture);
            if (importer.maxTextureSize != 512 || !importer.mipmapEnabled)
            {
                importer.maxTextureSize = 512; importer.mipmapEnabled = true; importer.SaveAndReimport();
            }
            string suffix = source.paint ? variant : "Shared";
            string name = spec.name + "_" + i + "_" + suffix;
            var material = new Material(shader) { name = name };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(source.texture));
            bool frontLens = source.name == "clearlens" || source.name == "Original clear optical lens";
            var color = frontLens ? new Color(.19f, .22f, .25f) :
                source.paint ? paint : source.glass ? new Color(.12f, .15f, .17f) : Color.white;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            material.SetFloat("_Metallic", source.glass ? .4f : source.metallic);
            material.SetFloat("_Smoothness", frontLens ? .38f : source.smoothness);
            materials[i] = SaveAsset(material, Base + "Materials/ParkedVehicles/" + name + ".mat");
        }
        var root = new GameObject("Parked " + spec.name + " " + variant);
        try
        {
            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            var levels = new LOD[3];
            for (int lod = 0; lod < 3; lod++)
            {
                var group = new GameObject("LOD" + lod); group.transform.SetParent(root.transform, false);
                var renderers = new Renderer[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    var part = new GameObject(spec.materials[i].name); part.transform.SetParent(group.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = meshes[lod][i];
                    var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials[i];
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                    if (spec.materials[i].name == "clearlens" || spec.materials[i].name == "redlens" ||
                        spec.materials[i].name == "Original clear optical lens" ||
                        spec.materials[i].name == "Original red optical lens" ||
                        spec.materials[i].name == "Original amber optical lens")
                        part.SetActive(false);
                    GameObjectUtility.SetStaticEditorFlags(part, StaticEditorFlags.BatchingStatic);
                    renderers[i] = renderer;
                }
                levels[lod] = new LOD(new[] { .16f, .06f, .008f }[lod], renderers);
            }
            var lodGroup = root.AddComponent<LODGroup>(); lodGroup.SetLODs(levels); lodGroup.RecalculateBounds();
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(spec.size.x * .88f, spec.size.y * .8f, spec.size.z * .96f);
            collider.center = new Vector3(0, spec.size.y * .46f, 0);
            return PrefabUtility.SaveAsPrefabAsset(root, Base + "Prefabs/CampusParked" + spec.name + variant + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static GameObject BuildLayout(MeshCollider road, GameObject grey, GameObject bike)
    {
        var root = new GameObject(GroupName);
        try
        {
            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            var coupeBlue = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedLuxuryCoupeSapphire.prefab");
            var coupeSilver = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedLuxuryCoupeSilver.prefab");
            var sedanGraphite = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedExecutiveSedanGraphite.prefab");
            var sedanWine = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedExecutiveSedanWine.prefab");
            var suv = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedPremiumSUVPearl.prefab");
            var sportBike = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedSportBikeRed.prefab");
            var touringBike = RequireAsset<GameObject>(Base + "Prefabs/CampusParkedTouringBikeBlack.prefab");
            AddVehicle(root.transform, road, grey, 0, 0, "W1 - grey hatchback");
            AddVehicle(root.transform, road, coupeBlue, 1, 0, "W2 - sapphire luxury coupe");
            AddVehicle(root.transform, road, suv, 2, 0, "W3 - pearl premium SUV");
            AddVehicle(root.transform, road, sportBike, 3, -1.35f, "W4 - red sport bike");
            AddVehicle(root.transform, road, bike, 3, 1.35f, "W4 - motorcycle B");
            AddVehicle(root.transform, road, sedanGraphite, 4, 0, "N1 - graphite executive sedan");
            AddVehicle(root.transform, road, coupeSilver, 6, 0, "N3 - silver luxury coupe", true);
            AddVehicle(root.transform, road, sedanWine, 5, 0, "N2 - wine executive sedan", true);
            AddVehicle(root.transform, road, touringBike, 7, 0, "N4 - black touring bike");
            return PrefabUtility.SaveAsPrefabAsset(root, LayoutPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void AddVehicle(Transform parent, MeshCollider road, GameObject prefab, int index, float offset, string name, bool reverse = false)
    {
        Bay bay = Bays[index]; Quaternion heading = Quaternion.Euler(0, bay.yaw, 0);
        Vector3 point = bay.center + heading * new Vector3(0, 0, offset);
        var hit = RoadHit(road, point);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.position = hit.point + Vector3.up * .004f;
        instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * heading * Quaternion.Euler(0, reverse ? 180 : 0, 0);
        // Verify the full visual footprint, including mirrors, against the parking space.
        bool hasLodGroup = instance.GetComponent<LODGroup>() != null;
        var meshes = instance.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => !hasLodGroup || f.transform.parent.name == "LOD0");
        foreach (var filter in meshes)
        {
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 local = Quaternion.Inverse(heading) * (filter.transform.TransformPoint(vertex) - bay.center);
                if (Mathf.Abs(local.x) > 1.25f || Mathf.Abs(local.z) > 3f)
                    throw new InvalidOperationException(name + " extends outside its car bay.");
            }
        }
    }

    public static Mesh BuildParkingMesh(Transform campus)
    {
        var road = FindRoad(campus);
        var vertices = new List<Vector3>(); var indices = new List<int>(); var uv = new List<Vector2>();
        foreach (int start in new[] { 0, 4, 8 })
        {
            Bay first = Bays[start]; Quaternion q = Quaternion.Euler(0, first.yaw, 0);
            Vector3 origin = first.center - q * new Vector3(0, 0, 3);
            AddLine(-1.25f, 0, -1.25f, 24); AddLine(1.25f, 0, 1.25f, 24);
            for (int i = 0; i <= 4; i++) AddLine(-1.25f, i * 6, 1.25f, i * 6);
            void AddLine(float x1, float z1, float x2, float z2)
            {
                Vector3 a = origin + q * new Vector3(x1, 0, z1), b = origin + q * new Vector3(x2, 0, z2);
                Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized) * .05f;
                int segments = Mathf.CeilToInt(Vector3.Distance(a, b) / .5f);
                for (int j = 0; j < segments; j++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (float)j / segments), r = Vector3.Lerp(a, b, (float)(j + 1) / segments);
                    int k = vertices.Count;
                    foreach (Vector3 v in new[] { p - side, r - side, r + side, p + side })
                        vertices.Add(RoadHit(road, v).point + Vector3.up * .006f);
                    indices.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                    uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                }
            }
        }
        var mesh = new Mesh { name = "Street Parking Bays - 12" };
        mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return SaveAsset(mesh, MeshPath);
    }

    static MeshCollider FindRoad(Transform root) => root.GetComponentsInChildren<MeshCollider>(true)
        .FirstOrDefault(c => c.name == "Campus asphalt" && c.enabled && c.sharedMesh != null)
        ?? throw new InvalidOperationException("Active campus asphalt collider is missing.");

    static RaycastHit RoadHit(MeshCollider road, Vector3 position)
    {
        if (!road.Raycast(new Ray(new Vector3(position.x, 5f, position.z), Vector3.down), out var hit, 10f))
            throw new InvalidOperationException("Parking extends outside asphalt at " + position);
        return hit;
    }

    static void SetMarkings(Transform campus, Mesh mesh)
    {
        var marking = campus.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Street Parking Bays - 12");
        Material paint = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("558ad9f45fe8d274381ae04514b34fef"));
        if (paint == null) throw new InvalidOperationException("Existing road-paint material is missing.");
        if (marking == null)
        {
            marking = new GameObject("Street Parking Bays - 12").transform; marking.SetParent(campus, false);
        }
        marking.SetPositionAndRotation(Vector3.zero, Quaternion.identity); marking.localScale = Vector3.one;
        // Unity can return a missing-component wrapper whose CLR reference is non-null.
        // Use Unity's lifetime check rather than the null-coalescing operator.
        var filter = marking.GetComponent<MeshFilter>();
        if (!filter) filter = marking.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = marking.GetComponent<MeshRenderer>();
        if (!renderer) renderer = marking.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = paint; renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void PlaceLayout(Transform campus, GameObject prefab)
    {
        var previous = Find(campus.gameObject.scene, GroupName);
        if (previous != null)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(previous.gameObject) != prefab)
                throw new InvalidOperationException("An unrelated object uses the parking group name; preserving it.");
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, campus);
        instance.name = GroupName; instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    static void Validate(Transform campus)
    {
        var group = campus.GetComponentsInChildren<Transform>(true).Single(t => t.name == GroupName);
        if (group.childCount != 9 || group.GetComponentsInChildren<BoxCollider>(true).Length != 9 ||
            group.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            throw new InvalidOperationException("Parking must contain nine fixed vehicles with colliders.");
        var colliders = group.GetComponentsInChildren<BoxCollider>(true);
        for (int i = 0; i < colliders.Length; i++)
            for (int j = i + 1; j < colliders.Length; j++)
                if (Physics.ComputePenetration(colliders[i], colliders[i].transform.position, colliders[i].transform.rotation,
                    colliders[j], colliders[j].transform.position, colliders[j].transform.rotation, out _, out _))
                    throw new InvalidOperationException("Two parked vehicle colliders overlap.");
    }

    static Transform Find(Scene scene, string name) => scene.GetRootGameObjects()
        .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);

    static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new FileNotFoundException("Required parking asset is missing: " + path);
        return asset;
    }

    static T SaveAsset<T>(T generated, string path) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(generated, path); return generated; }
        EditorUtility.CopySerialized(generated, existing); UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing); return existing;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
