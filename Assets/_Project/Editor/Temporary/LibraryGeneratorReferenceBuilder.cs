using System;
using System.Collections.Generic;
using UniDoom.CampusLibrary;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class LibraryGeneratorReferenceBuilder
{
    const string PrefabPath = "Assets/_Project/Scenes/CampusLibrary/Prefabs/CampusLibrary.prefab";
    const string MaterialFolder = "Assets/_Project/Scenes/CampusLibrary/Materials/Generator";
    const string MeshFolder = "Assets/_Project/Scenes/CampusLibrary/Meshes/Generator";
    const string AssemblyName = "Reference generator assembly";

    [MenuItem("Tools/UniDoom/CampusLibrary/Build Reference Generator")]
    public static void Build()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);

        Material steel = Material("Generator dark steel", new Color(.075f, .09f, .09f), .82f, .26f);
        Material painted = Material("Generator worn teal", new Color(.12f, .25f, .24f), .66f, .19f);
        Material black = Material("Generator oily black", new Color(.022f, .025f, .024f), .55f, .2f);
        Material red = Material("Generator oxide red", new Color(.34f, .075f, .055f), .7f, .18f);
        Material copper = Material("Generator aged copper", new Color(.42f, .2f, .075f), .75f, .28f);
        Material ceramic = Material("Generator fuse ceramic", new Color(.72f, .68f, .5f), .18f, .34f);
        Material glass = Material("Generator indicator glass", new Color(.018f, .055f, .025f), .1f, .5f, Color.black);

        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform anchor = Find(prefab.transform, "Library backup generator");
            if (!anchor) throw new InvalidOperationException("Library backup generator was not found in CampusLibrary.prefab.");

            Transform parent = anchor.parent;
            Transform previous = Find(parent, AssemblyName);
            if (previous) Object.DestroyImmediate(previous.gameObject);

            DisableLegacyRenderers(parent);

            var assembly = new GameObject(AssemblyName);
            assembly.transform.SetParent(parent, false);
            assembly.transform.position = new Vector3(anchor.position.x, .02f, anchor.position.z);
            assembly.transform.rotation = Quaternion.identity;

            BuildCabinet(assembly.transform, steel, painted, black, red, copper);
            BuildHose(assembly.transform, black);
            BuildFusePanel(assembly, steel, black, copper, ceramic, glass);

            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            Debug.Log("CampusLibrary reference generator rebuilt with three visible fuse sockets and green connected indicators.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void BuildCabinet(Transform root, Material steel, Material painted, Material black, Material red, Material copper)
    {
        // Open-front cabinet, matching the broad silhouette and exposed machinery of the reference.
        Cube("Cabinet back", root, new Vector3(0, 1.08f, .58f), new Vector3(2.55f, 1.95f, .09f), painted);
        Cube("Cabinet base", root, new Vector3(0, .12f, 0), new Vector3(2.65f, .18f, 1.35f), steel);
        Cube("Cabinet top", root, new Vector3(0, 2.06f, 0), new Vector3(2.65f, .16f, 1.35f), steel);
        Cube("Cabinet left frame", root, new Vector3(-1.26f, 1.08f, 0), new Vector3(.16f, 1.95f, 1.35f), steel);
        Cube("Cabinet right frame", root, new Vector3(1.26f, 1.08f, 0), new Vector3(.16f, 1.95f, 1.35f), steel);
        Cube("Cabinet middle brace", root, new Vector3(.12f, 1.08f, -.6f), new Vector3(.1f, 1.82f, .1f), steel);
        Cube("Cabinet upper front rail", root, new Vector3(0, 1.98f, -.62f), new Vector3(2.5f, .11f, .11f), steel);
        Cube("Cabinet lower front rail", root, new Vector3(0, .2f, -.62f), new Vector3(2.5f, .11f, .11f), steel);

        // Open doors keep the controls and internal components readable from the player's approach.
        Cube("Open left cabinet door", root, new Vector3(-1.66f, 1.08f, -.82f), new Vector3(.76f, 1.78f, .07f), painted, new Vector3(0, 58f, 0));
        Cube("Open right cabinet door", root, new Vector3(1.66f, 1.08f, -.82f), new Vector3(.76f, 1.78f, .07f), painted, new Vector3(0, -58f, 0));

        // Left reservoir and pump.
        Cube("Left reservoir", root, new Vector3(-.72f, .67f, .03f), new Vector3(.72f, .82f, .66f), painted);
        Cylinder("Left reservoir cap", root, new Vector3(-.72f, 1.13f, -.05f), new Vector3(.16f, .06f, .16f), steel);
        Cylinder("Pump motor", root, new Vector3(-.65f, 1.38f, .08f), new Vector3(.32f, .62f, .32f), black, new Vector3(0, 0, 90));
        Cylinder("Pump copper collar", root, new Vector3(-.28f, 1.38f, .08f), new Vector3(.2f, .1f, .2f), copper, new Vector3(0, 0, 90));

        // Central alternator and lower flywheel.
        Cylinder("Alternator body", root, new Vector3(.08f, .68f, .04f), new Vector3(.43f, .74f, .43f), black, new Vector3(90, 0, 0));
        Cylinder("Alternator front plate", root, new Vector3(.08f, .68f, -.39f), new Vector3(.34f, .07f, .34f), steel, new Vector3(90, 0, 0));
        for (int i = -2; i <= 2; i++)
            Cube("Alternator cooling fin " + (i + 3), root, new Vector3(i * .13f + .08f, .68f, -.45f), new Vector3(.045f, .68f, .08f), steel);

        // Red fuel/control tank and a compact electrical box.
        Cube("Fuel tank", root, new Vector3(.72f, .72f, .02f), new Vector3(.88f, .52f, .68f), red);
        Cylinder("Fuel tank cap", root, new Vector3(.72f, 1.02f, -.06f), new Vector3(.13f, .07f, .13f), copper);
        Cube("Upper control box", root, new Vector3(.7f, 1.64f, .17f), new Vector3(.9f, .4f, .46f), black);
        Cube("Control box lip", root, new Vector3(.7f, 1.64f, -.09f), new Vector3(.98f, .47f, .07f), steel);

        // A few pipes and feet make the assembly read as functional instead of a plain cabinet.
        Cylinder("Vertical pressure pipe", root, new Vector3(-1.03f, 1.28f, -.35f), new Vector3(.055f, .76f, .055f), copper);
        Cylinder("Pressure pipe elbow", root, new Vector3(-.83f, 1.65f, -.35f), new Vector3(.055f, .4f, .055f), copper, new Vector3(0, 0, 90));
        Cube("Left rubber foot", root, new Vector3(-.85f, .04f, -.18f), new Vector3(.35f, .08f, .42f), black);
        Cube("Right rubber foot", root, new Vector3(.85f, .04f, -.18f), new Vector3(.35f, .08f, .42f), black);
    }

    static void BuildHose(Transform root, Material material)
    {
        Mesh mesh = CreateHoseMesh();
        const string path = MeshFolder + "/Generator ribbed hose.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing)
        {
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            mesh = existing;
        }
        else AssetDatabase.CreateAsset(mesh, path);

        var go = new GameObject("Ribbed flexible generator hose");
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    static Mesh CreateHoseMesh()
    {
        const int segments = 28;
        const int sides = 10;
        var vertices = new Vector3[(segments + 1) * sides];
        var triangles = new int[segments * sides * 6];
        Vector3 p0 = new Vector3(-.58f, 1.59f, -.3f);
        Vector3 p1 = new Vector3(-.42f, 2.02f, -.78f);
        Vector3 p2 = new Vector3(.2f, 1.86f, -.91f);
        Vector3 p3 = new Vector3(.3f, 1.28f, -.4f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 center = Bezier(p0, p1, p2, p3, t);
            Vector3 tangent = BezierTangent(p0, p1, p2, p3, t).normalized;
            Vector3 side = Vector3.Cross(tangent, Vector3.up).normalized;
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            Vector3 up = Vector3.Cross(side, tangent).normalized;
            float radius = .065f * (1f + .13f * Mathf.Sin(t * Mathf.PI * 36f));
            for (int s = 0; s < sides; s++)
            {
                float angle = s * Mathf.PI * 2f / sides;
                vertices[i * sides + s] = center + (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            }
        }

        int index = 0;
        for (int i = 0; i < segments; i++)
        for (int s = 0; s < sides; s++)
        {
            int next = (s + 1) % sides;
            int a = i * sides + s;
            int b = i * sides + next;
            int c = (i + 1) * sides + s;
            int d = (i + 1) * sides + next;
            triangles[index++] = a; triangles[index++] = c; triangles[index++] = b;
            triangles[index++] = b; triangles[index++] = c; triangles[index++] = d;
        }

        var mesh = new Mesh { name = "Generator ribbed hose" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void BuildFusePanel(GameObject assembly, Material steel, Material black, Material copper, Material ceramic, Material glass)
    {
        Transform root = assembly.transform;
        Cube("Three-fuse panel backplate", root, new Vector3(.74f, 1.51f, -.64f), new Vector3(.94f, .72f, .1f), black);
        Cube("Fuse panel upper label rail", root, new Vector3(.74f, 1.86f, -.71f), new Vector3(.94f, .08f, .06f), steel);

        var fuseObjects = new GameObject[3];
        var indicators = new Renderer[3];
        var lights = new Light[3];
        for (int i = 0; i < 3; i++)
        {
            float x = .45f + i * .29f;
            Cylinder("Fuse socket upper " + (i + 1), root, new Vector3(x, 1.64f, -.73f), new Vector3(.075f, .055f, .075f), copper);
            Cylinder("Fuse socket lower " + (i + 1), root, new Vector3(x, 1.28f, -.73f), new Vector3(.075f, .055f, .075f), copper);

            var fuse = new GameObject("Installed fuse " + (i + 1));
            fuse.transform.SetParent(root, false);
            Cylinder("Ceramic cartridge", fuse.transform, new Vector3(x, 1.46f, -.76f), new Vector3(.055f, .25f, .055f), ceramic);
            Cylinder("Upper brass cap", fuse.transform, new Vector3(x, 1.6f, -.76f), new Vector3(.068f, .045f, .068f), copper);
            Cylinder("Lower brass cap", fuse.transform, new Vector3(x, 1.32f, -.76f), new Vector3(.068f, .045f, .068f), copper);
            fuse.SetActive(false);
            fuseObjects[i] = fuse;

            GameObject indicator = Cylinder("Fuse connected light " + (i + 1), root, new Vector3(x, 1.77f, -.77f), new Vector3(.052f, .028f, .052f), glass, new Vector3(90, 0, 0));
            indicators[i] = indicator.GetComponent<Renderer>();
            Light light = indicator.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.08f, 1f, .24f);
            light.intensity = 1.8f;
            light.range = .7f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            lights[i] = light;
        }

        var state = assembly.AddComponent<LibraryGeneratorVisualState>();
        var serialized = new SerializedObject(state);
        Assign(serialized.FindProperty("installedFuses"), fuseObjects);
        Assign(serialized.FindProperty("indicatorRenderers"), indicators);
        Assign(serialized.FindProperty("indicatorLights"), lights);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Assign<T>(SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
        => Primitive(PrimitiveType.Cube, name, parent, position, scale, material, rotation);

    static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
        => Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material, rotation);

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localEulerAngles = rotation;
        go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static Material Material(string name, Color color, float metallic, float smoothness, Color? emission = null)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader was not found.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        Color glow = emission ?? Color.black;
        material.SetColor("_EmissionColor", glow);
        if (glow.maxColorComponent > 0) material.EnableKeyword("_EMISSION");
        else material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    static void DisableLegacyRenderers(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            string name = renderer.gameObject.name;
            if (name == "Library backup generator" || name.StartsWith("Generator ", StringComparison.Ordinal) ||
                name.StartsWith("Installed fuse socket", StringComparison.Ordinal) || name == "Eye level fuse enclosure")
                renderer.enabled = false;
        }
    }

    static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    static Vector3 BezierTangent(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (b - a) + 6f * u * t * (c - b) + 3f * t * t * (d - c);
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
