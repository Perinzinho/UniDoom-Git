using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Pixel textures, persistent meshes and local lighting; no global gameplay settings.
public static class BasementFinalArt
{
    const string Art = "Assets/Art/Basement";
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Transform root;
    static System.Random random;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
    public static void Build()
    {
        mats.Clear(); random = new System.Random(66609);
        foreach(var path in Directory.GetFiles(Art + "/Materials", "*.mat"))
        { var mat = AssetDatabase.LoadAssetAtPath<Material>(path); mats[mat.name] = mat; }
        Texture("FinalFabric", false); Texture("FinalCorruption", true);
        Surface("FinalBlood", "FinalFabric", new Color(.65f, .023f, .032f), .38f);
        Surface("FinalDriedBlood", "FinalFabric", new Color(.35f, .015f, .025f), .08f);
        Surface("FinalFlesh", "FinalCorruption", new Color(.55f, .22f, .19f), .22f);
        Surface("FinalBone", "FinalFabric", new Color(.72f, .67f, .48f), .02f);
        Surface("FinalShroud", "FinalFabric", new Color(.62f, .60f, .48f), .02f);
        Surface("FinalCloth", "FinalFabric", new Color(.28f, .34f, .31f), .02f);
        Surface("FinalSoot", "FinalFabric", new Color(.027f, .025f, .025f), .01f);
        Surface("FinalMold", "FinalFabric", new Color(.13f, .18f, .11f), .01f);
        foreach(bool boss in new[] { false, true })
        {
            root = new GameObject(boss ? "BasementFinalSanctum" : "BasementFinalArena").transform;
            if(boss) Sanctum(); else Arena();
            foreach(Transform group in root.Cast<Transform>().ToArray()) Combine(group);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, Art + "/" + root.name + ".prefab");
            Object.DestroyImmediate(root.gameObject);
        }
        AssetDatabase.SaveAssets();
    }
    static Transform Group(string name, Vector3 p, float yaw = 0)
    { var t = new GameObject(name).transform; t.SetParent(root, false); t.localPosition = p; t.localRotation = Quaternion.Euler(0, yaw, 0); return t; }
    static GameObject Box(Transform p, string name, Vector3 position, Vector3 size, string mat, Vector3? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(p, false); go.transform.localPosition = position; go.transform.localScale = size;
        if(rotation.HasValue) go.transform.localRotation = Quaternion.Euler(rotation.Value);
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mats[mat]; return go;
    }
    static void Beam(Transform p, string name, Vector3 a, Vector3 b, float width, string mat)
    { var go = Box(p, name, (a + b) / 2, V(width, (b - a).magnitude, width), mat); go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); }
    static void Collision(Transform p, Vector3 center, Vector3 size)
    { var c = p.gameObject.AddComponent<BoxCollider>(); c.center = center; c.size = size; }
    static void Light(Transform p, Vector3 position, Color color, float intensity, float range, bool shadow = false)
    {
        var t = new GameObject("Local final-area light").transform; t.SetParent(p, false); t.localPosition = position;
        var l = t.gameObject.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
        l.shadows = shadow ? LightShadows.Soft : LightShadows.None; l.shadowBias = .025f;
    }
    static void Label(Transform p, string words, Vector3 pos, float size, string mat)
    {
        var go = new GameObject(words.Replace('\n', ' ')); go.transform.SetParent(p, false); go.transform.localPosition = pos;
        var text = go.AddComponent<TextMesh>(); text.text = words; text.fontSize = 64; text.characterSize = size * 10 / 64;
        text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = mats[mat].GetColor("_BaseColor");
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
    }
    static void Arena()
    {
        var steel = Group("Rusted gantries pipes valves and warning plates", Vector3.zero);
        foreach(int side in new[] { -1, 1 })
        {
            for(int i = 0; i < 4; i++)
            {
                float z = -11 + i * 8;
                Beam(steel, "Tall structural wall brace", V(side * 19.45f, .10f, z), V(side * 19.45f, 8.9f, z), .20f, "Iron");
                Box(steel, "Wall brace base", V(side * 19.3f, .40f, z), V(.65f, .8f, .8f), "Rust");
                Beam(steel, "Wall diagonal steel truss", V(side * 19.3f, 7.9f, z), V(side * 18, 9.05f, z), .18f, "Iron");
                for(int bolt = 0; bolt < 3; bolt++) Box(steel, "Exposed rivet", V(side * 19.01f, .2f + bolt * .2f, z - .33f), V(.055f, .055f, .055f), "Steel");
            }
            for(int i = 0; i < 3; i++)
            {
                float z = -8 + i * 10;
                Beam(steel, "Pressure return pipe", V(side * 19.15f, 4.6f, z - 2.2f), V(side * 19.15f, 4.6f, z + 2.2f), .18f, "Rust");
                Beam(steel, "Pressure riser", V(side * 19.15f, 1.2f, z), V(side * 19.15f, 6.8f, z), .20f, "Rust");
                Ring(steel, V(side * 18.88f, 2.1f, z), .36f, .055f, "Iron", Quaternion.Euler(0, 0, 90));
                Beam(steel, "Valve crossbar", V(side * 18.87f, 1.8f, z), V(side * 18.87f, 2.4f, z), .065f, "Steel");
                Beam(steel, "Valve crossbar", V(side * 18.87f, 2.1f, z - .3f), V(side * 18.87f, 2.1f, z + .3f), .065f, "Steel");
            }
        }
        var overhead = Group("Sagging overhead services and maintenance gantry", Vector3.zero);
        for(int z = -8; z <= 16; z += 8)
        {
            Beam(overhead, "Roof cross truss", V(-19.4f, 8.9f, z), V(19.4f, 8.9f, z), .22f, "Iron");
            for(int x = -16; x < 16; x += 4) Beam(overhead, "Truss diagonal", V(x, 8.9f, z), V(x + 4, 9.25f, z), .10f, "Rust");
        }
        for(int i = 0; i < 7; i++) Beam(overhead, "Drooping power cable", V(-17 + i * 5, 8.8f, -13), V(-15 + i * 5, 7.7f, 17), .025f, "Iron");
        var duct = Group("Damaged extractor fan", V(19.65f, 5.2f, 3), -90);
        Box(duct, "Vent housing", Vector3.zero, V(2.5f, 2.5f, .25f), "Iron");
        Box(duct, "Dark vent recess", V(0, 0, -.16f), V(2.15f, 2.15f, .018f), "FinalSoot");
        for(int i = 0; i < 4; i++) Box(duct, "Broken fan blade", V(0, 0, -.20f), V(.24f, 1.82f, .035f), "Steel", V(0, 0, i * 45));
        for(int i = -4; i <= 4; i++) Box(duct, "Bent ventilation guard", V(i * .22f, 0, -.25f), V(.035f, 2.10f, .028f), "Rust", V(0, 0, i == 2 ? 12 : 0));

        var station = Group("Abandoned restraint station", V(-15.3f, 0, 10), 90);
        Box(station, "Steel mortuary slab", V(0, .98f, 0), V(1.1f, .12f, 2.75f), "Steel");
        for(int s = -1; s <= 1; s += 2) for(int e = -1; e <= 1; e += 2) Box(station, "Table support", V(s * .42f, .48f, e * 1.12f), V(.09f, .95f, .09f), "Iron");
        WrappedBody(station, V(0, 1.13f, 0), false);
        Collision(station, V(0, .5f, 0), V(1.1f, 1.1f, 2.75f));
        Light(station, V(0, 3.2f, 0), new Color(1, .68f, .42f), 3.5f, 6);
        var blood = Group("Arena massacre traces and stained drainage", Vector3.zero);
        foreach(int side in new[] { -1, 1 })
        {
            Patch(blood, V(side * 15.5f, .018f, side < 0 ? 10 : -5), 2.2f, 1.1f, "FinalDriedBlood");
            Patch(blood, V(side * 19.72f, 2.0f, 7), .90f, 1.4f, "FinalBlood", V(0, 0, side < 0 ? -90 : 90));
            for(int i = 0; i < 12; i++) Box(blood, "Dried vertical wall streak", V(side * 19.71f, R(.6f, 2.2f), 7 + R(-1.4f, 1.4f)), V(.012f, R(.4f, 1.0f), .028f), "FinalDriedBlood");
        }
        for(int i = 0; i < 34; i++) Patch(blood, V(3.1f + R(-.5f, .5f), .020f, -10 + i * .76f), R(.05f, .14f), R(.12f, .32f), "FinalDriedBlood");
        var corruption = Group("Organic invasion of the pumping system", V(17.8f, .05f, 8));
        for(int i = 0; i < 8; i++)
        {
            Lump(corruption, "Corrupted fleshy pipe growth", V(R(-.7f, .7f), R(.12f, .75f), R(-1.0f, 1.0f)), V(R(.5f, 1.0f), R(.3f, .8f), R(.7f, 1.2f)), "FinalFlesh");
            Beam(corruption, "Tendon merging into wall", V(0, .4f, 0), V(1.8f, 1.5f + i * .42f, R(-1, 1)), .07f, "FinalFlesh");
        }
        Light(corruption, V(-1.1f, 2, -.5f), new Color(1, .19f, .12f), 2.8f, 7);
        var sign = Group("Final sector evacuation graffiti", V(-6.1f, 3.0f, 18.73f));
        Label(sign, "NÃO HÁ SAÍDA\nABAIXO DESTE PONTO", Vector3.zero, .28f, "FinalBlood");
        Decay(false);
    }
    static void Sanctum()
    {
        var arch = Group("Colossal rib cage around the infernal portal", V(0, 0, 34.6f));
        for(int side = -1; side <= 1; side += 2)
        {
            for(int i = 0; i < 7; i++)
            {
                float y = 1.5f + i * 1.40f;
                Vector3 a = V(side * 7.9f, y, .25f), b = V(side * 6.6f, y + .50f, -.65f), c = V(side * 5.3f, y + .90f, -1.0f);
                Beam(arch, "Giant calcified rib", a, b, .40f, "FinalBone"); Beam(arch, "Curving giant rib tip", b, c, .28f, "FinalBone");
                Lump(arch, "Bloated spinal growth", a, V(.67f, 1.0f, .65f), "FinalFlesh");
            }
            Beam(arch, "Corrupted spinal column", V(side * 7.9f, 1, .25f), V(side * 7.9f, 11.0f, .25f), .65f, "FinalFlesh");
            for(int i = 0; i < 3; i++) Beam(arch, "Portal crown bone spike", V(side * (1.6f + i * 1.6f), 10.3f, .2f), V(side * (1.9f + i * 1.9f), 11.8f, -.35f), .23f, "FinalBone");
        }
        Light(arch, V(-6.7f, 4.2f, -2.2f), new Color(1, .23f, .08f), 7, 14, true);
        Light(arch, V(6.7f, 6.2f, -2.2f), new Color(1, .15f, .10f), 7, 14);

        var suspended = Group("Six shrouded bodies suspended above the altar", V(0, 0, 28.6f));
        for(int i = 0; i < 6; i++)
        {
            float x = -8.6f + i * 3.45f, y = 6.6f + (i % 2) * 1.3f;
            Chain(suspended, V(x, y + 1.2f, 0), V(x, 11.7f, 0));
            WrappedBody(suspended, V(x, y, 0), true);
            Ring(suspended, V(x, y + .15f, 0), .30f, .055f, "Iron", Quaternion.identity);
        }
        Light(suspended, V(0, 8.7f, -3.5f), new Color(1, .70f, .45f), 7, 17, true);
        var ritual = Group("Blood pentagram bone heaps and sacrificial procession", V(0, .027f, 18.5f));
        for(int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2 / 5, b = (i + 2) * Mathf.PI * 2 / 5;
            Beam(ritual, "Blood ritual line", V(Mathf.Sin(a) * 6.0f, 0, Mathf.Cos(a) * 6.0f), V(Mathf.Sin(b) * 6.0f, 0, Mathf.Cos(b) * 6.0f), .11f, "FinalDriedBlood");
        }
        for(int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2 / 18;
            Skull(ritual, V(Mathf.Sin(a) * 8.7f, .08f, Mathf.Cos(a) * 8.7f));
        }
        var pools = Group("Blood flooded sacrificial gutters", Vector3.zero);
        for(int side = -1; side <= 1; side += 2)
        {
            Box(pools, "Steel gutter rim", V(side * 8.6f, .045f, 18.2f), V(.14f, .085f, 25), "Iron");
            Box(pools, "Coagulated blood in gutter", V(side * 8.15f, .012f, 18.2f), V(.75f, .010f, 25), "FinalBlood");
            for(int i = 0; i < 7; i++) Patch(pools, V(side * 8.0f, .023f, 8 + i * 3.3f), .8f, R(.3f, .8f), "FinalDriedBlood");
        }
        var victims = Group("Anonymous remains beside the ritual circle", V(-9.5f, .04f, 12.5f), 70);
        WrappedBody(victims, Vector3.zero, false); WrappedBody(victims, V(2.7f, 0, .4f), false);
        Patch(victims, V(.6f, -.018f, 0), 2.5f, 1.2f, "FinalBlood");
        var shrine = Group("Bone reliquaries and towering ritual machinery", Vector3.zero);
        foreach(int side in new[] { -1, 1 }) for(int i = 0; i < 3; i++)
        {
            float x = side * 17.4f, z = 7 + i * 10.6f;
            Box(shrine, "Reliquary plinth", V(x, .40f, z), V(1.6f, .8f, 1.6f), "Stone");
            Box(shrine, "Iron shrine pillar", V(x, 2.8f, z), V(1.0f, 4.0f, 1.0f), "Iron");
            for(int j = 0; j < 3; j++) Skull(shrine, V(x, 1.3f + j, z - .62f));
            Box(shrine, "Infernal beacon", V(x, 5.2f, z), V(.64f, .75f, .64f), "Red");
            for(int j = 0; j < 4; j++) Beam(shrine, "Shrine crown spike", V(x - .7f + j * .45f, 4.8f, z), V(x - .7f + j * .45f, 6.0f, z), .075f, "FinalBone");
            Light(shrine, V(x, 4.8f, z - 1.3f), new Color(1, .09f, .035f), 4.0f, 9.5f);
            Collision(shrine, V(x, 2.3f, z), V(1.6f, 4.6f, 1.6f));
        }
        var roof = Group("Cathedral foundry chains and vault bracing", Vector3.zero);
        for(int z = 6; z <= 30; z += 8)
        {
            Beam(roof, "Upper iron vault truss", V(-20.4f, 11.2f, z), V(20.4f, 11.2f, z), .24f, "Iron");
            for(int i = 0; i < 8; i++) Beam(roof, "Gothic roof diagonal", V(-20 + i * 5, 11.2f, z), V(-15 + i * 5, 11.85f, z), .13f, "Rust");
        }
        var wall = Group("Corrupted rear wall blood and crawling tendons", Vector3.zero);
        for(int side = -1; side <= 1; side += 2) for(int i = 0; i < 12; i++)
        {
            float x = side * R(9.2f, 18.2f), y = R(.35f, 6.7f);
            Patch(wall, V(x, y, 35.73f), R(.12f, .50f), R(.24f, .80f), "FinalDriedBlood", V(-90, 0, 0));
            Beam(wall, "Organic vein on stone", V(side * 9, .2f, 35.58f), V(x, y, 35.55f), R(.06f, .14f), "FinalFlesh");
        }
        var message = Group("Last warning at the threshold", V(-5.4f, 2.6f, .28f), 180);
        Label(message, "ABANDONE TODA\nESPERANÇA", Vector3.zero, .43f, "FinalBlood");
        Decay(true);
        var fill = Group("Dim warm arena readability", Vector3.zero);
        Light(fill, V(0, 5.2f, 12), new Color(.84f, .70f, .49f), 2.4f, 22);
    }
    static void Decay(bool boss)
    {
        var dirt = Group("Soot mold rubble and lost industrial debris", Vector3.zero);
        float edge = boss ? 20.72f : 19.72f, front = boss ? 1 : -14, back = boss ? 35 : 18;
        for(int side = -1; side <= 1; side += 2) for(int i = 0; i < 28; i++)
        {
            float z = R(front, back);
            Patch(dirt, V(side * edge, R(.30f, 2.2f), z), R(.15f, .5f), R(.3f, .9f), "FinalMold", V(0, 0, side < 0 ? -90 : 90));
            Box(dirt, "Broken masonry debris", V(side * R(edge - 1.8f, edge - .4f), R(.03f, .12f), z), V(R(.13f, .6f), R(.07f, .22f), R(.13f, .6f)), "Stone", V(R(-12, 12), R(0, 180), R(-12, 12)));
        }
        for(int i = 0; i < 4; i++) Patch(dirt, V(edge - .003f, 4.8f, front + 5 + i * 8), .85f, 1.5f, "FinalSoot", V(0, 0, 90));
    }
    static void WrappedBody(Transform p, Vector3 position, bool hanging)
    {
        // Opaque head wrapping and clothed adult silhouette, without identifiable faces.
        Quaternion q = hanging ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
        Action<string, Vector3, Vector3, string> part = (name, pos, size, mat) =>
        { var go = Lump(p, name, position + q * pos, size, mat); go.transform.localRotation = q; };
        part("Shrouded torso", V(0, .18f, 0), V(.56f, .33f, .79f), "FinalCloth");
        part("Opaque wrapped head", V(0, .15f, .56f), V(.27f, .28f, .34f), "FinalShroud");
        part("Covered neck", V(0, .16f, .36f), V(.18f, .23f, .21f), "FinalShroud");
        part("Clothed hips", V(0, .15f, -.46f), V(.45f, .30f, .27f), "FinalCloth");
        foreach(int side in new[] { -1, 1 })
        {
            part("Clothed arm", V(side * .32f, .13f, -.03f), V(.19f, .22f, .77f), "FinalCloth");
            part("Wrapped hand", V(side * .32f, .1f, -.48f), V(.15f, .17f, .2f), "FinalShroud");
            part("Clothed leg", V(side * .14f, .12f, -.94f), V(.21f, .23f, .95f), "FinalCloth");
            part("Boot", V(side * .14f, .11f, -1.44f), V(.22f, .22f, .3f), "Iron");
        }
        foreach(float z in new[] { -.18f, -.6f, -1.05f })
        { var go = Box(p, "Restraining iron band", position + q * V(0, .34f, z), V(.61f, .035f, .09f), "Iron"); go.transform.localRotation = q; }
    }
    static void Skull(Transform p, Vector3 position)
    {
        Lump(p, "Anonymous calcified skull", position + V(0, .17f, 0), V(.29f, .33f, .29f), "FinalBone");
        foreach(int side in new[] { -1, 1 }) Box(p, "Dark skull socket", position + V(side * .065f, .20f, -.132f), V(.078f, .063f, .020f), "FinalSoot");
        Box(p, "Broken skull jaw", position + V(0, .032f, -.055f), V(.20f, .06f, .18f), "FinalBone");
        for(int i = 0; i < 4; i++) Box(p, "Chipped tooth", position + V(-.075f + i * .05f, .07f, -.15f), V(.025f, .045f, .022f), "FinalBone");
    }
    static void Chain(Transform p, Vector3 a, Vector3 b)
    { int count = Mathf.CeilToInt((b - a).magnitude / .18f); for(int i = 0; i <= count; i++) Ring(p, Vector3.Lerp(a, b, i / (float)count), .082f, .023f, "Iron", Quaternion.Euler(90, i % 2 * 90, 0)); }
    static void Ring(Transform p, Vector3 center, float radius, float width, string mat, Quaternion rotation)
    {
        for(int i = 0; i < 12; i++)
        { float a = i * Mathf.PI * 2 / 12, b = (i + 1) * Mathf.PI * 2 / 12; Beam(p, "Faceted " + mat + " ring", center + rotation * V(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius), center + rotation * V(Mathf.Cos(b) * radius, 0, Mathf.Sin(b) * radius), width, mat); }
    }
    static GameObject Lump(Transform p, string name, Vector3 pos, Vector3 size, string mat)
    {
        const int rings = 5, sides = 8; var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        for(int r = 0; r <= rings; r++) for(int s = 0; s <= sides; s++)
        { float a = s * Mathf.PI * 2 / sides, b = r * Mathf.PI / rings; vertices.Add(V(Mathf.Sin(b) * Mathf.Cos(a) * size.x / 2, Mathf.Cos(b) * size.y / 2, Mathf.Sin(b) * Mathf.Sin(a) * size.z / 2)); uv.Add(new Vector2(s / (float)sides, r / (float)rings)); }
        for(int r = 0; r < rings; r++) for(int s = 0; s < sides; s++)
        { int a = r * (sides + 1) + s; indices.AddRange(new[] { a, a + 1, a + sides + 1, a + 1, a + sides + 2, a + sides + 1 }); }
        var go = MeshObject(p, name, vertices.ToArray(), uv.ToArray(), indices.ToArray(), mat); go.transform.localPosition = pos; return go;
    }
    static void Patch(Transform p, Vector3 pos, float width, float depth, string mat, Vector3? rot = null)
    {
        const int sides = 24; var v = new Vector3[sides + 1]; var uv = new Vector2[sides + 1]; var indices = new int[sides * 3]; uv[0] = new Vector2(.5f, .5f);
        for(int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2 / sides, r = R(.64f, 1); v[i + 1] = V(Mathf.Cos(a) * width * r, 0, Mathf.Sin(a) * depth * r); uv[i + 1] = new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f); indices[i * 3] = 0; indices[i * 3 + 1] = (i + 1) % sides + 1; indices[i * 3 + 2] = i + 1; }
        var go = MeshObject(p, "Irregular " + mat + " stain", v, uv, indices, mat); go.transform.localPosition = pos; if(rot.HasValue) go.transform.localRotation = Quaternion.Euler(rot.Value);
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }
    static GameObject MeshObject(Transform p, string name, Vector3[] v, Vector2[] uv, int[] indices, string mat)
    { var mesh = new Mesh { name = name }; mesh.vertices = v; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); var go = new GameObject(name); go.transform.SetParent(p, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mats[mat]; return go; }
    static void Combine(Transform group)
    {
        var renderers = group.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<MeshFilter>() && !r.GetComponent<TextMesh>()).ToArray();
        foreach(var bucket in renderers.GroupBy(r => r.sharedMaterial))
        {
            var mesh = new Mesh { name = "Final_" + root.name + "_" + group.name + "_" + bucket.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(bucket.Select(r => new CombineInstance { mesh = r.GetComponent<MeshFilter>().sharedMesh, transform = group.worldToLocalMatrix * r.transform.localToWorldMatrix }).ToArray(), true, true);
            string safe = string.Concat(mesh.name.Select(c => char.IsLetterOrDigit(c) ? c : '_')), path = Art + "/Meshes/" + safe + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; } else AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(bucket.Key.name); go.transform.SetParent(group, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = bucket.Key; if(bucket.All(x => x.shadowCastingMode == ShadowCastingMode.Off)) r.shadowCastingMode = ShadowCastingMode.Off;
        }
        foreach(var r in renderers) { var mesh = r.GetComponent<MeshFilter>().sharedMesh; if(!AssetDatabase.Contains(mesh)) Object.DestroyImmediate(mesh); Object.DestroyImmediate(r.gameObject); }
    }
    static void Texture(string name, bool organic)
    {
        var texture = new Texture2D(128, 128, TextureFormat.RGB24, false); var noise = new System.Random(organic ? 669 : 778);
        for(int y = 0; y < 128; y += 4) for(int x = 0; x < 128; x += 4)
        {
            float value = .64f + (float)noise.NextDouble() * .34f;
            if(organic && (x / 4 + y / 8) % 7 < 2) value *= .54f;
            for(int yy = 0; yy < 4; yy++) for(int xx = 0; xx < 4; xx++) texture.SetPixel(x + xx, y + yy, new Color(value, value, value));
        }
        texture.Apply(); string path = Art + "/Textures/" + name + ".png"; File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.filterMode = FilterMode.Point; importer.maxTextureSize = 128; importer.anisoLevel = 1; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
    }
    static void Surface(string name, string texture, Color color, float smoothness)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var created = new Material(mats["Bone"]) { name = name }; created.SetColor("_BaseColor", color); created.SetFloat("_Smoothness", smoothness); created.SetFloat("_Metallic", 0); created.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/" + texture + ".png")); created.SetTextureScale("_BaseMap", Vector2.one);
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if(existing) { EditorUtility.CopySerialized(created, existing); Object.DestroyImmediate(created); } else { AssetDatabase.CreateAsset(created, path); existing = created; } mats[name] = existing;
    }
}
