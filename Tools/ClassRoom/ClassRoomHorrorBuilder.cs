// Run in an isolated Unity project; preserves the existing shell, door and material style.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ClassRoomHorrorBuilder
{
    const string Art = "Assets/Art/ClassRoom";
    const string InteriorPath = Art + "/AbandonedClassRoomInterior.prefab";
    const string DetailName = "University apocalypse details";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static System.Random random;
    static Transform details;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());

    public static void Build()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/.utmp/"))
            throw new Exception("Generate in an isolated project and install the validated assets.");
        random = new System.Random(6661978);
        materials.Clear();
        foreach (string path in Directory.GetFiles(Art + "/Materials", "*.mat"))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            materials[material.name] = material;
        }
        var fabric = FabricTexture();
        Surface("DriedBlood", "Paper", new Color(.40f, .020f, .027f), .12f);
        Surface("Blood", "Paper", new Color(.76f, .026f, .038f), .40f);
        Surface("Soot", "Black", new Color(.038f, .030f, .027f), .01f);
        Surface("Mildew", "Rubble", new Color(.15f, .19f, .12f), .01f);
        Surface("Dust", "Paper", new Color(.43f, .39f, .29f), .01f);
        Surface("CorpseCoat", "Paper", new Color(.42f, .48f, .45f), .01f);
        Surface("CorpseTrousers", "Paper", new Color(.22f, .27f, .30f), .01f);
        Surface("CoveredFace", "Canvas", new Color(.59f, .53f, .42f), .01f);
        Surface("Tarp", "Canvas", new Color(.84f, .81f, .69f), .01f);
        Surface("Caution", "Paper", new Color(.78f, .57f, .13f), .02f);
        foreach(string name in new[] { "Blood", "DriedBlood", "CorpseCoat", "CorpseTrousers", "CoveredFace", "Tarp" })
        {
            materials[name].SetTexture("_BaseMap", fabric);
            materials[name].SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(materials[name]);
        }

        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab");
        var controller = player.GetComponent<CharacterController>();
        float playerHeight = controller.height * player.transform.localScale.y;
        float relativeScale = playerHeight / 2.1209743f;
        var furnitureScale = V(1.32f, 1.28f, 1.32f) * relativeScale;
        var root = PrefabUtility.LoadPrefabContents(InteriorPath);
        try
        {
            // Absolute values make repeated generation safe: desks are never scaled twice.
            foreach (Transform t in root.transform)
            {
                if (t.name.StartsWith("Student desk ") || t.name.StartsWith("Student chair ") ||
                    t.name.StartsWith("Displaced desk barrier ") || t.name == "Fallen chair beside displaced desks")
                    t.localScale = furnitureScale;
                if (t.name == "Teacher desk" || t.name == "Side supply table")
                    t.localScale = V(1.14f, 1.28f, 1.14f) * relativeScale;
                if (t.name == "Teacher chair")
                {
                    t.localScale = furnitureScale;
                    t.localPosition = V(.3f, 0, 4.34f);
                }
                if (t.name.StartsWith("Student chair "))
                {
                    string deskName = t.name.Replace("chair", "desk");
                    var desk = root.transform.Find(deskName);
                    t.localPosition = V(desk.localPosition.x, 0, desk.localPosition.z - .79f);
                }
            }
            // Larger furniture needs more room between rows and a generous right flank.
            float[] columns = { -4.4f, -2.0f, .4f, 4.5f, 7.3f, 10.1f };
            float[] rows = { -9.65f, -8.15f, -5.4f, -3.9f, -1.15f, .35f, 1.85f };
            for (int row = 0; row < rows.Length; row++) for (int col = 0; col < columns.Length; col++)
            {
                var desk = root.transform.Find("Student desk " + row + "-" + col);
                var chair = root.transform.Find("Student chair " + row + "-" + col);
                desk.localPosition = V(columns[col], 0, rows[row]);
                chair.localPosition = V(columns[col], 0, rows[row] - .72f);
                chair.localRotation = desk.localRotation;
            }
            Topple(root.transform.Find("Student desk 1-5"), V(10.1f, 0, -8.15f), V(0, 12, 78));
            Topple(root.transform.Find("Student chair 1-5"), V(10.35f, 0, -9.15f), V(78, 25, 0));
            Topple(root.transform.Find("Student chair 4-0"), V(-4.65f, 0, -2.0f), V(0, -17, 82));
            root.transform.Find("Small emergency desk lamp").localPosition = V(4.76f, 1.055f, 4.02f);
            root.transform.Find("Warm desk lamp").localPosition = V(4.76f, 1.24f, 4.02f);

            var old = root.transform.Find(DetailName);
            if (old) Object.DestroyImmediate(old.gameObject);
            details = new GameObject(DetailName).transform;
            details.SetParent(root.transform, false);
            University(root.transform);
            Decay();
            Aftermath();
            Lighting(root.transform);
            foreach (Transform group in details.Cast<Transform>().ToArray()) Combine(group);
            PrefabUtility.SaveAsPrefabAsset(root, InteriorPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        ResizeProp("StudentDesk", furnitureScale);
        ResizeProp("StudentChair", furnitureScale);
        AssetDatabase.SaveAssets();
        ClassRoomHorrorVerifier.VerifyAndRender();
        Debug.Log("CLASSROOM_HORROR_BUILD_OK");
    }

    static void Surface(string name, string source, Color color, float smoothness)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var created = new Material(materials[source]) { name = name };
        created.SetColor("_BaseColor", color);
        created.SetFloat("_Smoothness", smoothness);
        created.SetFloat("_Metallic", 0);
        created.DisableKeyword("_EMISSION");
        created.SetColor("_EmissionColor", Color.black);
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing)
        {
            EditorUtility.CopySerialized(created, existing);
            Object.DestroyImmediate(created);
        }
        else { AssetDatabase.CreateAsset(created, path); existing = created; }
        materials[name] = existing;
    }

    static Texture2D FabricTexture()
    {
        string path = Art + "/Textures/ApocalypseFabric.png";
        var texture = new Texture2D(128, 128, TextureFormat.RGB24, false);
        var noise = new System.Random(61978);
        for(int y = 0; y < 128; y += 4) for(int x = 0; x < 128; x += 4)
        {
            float value = .72f + (float)noise.NextDouble() * .26f;
            for(int yy = 0; yy < 4; yy++) for(int xx = 0; xx < 4; xx++)
                texture.SetPixel(x + xx, y + yy, new Color(value, value, value));
        }
        texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.filterMode = FilterMode.Point; importer.maxTextureSize = 128; importer.anisoLevel = 1;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void ResizeProp(string name, Vector3 scale)
    {
        string path = Art + "/Prefabs/" + name + ".prefab";
        if (!File.Exists(path)) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        try { root.transform.localScale = scale; PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Topple(Transform t, Vector3 position, Vector3 rotation)
    {
        t.localPosition = position;
        t.localRotation = Quaternion.Euler(rotation);
        var renderers = t.GetComponentsInChildren<Renderer>();
        float bottom = renderers.Min(r => r.bounds.min.y);
        t.position += Vector3.up * (t.parent.position.y - bottom + .015f);
    }

    static Transform Group(string name, Vector3 position, float yaw = 0)
    {
        var t = new GameObject(name).transform;
        t.SetParent(details, false); t.localPosition = position;
        t.localRotation = Quaternion.Euler(0, yaw, 0);
        return t;
    }
    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, string material, Vector3? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.transform.localScale = size;
        if (rotation.HasValue) go.transform.localRotation = Quaternion.Euler(rotation.Value);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = materials[material];
        return go;
    }
    static void Beam(Transform p, string name, Vector3 a, Vector3 b, float thickness, string material)
    {
        var go = Box(p, name, (a + b) * .5f, V(thickness, (b - a).magnitude, thickness), material);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }
    static void Label(Transform parent, string text, Vector3 position, float size, string material)
    {
        var t = new GameObject(text.Replace('\n', ' ')).transform;
        t.SetParent(parent, false); t.localPosition = position;
        var label = t.gameObject.AddComponent<TextMesh>();
        label.text = text; label.fontSize = 64; label.characterSize = size * 10f / 64f;
        label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        label.color = materials[material].GetColor("_BaseColor");
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
    }

    static void University(Transform root)
    {
        var motto = root.Find("School motto").GetComponentInChildren<TextMesh>();
        motto.text = "FACULDADE DE ENGENHARIA  /  BLOCO C";
        motto.characterSize = .035f;
        var board = Group("Last university lecture and warning", V(.5f, 2.0f, 4.79f));
        Label(board, "FÍSICA II  /  AULA 06\n∇ × E = -∂B/∂t\nω² = k / m", V(-1.30f, .14f, -.02f), .15f, "Chalk");
        Label(board, "NÃO ABRAM\nA PORTA", V(.95f, -.20f, -.035f), .23f, "Blood");
        for (int i = 0; i < 6; i++)
            Beam(board, "Frantic tally mark", V(-2.1f + i * .10f, -.42f, -.021f), V(-2.08f + i * .10f, -.20f, -.021f), .014f, "Chalk");
        var notice = Group("University closure notice", V(7.1f, 2.12f, 4.84f));
        Box(notice, "Notice board", Vector3.zero, V(2.0f, 1.20f, .065f), "Wood");
        for (int i = 0; i < 5; i++)
            Box(notice, "Cancelled classes", V(-.75f + i * .36f, R(-.13f, .13f), -.041f), V(.30f, .48f, .012f), "Paper", V(0, 0, R(-10, 10)));
        Label(notice, "CAMPUS INTERDITADO\nEVACUAÇÃO IMEDIATA", V(0, .34f, -.055f), .15f, "Black");
        var lectern = Group("Abandoned lecturer workstation", V(9.0f, 0, 3.94f), -10);
        Box(lectern, "Lectern base", V(0, .06f, 0), V(.85f, .12f, .62f), "Iron");
        Box(lectern, "Lectern pedestal", V(0, .59f, .07f), V(.58f, 1.04f, .43f), "Wood");
        Box(lectern, "Angled lectern top", V(0, 1.16f, 0), V(1.0f, .075f, .75f), "Wood", V(12, 0, 0));
        Box(lectern, "Old monitor case", V(.08f, 1.49f, .13f), V(.58f, .45f, .43f), "Steel");
        Box(lectern, "Dead CRT screen", V(.08f, 1.49f, -.091f), V(.47f, .32f, .009f), "Black");
        Box(lectern, "Keyboard", V(0, 1.22f, -.23f), V(.46f, .04f, .17f), "Iron", V(12, 0, 0));
        for (int i = 0; i < 8; i++)
            Box(lectern, "Keyboard key", V(-.18f + i * .05f, 1.255f, -.23f), V(.034f, .008f, .08f), "Dust", V(12, 0, 0));
        Beam(lectern, "Trailing severed monitor cable", V(.20f, 1.36f, .32f), V(.44f, .055f, .44f), .018f, "Black");
        var c = lectern.gameObject.AddComponent<BoxCollider>(); c.center = V(0, .60f, 0); c.size = V(.86f, 1.20f, .65f);

        var projector = Group("Broken overhead projector", V(6.0f, 3.08f, -.3f), 12);
        Beam(projector, "Projector ceiling mount", V(0, .58f, 0), V(0, .05f, 0), .06f, "Iron");
        Box(projector, "Projector housing", Vector3.zero, V(.65f, .24f, .46f), "Steel", V(0, 0, 9));
        Box(projector, "Black projection lens", V(0, -.02f, .25f), V(.19f, .13f, .10f), "Black");
        for (int i = 0; i < 7; i++) Box(projector, "Projector ventilation grille", V(.34f, 0, -.14f + i * .045f), V(.006f, .11f, .014f), "Black");
        var screen = Group("Torn fallen projection screen", V(11.80f, 1.7f, -1.25f), -90);
        Beam(screen, "Bent screen roller", V(-1.22f, .84f, 0), V(1.23f, .75f, 0), .08f, "Steel");
        for (int i = 0; i < 12; i++)
        {
            float h = i >= 4 && i <= 6 ? R(.45f, .87f) : R(1.14f, 1.46f);
            Box(screen, "Torn yellowed canvas strip", V(-1.11f + i * .20f, .75f - h / 2, -.06f), V(.195f, h, .013f), "Canvas");
        }
        for (int row = 0; row < 7; row += 2)
        {
            var desk = root.Find("Student desk " + row + "-" + (row % 6));
            var p = Group("University desk belongings " + row, desk.localPosition);
            p.localRotation = desk.localRotation;
            Box(p, "Closed abandoned laptop", V(.28f, 1.081f, .03f), V(.37f, .025f, .27f), "CorpseCoat");
            Box(p, "Dusty laptop stripe", V(.28f, 1.095f, .03f), V(.28f, .004f, .006f), "Dust");
            Box(p, "Coffee cup", V(.52f, 1.13f, -.21f), V(.075f, .12f, .075f), "Paper");
            Box(p, "Cold coffee", V(.52f, 1.193f, -.21f), V(.061f, .004f, .061f), "Black");
            Box(p, "Technical manual", V(-.30f, 1.091f, -.12f), V(.30f, .055f, .37f), "BookBlue");
        }
        var bag = Group("Dropped university satchel", V(5.12f, .025f, -8.85f), 27);
        Box(bag, "Canvas satchel", V(0, .19f, 0), V(.45f, .36f, .25f), "CorpseCoat", V(0, 0, 12));
        Beam(bag, "Broken satchel strap", V(-.21f, .25f, 0), V(-.38f, .025f, -.27f), .031f, "Black");
        Box(bag, "Student notebook", V(.36f, .015f, -.18f), V(.30f, .025f, .39f), "BookRed", V(0, -28, 0));
    }

    static void Decay()
    {
        var grime = Group("Mildew water damage and blast scars", Vector3.zero);
        for (int i = 0; i < 40; i++)
        {
            float z = R(-10.6f, 4.5f), y = R(.12f, .85f);
            Patch(grime, V(11.875f, y, z), R(.10f, .35f), R(.12f, .50f), "Mildew", V(0, 0, 90));
            Box(grime, "Vertical leaking wall stain", V(11.867f, y + .45f, z), V(.008f, R(.3f, 1.2f), R(.015f, .09f)), "Mildew");
        }
        Patch(grime, V(11.872f, 2.53f, -4.7f), .79f, 1.16f, "Soot", V(0, 0, 90));
        for (int i = 0; i < 12; i++)
        {
            float z = -4.7f + R(-.65f, .65f), y = 2.53f + R(-.4f, .6f);
            Box(grime, "Buckshot crater", V(11.862f, y, z), V(.012f, .065f, .075f), "Black", V(R(-30, 30), 0, 0));
            Box(grime, "Chipped plaster at impact", V(11.853f, y - .045f, z), V(.015f, .025f, .10f), "Rubble");
        }
        var debris = Group("Splintered ceiling and abandoned barricade supplies", Vector3.zero);
        for (int i = 0; i < 32; i++)
        {
            float x = i < 16 ? R(9.7f, 11.4f) : R(-5.1f, -4.9f), z = R(-10, 1.9f);
            Box(debris, "Broken ceiling tile fragment", V(x, R(.025f, .10f), z), V(R(.12f, .6f), .025f, R(.15f, .45f)), "Ceiling", V(R(-12, 12), R(0, 180), R(-12, 12)));
            if (i % 3 == 0) Box(debris, "Splintered barricade timber", V(x + .15f, .06f, z), V(.11f, .06f, .8f), "Wood", V(0, R(0, 180), 0));
        }
        var junction = Group("Burned open electrical junction", V(11.82f, 2.08f, -4.7f), -90);
        Box(junction, "Junction box", Vector3.zero, V(.55f, .70f, .14f), "Iron");
        Box(junction, "Scorched circuit board", V(0, 0, -.085f), V(.46f, .61f, .03f), "Black");
        Box(junction, "Hanging open steel lid", V(.46f, -.12f, -.07f), V(.50f, .64f, .04f), "Steel", V(0, -67, 18));
        for (int i = 0; i < 4; i++)
        {
            Beam(junction, "Loose exposed cable", V(-.17f + i * .10f, .24f, -.11f), V(-.12f + i * .10f, -.65f - i * .11f, -.2f), .018f, i % 2 == 0 ? "Blood" : "Black");
            Box(junction, "Fuse", V(-.17f + i * .10f, .12f, -.12f), V(.055f, .13f, .06f), "Paper");
        }
    }

    static void Aftermath()
    {
        var floor = Group("Blood pools drag marks and bloody footsteps", Vector3.zero);
        Patch(floor, V(7.6f, .027f, 3.30f), 1.18f, .70f, "DriedBlood");
        Patch(floor, V(7.75f, .029f, 3.43f), .77f, .45f, "Blood");
        Patch(floor, V(.98f, .027f, -7.45f), .85f, .72f, "DriedBlood");
        Patch(floor, V(-3.10f, .027f, -4.18f), .98f, .60f, "DriedBlood");
        Patch(floor, V(-3.05f, .029f, -4.10f), .64f, .40f, "Blood");
        // Thin, interrupted skid marks tell a story without adding collision to the route.
        for (int i = 0; i < 24; i++)
        {
            float z = -8.6f + i * .46f, x = 2.05f + Mathf.Sin(i * .26f) * .55f;
            for (int side = -1; side <= 1; side += 2)
                Patch(floor, V(x + side * .19f, .026f, z), R(.025f, .085f), R(.12f, .30f), "DriedBlood");
            if (i % 2 == 0) Patch(floor, V(x + R(-.40f, .40f), .028f, z + .08f), R(.035f, .10f), R(.045f, .14f), "Blood");
        }
        for (int i = 0; i < 13; i++)
        {
            var step = Group("Bloody boot print " + i, V(3.25f + (i % 2 == 0 ? -.16f : .16f), .031f, -9.0f + i * .68f), R(-9, 9));
            Patch(step, V(0, 0, -.085f), .081f, .063f, "DriedBlood");
            for (int tread = 0; tread < 3; tread++) Box(step, "Boot sole tread", V(0, 0, .025f + tread * .041f), V(.15f, .002f, .028f), "DriedBlood");
        }
        var wall = Group("Blood spray and desperate handprints", Vector3.zero);
        Patch(wall, V(7.5f, 1.42f, 4.871f), .65f, .48f, "DriedBlood", V(-90, 0, 0));
        for (int i = 0; i < 37; i++)
        {
            float x = 7.5f + R(-1.15f, 1.15f), y = 1.42f + R(-.70f, .90f);
            Patch(wall, V(x, y, 4.860f), R(.015f, .063f), R(.015f, .055f), "Blood", V(-90, 0, 0));
            if (i % 4 == 0) Box(wall, "Dried blood drip", V(x, y - .17f, 4.858f), V(.018f, R(.18f, .43f), .006f), "DriedBlood");
        }
        Handprint(wall, V(-3.27f, 1.63f, 4.865f));
        Handprint(wall, V(-3.18f, 1.08f, 4.863f));
        var warning = Group("Desperate wall warning", V(-.30f, 1.42f, -10.868f), 180);
        Label(warning, "ELES AINDA\nESTÃO AQUI", Vector3.zero, .28f, "Blood");
        Corpse("Faceless prone victim near windows", V(-3.05f, .05f, -4.08f), 64, false);
        Corpse("Covered adult body at lecture wall", V(7.70f, .05f, 3.46f), 82, true);
        Corpse("Faceless victim behind toppled locker", V(1.03f, .05f, -7.50f), -20, false);
        var tape = Group("Torn emergency quarantine tape", V(11.73f, 1.42f, -8.8f), -90);
        Box(tape, "Sagging quarantine tape", Vector3.zero, V(2.0f, .10f, .008f), "Caution", V(0, 0, -12));
        for (int i = 0; i < 10; i++) Box(tape, "Black hazard stripe", V(-.92f + i * .20f, .18f - i * .038f, -.009f), V(.055f, .105f, .005f), "Black", V(0, 0, -38));
        Label(tape, "ISOLAMENTO", V(0, .27f, -.021f), .12f, "Caution");
    }

    static void Handprint(Transform parent, Vector3 p)
    {
        Patch(parent, p, .060f, .077f, "Blood", V(-90, 0, 0));
        for (int finger = 0; finger < 4; finger++)
            Box(parent, "Smeared handprint finger", p + V(-.048f + finger * .031f, .083f + (finger == 0 ? -.025f : 0), -.002f), V(.018f, .090f + finger * .006f, .006f), "Blood", V(0, 0, (finger - 1) * -8));
        Beam(parent, "Handprint thumb", p + V(-.065f, -.012f, -.002f), p + V(-.105f, .053f, -.002f), .024f, "Blood");
        for (int i = 0; i < 3; i++) Box(parent, "Hand dragged down wall", p + V(-.032f + i * .033f, -.15f, -.003f), V(.017f, .22f, .006f), "DriedBlood");
    }

    static void Corpse(string name, Vector3 position, float yaw, bool covered)
    {
        var p = Group(name, position, yaw);
        // Faceted adult silhouette. Head is entirely covered; no facial geometry or texture.
        Ellipsoid(p, "Coat torso", V(0, .16f, .12f), V(.56f, .32f, .78f), "CorpseCoat");
        Ellipsoid(p, "Trouser hips", V(0, .135f, -.37f), V(.47f, .28f, .32f), "CorpseTrousers");
        Ellipsoid(p, "Covered neck and coat collar", V(.01f, .15f, .54f), V(.18f, .22f, .24f), "CoveredFace");
        Ellipsoid(p, "Head with opaque cloth covering", V(.015f, .155f, .72f), V(.255f, .27f, .32f), "CoveredFace");
        Box(p, "Coat front seam", V(0, .321f, .12f), V(.018f, .010f, .52f), "Iron");
        Box(p, "Trouser belt", V(0, .262f, -.36f), V(.35f, .018f, .075f), "Black");
        for (int side = -1; side <= 1; side += 2)
        {
            var upperLeg = Ellipsoid(p, "Trouser thigh", V(side * .15f, .13f, -.70f), V(.21f, .24f, .53f), "CorpseTrousers");
            upperLeg.transform.localRotation = Quaternion.Euler(0, side * -9, 0);
            Ellipsoid(p, "Trouser lower leg", V(side * .21f, .105f, -1.12f), V(.17f, .21f, .47f), "CorpseTrousers");
            Box(p, "Worn boot", V(side * .21f, .085f, -1.43f), V(.19f, .16f, .29f), "Black", V(0, side * 9, 0));
            var sleeve = Ellipsoid(p, "Coat sleeve", V(side * .36f, .125f, .22f), V(.22f, .23f, .54f), "CorpseCoat");
            sleeve.transform.localRotation = Quaternion.Euler(0, side * -26, 0);
            Ellipsoid(p, "Motionless gloved hand", V(side * .49f, .08f, -.07f), V(.15f, .13f, .24f), "CoveredFace");
        }
        if (covered)
        {
            // A coarse drape follows shoulders, knees and feet, including the head.
            float[] z = { -1.61f, -1.25f, -.8f, -.32f, .15f, .57f, .94f };
            float[] height = { .08f, .27f, .28f, .34f, .40f, .34f, .12f };
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int row = 0; row < z.Length; row++)
                for (int col = 0; col < 5; col++)
                {
                    float x = (col - 2) * .215f;
                    vertices.Add(V(x, col == 0 || col == 4 ? .012f : height[row] - (col == 2 ? 0 : .035f), z[row]));
                    uv.Add(new Vector2(col / 4f, row / 6f));
                }
            for (int row = 0; row < 6; row++) for (int col = 0; col < 4; col++)
            {
                int a = row * 5 + col;
                triangles.AddRange(new[] { a, a + 5, a + 1, a + 1, a + 5, a + 6 });
            }
            MeshObject(p, "Opaque faceted mortuary sheet", vertices.ToArray(), uv.ToArray(), triangles.ToArray(), "Tarp");
            Box(p, "Blood soaked sheet edge", V(.40f, .024f, .1f), V(.055f, .014f, .62f), "DriedBlood");
        }
        // Low decoration remains walkable: no colliders on bodies, stains or fragments.
    }

    static GameObject Ellipsoid(Transform p, string name, Vector3 position, Vector3 size, string material)
    {
        const int rings = 5, sides = 8;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        for (int ring = 0; ring <= rings; ring++) for (int side = 0; side <= sides; side++)
        {
            float latitude = Mathf.PI * ring / rings, a = Mathf.PI * 2 * side / sides;
            vertices.Add(V(Mathf.Sin(latitude) * Mathf.Cos(a) * size.x / 2, Mathf.Cos(latitude) * size.y / 2, Mathf.Sin(latitude) * Mathf.Sin(a) * size.z / 2));
            uv.Add(new Vector2(side / (float)sides, ring / (float)rings));
        }
        for (int ring = 0; ring < rings; ring++) for (int side = 0; side < sides; side++)
        {
            int a = ring * (sides + 1) + side;
            indices.AddRange(new[] { a, a + 1, a + sides + 1, a + 1, a + sides + 2, a + sides + 1 });
        }
        var go = MeshObject(p, name, vertices.ToArray(), uv.ToArray(), indices.ToArray(), material);
        go.transform.localPosition = position; return go;
    }
    static void Patch(Transform p, Vector3 position, float width, float depth, string material, Vector3? rotation = null)
    {
        const int sides = 20;
        var vertices = new Vector3[sides + 1]; var uv = new Vector2[sides + 1]; var indices = new int[sides * 3];
        uv[0] = new Vector2(.5f, .5f);
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, r = R(.64f, 1);
            vertices[i + 1] = V(Mathf.Cos(a) * width * r, 0, Mathf.Sin(a) * depth * r);
            uv[i + 1] = new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f);
            indices[i * 3] = 0; indices[i * 3 + 1] = (i + 1) % sides + 1; indices[i * 3 + 2] = i + 1;
        }
        var go = MeshObject(p, "Irregular " + material + " stain", vertices, uv, indices, material);
        go.transform.localPosition = position;
        if (rotation.HasValue) go.transform.localRotation = Quaternion.Euler(rotation.Value);
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }
    static GameObject MeshObject(Transform p, string name, Vector3[] vertices, Vector2[] uv, int[] indices, string material)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject(name); go.transform.SetParent(p, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = materials[material]; return go;
    }

    static void Lighting(Transform root)
    {
        foreach (var light in root.GetComponentsInChildren<Light>())
        {
            if (light.name.StartsWith("Fluorescent light "))
            {
                int index = int.Parse(light.name.Substring("Fluorescent light ".Length));
                light.intensity = index == 0 || index == 3 || index == 7 ? 2.0f : index == 4 ? .48f : .0f;
                light.color = new Color(1, .78f, .49f);
            }
            else if (light.name.StartsWith("Cold window")) light.intensity = .65f;
            else if (light.name == "Dim room bounce") light.intensity = .48f;
            else if (light.name == "Red exit spill") { light.intensity = 2.8f; light.range = 4.8f; }
        }
        foreach (int index in new[] { 2, 5, 6, 8 })
        {
            var fixture = root.Find("Suspended fluorescent fixture " + index);
            foreach (var renderer in fixture.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterial == materials["Tube"]) renderer.sharedMaterial = materials["Black"];
        }
        var emergency = Group("Failing red quarantine beacon", V(10.80f, 2.90f, 4.55f));
        Box(emergency, "Beacon rusted mount", Vector3.zero, V(.30f, .18f, .12f), "Iron");
        Box(emergency, "Red emergency beacon", V(0, 0, -.09f), V(.20f, .11f, .08f), "ExitRed");
        var lightObject = new GameObject("Red quarantine light"); lightObject.transform.SetParent(emergency, false);
        lightObject.transform.localPosition = V(0, -.2f, -.32f);
        var beacon = lightObject.AddComponent<Light>(); beacon.type = LightType.Point;
        beacon.color = new Color(1, .06f, .035f); beacon.intensity = 2.3f; beacon.range = 5.6f;
        beacon.shadows = LightShadows.None;
        FloorBounce("Emergency lamp floor reflection", V(7.0f, 1.20f, 3.08f), new Color(1, .65f, .40f), 1.65f, 4.0f);
        FloorBounce("Surviving fixture floor reflection", V(1.15f, 2.35f, -7.8f), new Color(.88f, .72f, .51f), 1.5f, 4.4f);
    }

    static void FloorBounce(string name, Vector3 position, Color color, float intensity, float range)
    {
        var t = Group(name, position);
        var light = t.gameObject.AddComponent<Light>(); light.type = LightType.Point;
        light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
    }

    static void Combine(Transform group)
    {
        var renderers = group.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<MeshFilter>() && !r.GetComponent<TextMesh>()).ToArray();
        foreach (var bucket in renderers.GroupBy(r => r.sharedMaterial))
        {
            var mesh = new Mesh { name = "Horror_" + group.name + "_" + bucket.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(bucket.Select(r => new CombineInstance { mesh = r.GetComponent<MeshFilter>().sharedMesh, transform = group.worldToLocalMatrix * r.transform.localToWorldMatrix }).ToArray(), true, true);
            string safe = string.Concat(mesh.name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            string path = Art + "/Meshes/" + safe + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(bucket.Key.name); go.transform.SetParent(group, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = bucket.Key;
            if (bucket.All(r => r.shadowCastingMode == ShadowCastingMode.Off)) renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        foreach (var renderer in renderers)
        {
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            if (!AssetDatabase.Contains(mesh)) Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(renderer.gameObject);
        }
    }
}
