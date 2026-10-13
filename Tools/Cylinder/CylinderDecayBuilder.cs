// Apply to the current Cylinder in an isolated Unity project; retain existing geometry and GUIDs.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class CylinderDecayBuilder
{
    const string Art = "Assets/Art/Cylinder", RoomPath = "Assets/prefabs/Scenario/Cylinder.prefab";
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static System.Random random;
    static Transform details, atrium;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
    static Vector3 Oval(float a, float x, float z, float y = 0) => V(Mathf.Sin(a) * x, y, Mathf.Cos(a) * z);

    public static void Build()
    {
        if(!Application.dataPath.Replace('\\', '/').Contains("/.utmp/")) throw new Exception("Generate in an isolated project");
        mats.Clear(); random = new System.Random(1709);
        foreach(string path in Directory.GetFiles(Art + "/Materials", "*.mat"))
            mats[Path.GetFileNameWithoutExtension(path)] = AssetDatabase.LoadAssetAtPath<Material>(path);
        DoorTextures(); FabricTexture();
        Surface("DecayBlood", new Color(.55f, .023f, .035f), .26f);
        Surface("DecayMold", new Color(.15f, .21f, .12f), .01f);
        Surface("DecayChitin", new Color(.53f, .52f, .38f), .04f);
        Surface("DecayOrganic", new Color(.31f, .19f, .13f), .08f);
        Surface("DecayDust", new Color(.43f, .38f, .27f), .01f);
        Surface("DecayPaper", new Color(.77f, .71f, .54f), .01f);
        foreach(string ink in new[] { "DecayLettering", "DecayLetteringBlood" })
        {
            var created = new Material(mats[ink.EndsWith("Blood") ? "DecayBlood" : "DecayPaper"]) { name = ink };
            created.shader = Shader.Find("Universal Render Pipeline/Unlit");
            created.SetColor("_BaseColor", ink.EndsWith("Blood") ? new Color(.62f, .037f, .041f) : new Color(.92f, .82f, .60f));
            SaveMaterial(Art + "/Materials/" + ink + ".mat", created, ink);
        }
        var room = PrefabUtility.LoadPrefabContents(RoomPath);
        try
        {
            atrium = room.transform.Find("Abandoned circular atrium");
            foreach(Transform bay in atrium)
            {
                if(!bay.name.StartsWith("Service bay ")) continue;
                int index = int.Parse(bay.name.Substring("Service bay ".Length));
                foreach(var renderer in bay.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial && renderer.sharedMaterial.name.StartsWith("SealedDoor"))
                        renderer.sharedMaterial = mats[new[] { "SealedDoor", "SealedDoorPanel", "SealedDoorSlab" }[index % 3]];
            }
            foreach(var label in atrium.GetComponentsInChildren<TextMesh>())
                if(label.text == "CANTEEN 09") label.text = "CANTINA 09";
            Lighting();
            var previous = atrium.Find("Timeworn atrium stories"); if(previous) Object.DestroyImmediate(previous.gameObject);
            details = new GameObject("Timeworn atrium stories").transform; details.SetParent(atrium, false);
            WorldLettering();
            Canteen(); ServiceStories(); CreatureTraces(); Weathering();
            foreach(Transform group in details.Cast<Transform>().ToArray()) Combine(group);
            PrefabUtility.SaveAsPrefabAsset(room, RoomPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(room); }
        AssetDatabase.SaveAssets(); CylinderDecayVerifier.Verify();
        Debug.Log("CYLINDER_DECAY_BUILD_OK");
    }
    static void DoorTextures()
    {
        string[] names = { "SealedDoor", "SealedDoorPanel", "SealedDoorSlab" };
        for(int variant = 0; variant < names.Length; variant++)
        {
            string name = names[variant]; var texture = new Texture2D(128, 256, TextureFormat.RGB24, false);
            var noise = new System.Random(491 + variant);
            for(int y = 0; y < 256; y++) for(int x = 0; x < 128; x++)
            {
                bool frame = x < 6 || x > 121 || y < 6 || y > 249;
                bool seam = variant == 0 ? x > 61 && x < 65 : variant == 1 ? (x == 18 || x == 108) && y > 22 && y < 233 : x == 64;
                bool vent = variant == 0 && y > 187 && y < 227 && x > 18 && x < 110 && y % 8 < 3;
                Color color = variant == 0 ? new Color(.26f, .30f, .25f) : variant == 1 ? new Color(.34f, .27f, .21f) : new Color(.33f, .35f, .32f);
                color *= .69f + (float)noise.NextDouble() * .25f;
                if(frame || seam || vent) color *= .27f;
                if(y < 65 && !frame) color *= .68f;
                if((x / 9 + y / 23 + variant * 3) % 17 == 2 && !frame) color = Color.Lerp(color, new Color(.13f, .15f, .08f), .38f);
                // Quiet painted seams and weathering only: no cross boards, handle or active indicator.
                texture.SetPixel(x, y, color);
            }
            SaveTexture(name, texture);
            if(variant == 0) continue;
            string path = Art + "/Materials/" + name + ".mat";
            var created = new Material(mats["SealedDoor"]) { name = name };
            created.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/" + name + ".png"));
            SaveMaterial(path, created, name);
        }
    }
    static void FabricTexture()
    {
        var texture = new Texture2D(128, 128, TextureFormat.RGB24, false); var noise = new System.Random(9007);
        for(int y = 0; y < 128; y += 4) for(int x = 0; x < 128; x += 4)
        {
            float value = .66f + (float)noise.NextDouble() * .33f;
            for(int yy = 0; yy < 4; yy++) for(int xx = 0; xx < 4; xx++) texture.SetPixel(x + xx, y + yy, new Color(value, value, value));
        }
        SaveTexture("DecayFabric", texture);
    }
    static void SaveTexture(string name, Texture2D texture)
    {
        texture.Apply(); string path = Art + "/Textures/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.filterMode = FilterMode.Point; importer.maxTextureSize = 128;
        importer.anisoLevel = 1; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
    }
    static void Surface(string name, Color color, float smoothness)
    {
        var created = new Material(mats["Concrete"]) { name = name, enableInstancing = true };
        created.SetColor("_BaseColor", color); created.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/DecayFabric.png"));
        created.SetTextureScale("_BaseMap", Vector2.one); created.SetFloat("_Smoothness", smoothness); created.SetFloat("_Metallic", 0);
        SaveMaterial(Art + "/Materials/" + name + ".mat", created, name);
    }
    static void SaveMaterial(string path, Material created, string name)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing) { EditorUtility.CopySerialized(created, existing); Object.DestroyImmediate(created); }
        else { AssetDatabase.CreateAsset(created, path); existing = created; }
        mats[name] = existing;
    }
    static void Lighting()
    {
        foreach(var light in atrium.GetComponentsInChildren<Light>())
        {
            if(light.GetComponentInParent<OpenDoorWithKey>() || light.transform.parent.name.StartsWith("Service bay ")) continue;
            if(light.transform.parent == atrium) light.intensity = 3.2f;
            else if(light.transform.parent.name == "Kiosk bay") light.intensity = 4.5f;
            else if(light.transform.parent.name == "Pendant")
            {
                bool broken = light.transform.parent.localPosition.x < 0 && light.transform.parent.localPosition.z < -3;
                light.intensity = broken ? 0 : 2.8f;
                if(broken) foreach(var renderer in light.transform.parent.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial == mats["Amber"]) renderer.sharedMaterial = mats["Dark"];
            }
        }
    }
    static Transform Group(string name, Vector3 position, float yaw = 0)
    { var t = new GameObject(name).transform; t.SetParent(details, false); t.localPosition = position; t.localRotation = Quaternion.Euler(0, yaw, 0); return t; }
    static Transform Bay(string name, int number)
    { var source = atrium.Find("Service bay " + number); var t = Group(name, source.localPosition); t.localRotation = source.localRotation; return t; }
    static GameObject Box(Transform p, string name, Vector3 position, Vector3 size, string material, Vector3? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(p, false);
        go.transform.localPosition = position; go.transform.localScale = size; if(rotation.HasValue) go.transform.localRotation = Quaternion.Euler(rotation.Value);
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mats[material]; return go;
    }
    static void Beam(Transform p, string name, Vector3 a, Vector3 b, float thickness, string material)
    { var go = Box(p, name, (a + b) / 2, V(thickness, (b - a).magnitude, thickness), material); go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); }
    static void Label(Transform p, string words, Vector3 pos, float size, string material)
    {
        material = material == "DecayBlood" ? "DecayLetteringBlood" : "DecayLettering";
        // World geometry gives signs a small Doom-like pixel font and proper wall occlusion.
        string plain = string.Concat(words.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToUpperInvariant();
        string[] lines = plain.Split('\n'); float pixel = size / 7, advance = pixel * 6;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        for(int line = 0; line < lines.Length; line++) for(int letter = 0; letter < lines[line].Length; letter++)
        {
            if(!Glyphs.TryGetValue(lines[line][letter], out string glyph)) continue;
            for(int row = 0; row < 7; row++)
            {
                int bits = Convert.ToInt32(glyph.Substring(row * 2, 2), 16);
                for(int col = 0; col < 5; col++) if((bits & (1 << (4 - col))) != 0)
                {
                    float x = (letter - (lines[line].Length - 1) / 2f) * advance + (col - 2) * pixel;
                    float y = ((lines.Length - 1) / 2f - line) * pixel * 9 + (3 - row) * pixel;
                    int n = vertices.Count; float half = pixel * .48f;
                    vertices.AddRange(new[] { V(x - half, y - half, 0), V(x + half, y - half, 0), V(x + half, y + half, 0), V(x - half, y + half, 0) });
                    uv.AddRange(Enumerable.Repeat(new Vector2(.5f, .5f), 4)); triangles.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
                }
            }
        }
        var go = MeshObject(p, "Pixel lettering " + plain.Replace('\n', ' '), vertices.ToArray(), uv.ToArray(), triangles.ToArray(), material);
        go.transform.localPosition = pos; go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }
    static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        ['A']="0E11111F111111", ['B']="1E11111E11111E", ['C']="0F10101010100F", ['D']="1E11111111111E", ['E']="1F10101E10101F", ['F']="1F10101E101010",
        ['G']="0F10101711110F", ['H']="1111111F111111", ['I']="0E04040404040E", ['J']="0702020202120C", ['K']="11121418141211", ['L']="1010101010101F",
        ['M']="111B1515111111", ['N']="11191513111111", ['O']="0E11111111110E", ['P']="1E11111E101010", ['Q']="0E11111115120D", ['R']="1E11111E141211",
        ['S']="0F10100E01011E", ['T']="1F040404040404", ['U']="1111111111110E", ['V']="11111111110A04", ['W']="11111115151B11", ['X']="11110A040A1111",
        ['Y']="11110A04040404", ['Z']="1F01020408101F", ['0']="0E11131519110E", ['1']="040C040404040E", ['2']="0E11010204081F", ['3']="1E01010601011E",
        ['4']="02060A121F0202", ['5']="1F101E0101110E", ['6']="0E10101E11110E", ['7']="1F010204080808", ['8']="0E11110E11110E", ['9']="0E11110F01010E",
        [':']="00040400040400", ['/']="01010204081010", ['-']="0000001F000000", ['.']="00000000000C0C", ['?']="0E110102040004"
    };
    static void WorldLettering()
    {
        int index = 0;
        foreach(var text in atrium.GetComponentsInChildren<TextMesh>())
        {
            var renderer = text.GetComponent<MeshRenderer>(); float height = renderer.bounds.size.y / text.transform.lossyScale.y;
            if(height < .06f || height > .6f) height = .20f;
            var group = Group("World lettering " + index++, atrium.InverseTransformPoint(text.transform.position));
            group.localRotation = Quaternion.Inverse(atrium.rotation) * text.transform.rotation;
            Label(group, text.text, Vector3.zero, height, "DecayPaper"); renderer.enabled = false;
        }
    }
    static void Light(Transform p, Vector3 pos, Color color, float intensity, float range)
    { var go = new GameObject("Story light"); go.transform.SetParent(p, false); go.transform.localPosition = pos; var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None; }
    static void Solid(Transform p, Vector3 center, Vector3 size)
    { var c = p.gameObject.AddComponent<BoxCollider>(); c.center = center; c.size = size; }

    static void Canteen()
    {
        var goods = Group("Canteen abandoned meals spoiled supplies and spilled coffee", Vector3.zero);
        for(int i = 0; i < 8; i++)
        {
            float a = (i + .20f) * Mathf.PI / 4; Vector3 at = Oval(a, 3.88f, 3.88f, 1.205f);
            var tray = Box(goods, "Forgotten metal serving tray", at, V(.45f, .018f, .30f), "Rust", V(0, a * Mathf.Rad2Deg, 0));
            for(int k = 0; k < 2; k++) Box(goods, "Mold on forgotten food", at + V(-.10f + k * .13f, .035f, 0), V(.105f, .052f, .105f), "DecayMold");
            if(i % 2 == 0) Lump(goods, "Empty discarded bottle", at + V(.10f, .11f, .05f), V(.09f, .22f, .09f), "DoorActive");
            else Box(goods, "Dusty paper carton", at + V(.12f, .10f, .025f), V(.10f, .20f, .12f), "DecayPaper", V(0, 12, 0));
        }
        var notice = Group("Canteen closure menu and stopped clock", V(1.8f, 2.5f, 3.58f), 180);
        Box(notice, "Discarded menu board", Vector3.zero, V(1.24f, .60f, .035f), "Wood");
        Label(notice, "CANTINA ENCERRADA\nPOR TEMPO INDETERMINADO", V(0, 0, -.023f), .10f, "DecayPaper");
        var aftermath = Group("Overturned queue stand and abandoned luggage", V(7.2f, .01f, 4.2f), 27);
        Box(aftermath, "Overturned sign foot", V(0, .09f, 0), V(.7f, .14f, .52f), "Iron", V(0, 0, 78));
        Beam(aftermath, "Bent fallen sign stem", V(0, .08f, 0), V(.76f, .11f, -.18f), .058f, "Rust");
        Box(aftermath, "Fallen queue sign", V(.97f, .12f, -.18f), V(.54f, .026f, .34f), "DecayPaper", V(0, -17, 0));
        Box(aftermath, "Left-behind canvas satchel", V(-.57f, .20f, -.40f), V(.48f, .38f, .27f), "DecayDust", V(0, -20, 7));
        Beam(aftermath, "Torn satchel strap", V(-.8f, .32f, -.4f), V(-1.1f, .02f, -.55f), .027f, "Iron");
        for(int i = 0; i < 9; i++) Box(aftermath, "Scattered evacuation leaflet", V(R(-1.0f, 1.2f), .027f, R(-.8f, .6f)), V(.19f, .003f, .28f), "DecayPaper", V(0, R(0, 180), 0));
        var broken = Group("Broken cafe table porcelain and dragged chair traces", V(-7.2f, 0, -4.3f), 35);
        for(int i = 0; i < 8; i++) Box(broken, "Broken cup shard", V(R(-.7f, .7f), .030f, R(-.6f, .6f)), V(.05f, .023f, .05f), "Concrete", V(0, R(0, 180), 0));
        Patch(broken, V(.3f, .027f, 0), .45f, .31f, "DecayOrganic");
        for(int i = 0; i < 3; i++) Beam(broken, "Chair dragged over stone", V(-.4f + i * .13f, .024f, -.8f), V(-.3f + i * .13f, .024f, .6f), .013f, "DecayDust");
    }
    static void ServiceStories()
    {
        var radio = Bay("Abandoned emergency radio and campus map", 7);
        Box(radio, "Emergency shelf", V(0, 1.0f, -.65f), V(1.35f, .09f, .56f), "Wood");
        for(int side = -1; side <= 1; side += 2) Box(radio, "Shelf leg", V(side * .50f, .50f, -.58f), V(.055f, 1.0f, .055f), "Iron");
        Box(radio, "Dusty shortwave radio", V(-.18f, 1.21f, -.65f), V(.60f, .35f, .31f), "Rust");
        Box(radio, "Radio tuning screen", V(-.18f, 1.28f, -.816f), V(.35f, .075f, .013f), "Green");
        for(int i = 0; i < 5; i++) Box(radio, "Radio speaker slot", V(-.18f + i * .055f, 1.10f, -.817f), V(.018f, .10f, .013f), "Dark");
        Beam(radio, "Broken aerial", V(.08f, 1.36f, -.56f), V(.27f, 1.84f, -.59f), .013f, "Iron");
        Box(radio, "Campus map board", V(0, 2.55f, -.29f), V(1.52f, 1.05f, .05f), "Rust");
        Box(radio, "Yellowed campus schematic", V(0, 2.55f, -.325f), V(1.39f, .90f, .018f), "DecayPaper");
        for(int i = 0; i < 4; i++) { Box(radio, "Map building outline", V(-.43f + (i % 2) * .56f, 2.45f + (i / 2) * .28f, -.338f), V(.29f, .15f, .006f), "DoorActive"); }
        Beam(radio, "Campus map path", V(-.43f, 2.36f, -.346f), V(.35f, 2.36f, -.346f), .018f, "Dark");
        Beam(radio, "Campus map path", V(.35f, 2.36f, -.346f), V(.35f, 2.81f, -.346f), .018f, "Dark");
        Beam(radio, "Campus map path", V(.35f, 2.81f, -.346f), V(-.43f, 2.81f, -.346f), .018f, "Dark");
        Label(radio, "ÚLTIMO SINAL: 03:17\nNINGUÉM RESPONDE", V(0, 3.10f, -.36f), .15f, "DecayPaper");
        Solid(radio, V(0, .60f, -.59f), V(1.35f, 1.20f, .6f));
        Light(radio, V(0, 2.3f, -1.45f), new Color(1, .76f, .49f), 1.8f, 4.3f);

        var board = Bay("Water damaged notices and missing people board", 19);
        Box(board, "Old bulletin frame", V(0, 2.3f, -.28f), V(1.70f, 1.16f, .055f), "Wood");
        for(int i = 0; i < 7; i++) Box(board, "Faded missing-person notice", V(-.66f + i * .20f, 2.1f + (i % 2) * .22f, -.32f), V(.18f, .34f, .013f), "DecayPaper", V(0, 0, R(-8, 8)));
        Label(board, "DESAPARECIDOS", V(0, 2.78f, -.35f), .16f, "DecayPaper");
        for(int i = 0; i < 5; i++) Box(board, "Ink washed off notices", V(-.4f + i * .18f, 2.26f, -.337f), V(.07f, .045f, .005f), "DecayDust");

        var machine = Bay("Broken vending machine and lost maintenance cart", 33);
        Box(machine, "Vending cabinet", V(-.20f, 1.0f, -.59f), V(1.08f, 2.0f, .63f), "Rust");
        Box(machine, "Dark broken vending glass", V(-.20f, 1.16f, -.917f), V(.82f, 1.18f, .018f), "Dark");
        for(int row = 0; row < 3; row++) for(int col = 0; col < 3; col++)
            Box(machine, "Forgotten vending carton", V(-.45f + col * .25f, .74f + row * .36f, -.943f), V(.15f, .18f, .035f), (row + col) % 2 == 0 ? "DecayPaper" : "RedPaint");
        Beam(machine, "Cracked vending glass scar", V(-.58f, 1.59f, -.966f), V(.09f, .95f, -.966f), .016f, "Concrete");
        Box(machine, "Broken dispensing slot", V(-.20f, .30f, -.943f), V(.61f, .19f, .028f), "Iron");
        Solid(machine, V(-.20f, 1.0f, -.6f), V(1.10f, 2.0f, .68f));
        Label(machine, "FORA DE SERVIÇO", V(-.20f, 1.98f, -.95f), .12f, "DecayPaper");

        foreach(int number in new[] { 5, 15, 27, 39 })
        {
            var bench = Bay("Weathered bench and abandoned belongings " + number, number);
            Box(bench, "Splintered bench seat", V(0, .58f, -.58f), V(1.62f, .085f, .48f), "Wood");
            Box(bench, "Bench back", V(0, .91f, -.37f), V(1.62f, .36f, .055f), "Wood");
            foreach(int side in new[] { -1, 1 }) Beam(bench, "Bench support", V(side * .62f, .02f, -.58f), V(side * .62f, .57f, -.58f), .07f, "Iron");
            Box(bench, "Discarded textbook", V(.41f, .65f, -.6f), V(.23f, .05f, .29f), number % 2 == 0 ? "RedPaint" : "DecayPaper", V(0, 12, 0));
            Solid(bench, V(0, .47f, -.55f), V(1.66f, .94f, .61f));
        }
        var clock = Bay("Clock stopped at the evacuation time", 45);
        Box(clock, "Broken station clock", V(0, 3.05f, -.29f), V(.80f, .48f, .06f), "Iron");
        Label(clock, "03:17", V(0, 3.05f, -.34f), .23f, "DecayPaper");
    }
    static void CreatureTraces()
    {
        var trail = Group("Four toed tracks and blood leading behind the canteen", Vector3.zero);
        for(int i = 0; i < 34; i++)
        {
            float a = .14f + i * Mathf.PI / 36; Vector3 p = Oval(a, 12.3f, 10.2f, .031f);
            Patch(trail, p, .15f, .21f, "DecayBlood");
            for(int toe = 0; toe < 4; toe++) Beam(trail, "Four long claw imprints", p + V(-.16f + toe * .10f, .003f, -.20f), p + V(-.19f + toe * .12f, .003f, -.39f), .036f, "DecayBlood");
            if(i % 5 == 0) Patch(trail, p + V(.3f, -.003f, .25f), .31f, .25f, "DecayBlood");
        }
        for(int i = 0; i < 12; i++) Patch(trail, V(1.9f + i * .04f, .032f, -11.5f - i * .48f), R(.035f, .09f), R(.10f, .22f), "DecayBlood");
        foreach(int number in new[] { 9, 21, 30, 42 })
        {
            var scars = Bay("Deep claw gouges and crumbling masonry " + number, number);
            for(int i = 0; i < 4; i++)
            {
                Beam(scars, "Creature claw gouge", V(-.47f + i * .19f, .90f, -.278f), V(-.22f + i * .19f, 2.55f, -.278f), .047f, "Dark");
                Beam(scars, "Exposed stone at gouge", V(-.51f + i * .19f, .96f, -.276f), V(-.26f + i * .19f, 2.60f, -.276f), .018f, "Rubble");
            }
            Patch(scars, V(.40f, 1.20f, -.273f), .32f, .45f, "DecayBlood", V(-90, 0, 0));
        }
        var nest = Bay("Creature molt nest hidden behind the upper gallery", 30);
        Lump(nest, "Discarded creature carapace", V(0, 4.72f, -.36f), V(1.08f, 1.02f, .86f), "DecayOrganic");
        Lump(nest, "Empty insectlike head shell", V(0, 4.90f, -.94f), V(.53f, .43f, .43f), "DecayChitin");
        for(int side = -1; side <= 1; side += 2) for(int i = 0; i < 3; i++)
        {
            Vector3 a = V(side * .40f, 4.74f, -.15f - i * .21f), b = V(side * (1.05f + i * .13f), 5.10f, -.32f - i * .37f), c = V(side * (1.38f + i * .13f), 4.30f, -.18f - i * .40f);
            Beam(nest, "Long discarded chitin leg", a, b, .075f, "DecayOrganic"); Beam(nest, "Broken leg tip", b, c, .048f, "DecayChitin");
        }
        foreach(Transform child in nest)
            if(child.name == "Discarded creature carapace" || child.name == "Empty insectlike head shell" || child.name == "Long discarded chitin leg" || child.name == "Broken leg tip")
                child.localPosition += Vector3.back * 1.15f;
        for(int i = 0; i < 9; i++) Lump(nest, "Pale hatched egg shell", V(R(-.86f, .86f), 4.46f, R(-.73f, .04f)), V(.28f, .45f, .28f), "DecayChitin");
        for(int i = 0; i < 8; i++) Beam(nest, "Sticky nest strand", V(R(-1.1f, 1.1f), 5.50f, -.2f), V(R(-1.3f, 1.3f), .30f, -.3f), .017f, "DecayOrganic");
        Light(nest, V(-.6f, 5.4f, -1.75f), new Color(.78f, .72f, .46f), 2.8f, 5.5f);
        var planter = atrium.Find("Column and planter 10");
        var infestation = Group("Egg clutch spreading through a dead planter", planter.localPosition);
        for(int i = 0; i < 11; i++) Lump(infestation, "Unhatched clustered egg", V(R(-.7f, .7f), .78f, R(-.52f, .52f)), V(.30f, .55f, .31f), "DecayChitin");
        for(int i = 0; i < 7; i++) Beam(infestation, "Organic tendril on column", V(R(-.45f, .45f), .45f, -.28f), V(R(-.20f, .20f), 1.5f + i * .59f, -.28f), .035f, "DecayOrganic");
        Patch(infestation, V(.30f, .022f, .42f), .94f, .49f, "DecayBlood");
        var message = Bay("Faded warning beside the rear access", 25);
        Label(message, "OUVIMOS ALGO\nDENTRO DAS PAREDES", V(0, 2.68f, -.285f), .19f, "DecayBlood");
    }
    static void Weathering()
    {
        foreach(int number in new[] { 3, 6, 10, 14, 17, 20, 26, 29, 32, 35, 40, 44, 47 })
        {
            var damage = Bay("Water damage peeling wall and exposed conduit " + number, number);
            for(int i = 0; i < 6; i++)
            {
                float x = R(-.9f, .9f), y = R(.25f, 2.9f);
                Patch(damage, V(x, y, -.269f), R(.07f, .25f), R(.15f, .60f), i % 3 == 0 ? "DecayDust" : "DecayMold", V(-90, 0, 0));
                Box(damage, "Long mineral leak", V(x, y - .3f, -.270f), V(.022f, R(.25f, .65f), .006f), "DecayMold");
            }
            if(number % 2 == 0)
            {
                Beam(damage, "Corroded exposed conduit", V(-.95f, .2f, -.30f), V(-.95f, 3.7f, -.30f), .038f, "Rust");
                Box(damage, "Junction box", V(-.93f, 1.85f, -.33f), V(.20f, .31f, .12f), "Iron");
                Beam(damage, "Broken hanging wire", V(-.90f, 1.72f, -.40f), V(-.68f, .67f, -.52f), .015f, "Dark");
            }
            for(int i = 0; i < 5; i++) Box(damage, "Fallen plaster fragment", V(R(-.8f, .8f), R(.03f, .08f), R(-.80f, -.45f)), V(R(.12f, .35f), .05f, R(.09f, .26f)), "Rubble", V(R(-10, 10), R(0, 180), 0));
        }
        var roof = Group("Torn roof insulation dangling cables and corrosion", Vector3.zero);
        for(int i = 0; i < 7; i++)
        {
            float a = (i + .4f) * Mathf.PI * 2 / 7; Vector3 at = Oval(a, 20, 14.5f, 6.5f);
            Beam(roof, "Ceiling cable torn loose", at + V(-.5f, .8f, 0), at + V(.15f, -1.2f, .3f), .019f, "Dark");
            for(int strip = 0; strip < 3; strip++) Box(roof, "Torn weathered insulation strip", at + V(-.2f + strip * .19f, -.2f - strip * .12f, 0), V(.14f, .75f + strip * .2f, .009f), "DecayDust", V(0, i * 35, R(-12, 12)));
        }
    }
    static GameObject Lump(Transform p, string name, Vector3 pos, Vector3 size, string material)
    {
        const int rings = 5, sides = 8; var v = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        for(int r = 0; r <= rings; r++) for(int s = 0; s <= sides; s++)
        { float a = s * Mathf.PI * 2 / sides, b = r * Mathf.PI / rings; v.Add(V(Mathf.Sin(b) * Mathf.Cos(a) * size.x / 2, Mathf.Cos(b) * size.y / 2, Mathf.Sin(b) * Mathf.Sin(a) * size.z / 2)); uv.Add(new Vector2(s / (float)sides, r / (float)rings)); }
        for(int r = 0; r < rings; r++) for(int s = 0; s < sides; s++) { int a = r * (sides + 1) + s; indices.AddRange(new[] { a, a + 1, a + sides + 1, a + 1, a + sides + 2, a + sides + 1 }); }
        var go = MeshObject(p, name, v.ToArray(), uv.ToArray(), indices.ToArray(), material); go.transform.localPosition = pos; return go;
    }
    static void Patch(Transform p, Vector3 pos, float width, float depth, string material, Vector3? rot = null)
    {
        const int sides = 20; var v = new Vector3[sides + 1]; var uv = new Vector2[sides + 1]; var indices = new int[sides * 3]; uv[0] = new Vector2(.5f, .5f);
        for(int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2 / sides, r = R(.65f, 1); v[i + 1] = V(Mathf.Cos(a) * width * r, 0, Mathf.Sin(a) * depth * r); uv[i + 1] = new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f); indices[i * 3] = 0; indices[i * 3 + 1] = (i + 1) % sides + 1; indices[i * 3 + 2] = i + 1; }
        var go = MeshObject(p, "Irregular " + material + " mark", v, uv, indices, material); go.transform.localPosition = pos; if(rot.HasValue) go.transform.localRotation = Quaternion.Euler(rot.Value);
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }
    static GameObject MeshObject(Transform p, string name, Vector3[] v, Vector2[] uv, int[] indices, string material)
    { var mesh = new Mesh { name = name }; mesh.vertices = v; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); var go = new GameObject(name); go.transform.SetParent(p, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mats[material]; return go; }
    static void Combine(Transform group)
    {
        var renderers = group.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<MeshFilter>() && !r.GetComponent<TextMesh>()).ToArray();
        foreach(var bucket in renderers.GroupBy(r => r.sharedMaterial))
        {
            var mesh = new Mesh { name = "Decay_" + group.name + "_" + bucket.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(bucket.Select(r => new CombineInstance { mesh = r.GetComponent<MeshFilter>().sharedMesh, transform = group.worldToLocalMatrix * r.transform.localToWorldMatrix }).ToArray(), true, true);
            string safe = string.Concat(mesh.name.Select(c => char.IsLetterOrDigit(c) ? c : '_')), path = Art + "/Meshes/" + safe + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; } else AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(bucket.Key.name); go.transform.SetParent(group, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = bucket.Key; if(bucket.All(r => r.shadowCastingMode == ShadowCastingMode.Off)) renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        foreach(var renderer in renderers) { var mesh = renderer.GetComponent<MeshFilter>().sharedMesh; if(!AssetDatabase.Contains(mesh)) Object.DestroyImmediate(mesh); Object.DestroyImmediate(renderer.gameObject); }
    }
}
