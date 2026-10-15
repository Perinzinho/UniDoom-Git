using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using Object = UnityEngine.Object;
using static CylinderCanteenGeometry;

public static class CylinderCanteenBuilder
{
    public const string Prefab = "Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab";
    public const string Root = "Assets/_Project/Scenes/Cylinder";
    public const string Output = Root + "/Meshes/CanteenDoors";
    static Transform atrium;
    static readonly Dictionary<string, Mesh> shapes = new Dictionary<string, Mesh>();
    static int removedText;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    public static void Build()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/.utmp/CylinderDetailPreview/")) throw new Exception("Use isolated project");
        Directory.CreateDirectory(Output); Directory.CreateDirectory(Root + "/Materials/CanteenDoors"); AssetDatabase.Refresh();
        var room = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            atrium = room.transform.Find("Abandoned circular atrium");
            if (atrium.Find("Wordless door identities")) throw new Exception("Revision already applied");
            LoadMaterials();
            RemoveTexts(room);
            RefineCanteen();
            ClearAwning(room);
            DoorIdentities();
            FrontDoor();
            Prune(room);
            PrefabUtility.SaveAsPrefabAsset(room, Prefab);
            Directory.CreateDirectory("CanteenEvidence");
            File.WriteAllText("CanteenEvidence/removed-text.txt", removedText + " text components/lettering renderers cleared or hidden.\nImported electrical-cabinet lettering is replaced by unlabelled PBR metal.\nAll new plaques contain only geometric pictograms.\n");
        }
        finally { PrefabUtility.UnloadPrefabContents(room); }
        AssetDatabase.SaveAssets(); Debug.Log("CYLINDER_CANTEEN_BUILD_PASS");
    }

    public static void OptimizeGenerated()
    {
        var room=PrefabUtility.LoadPrefabContents(Prefab);
        try { ClearAwning(room); Prune(room); PrefabUtility.SaveAsPrefabAsset(room,Prefab); }
        finally { PrefabUtility.UnloadPrefabContents(room); }
        AssetDatabase.SaveAssets();
    }

    static void ClearAwning(GameObject room)
    {
        var detail=room.transform.Find("Abandoned circular atrium/Total Chaos inspired material and geometry detail");
        if(detail) foreach(var r in detail.GetComponentsInChildren<MeshRenderer>(true))
            if(r.sharedMaterials.Any(m=>m&&m.name=="DecayPaper")) r.enabled=false;
    }

    static void Prune(GameObject room)
    {
        foreach(var root in room.GetComponentsInChildren<Transform>(true).Where(t =>
            t.name=="Refined canteen models" || t.name=="Wordless door identities" || t.name=="Larger front entrance" ||
            t.name=="Closed secondary entrance opening" || t.name.StartsWith("Room-specific door inset - ")).ToArray())
            foreach(var t in root.GetComponentsInChildren<Transform>(true).Reverse())
            {
                if(!t || t==root || t.childCount!=0 || t.GetComponents<Component>().Length!=1) continue;
                if(t.name.StartsWith("Door identity - ") || t.name.StartsWith("Geometric pictogram - ") || t.name.StartsWith("Wordless ") ||
                   t.name=="Twin group espresso machine" || t.name=="Detailed insulated coffee urn" || t.name=="Hollow ceramic cup") continue;
                Object.DestroyImmediate(t.gameObject);
            }
    }

    static Material Flat(string name, Color color, float metal, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
        m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
        AssetDatabase.CreateAsset(m, Root + "/Materials/CanteenDoors/" + name + ".mat"); return m;
    }

    static void LoadMaterials()
    {
        foreach (var key in new[] { "Concrete", "Iron", "Rust", "Wood", "RedPaint", "Plaster" })
            mats[key] = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/DetailRevision/" + key + ".mat");
        mats["Dark"] = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/DetailRevision/Dark.mat");
        mats["Amber"] = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/DetailRevision/Amber.mat");
        mats["Steel"] = Flat("Aged satin steel", new Color(.48f, .52f, .51f), .68f, .42f);
        mats["Ceramic"] = Flat("Stained ivory porcelain", new Color(.70f, .68f, .60f), 0, .24f);
        mats["Rubber"] = Flat("Aged rubber", new Color(.025f, .03f, .028f), 0, .14f);
        mats["Coffee"] = Flat("Coffee residue", new Color(.055f, .021f, .009f), 0, .25f);
        mats["Icon"] = Flat("Worn ivory pictogram paint", new Color(.78f, .77f, .64f), 0, .12f);
        mats["Classroom"] = Flat("Classroom slate enamel", new Color(.13f, .24f, .22f), .15f, .22f);
        mats["Library"] = Flat("Library oxblood enamel", new Color(.27f, .115f, .08f), .15f, .22f);
        mats["Director"] = Flat("Director aged brass", new Color(.40f, .30f, .14f), .55f, .32f);
        mats["Service"] = Flat("Service grey enamel", new Color(.18f, .20f, .19f), .15f, .20f);
        var steel = mats["Steel"];
        steel.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Global/Textures/TotalChaosInspired/Metal_Normal.png"));
        steel.EnableKeyword("_NORMALMAP"); steel.SetFloat("_BumpScale", .16f); EditorUtility.SetDirty(steel);
    }

    static void RemoveTexts(GameObject room)
    {
        foreach (var text in room.GetComponentsInChildren<TextMesh>(true))
        {
            text.text = ""; var r = text.GetComponent<Renderer>(); if (r) r.enabled = false; removedText++;
        }
        foreach (var text in room.GetComponentsInChildren<TMP_Text>(true))
        {
            text.text = ""; text.enabled = false; var r = text.GetComponent<Renderer>(); if (r) r.enabled = false; removedText++;
        }
        foreach (var r in room.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.sharedMaterials.Any(m => m && m.name.Contains("Lettering"))) { r.enabled = false; removedText++; }
            // Source FBX cabinet has words/numbers in its albedo. Keep its geometry and use unlabelled metal.
            if (r.sharedMaterials.Any(m => m && m.name == "utility_box_02"))
                r.sharedMaterials = r.sharedMaterials.Select(m => m && m.name == "utility_box_02" ? mats["Iron"] : m).ToArray();
        }
    }

    static void RefineCanteen()
    {
        var old = atrium.Find("Central canteen - warm landmark");
        // Only the combined visual shell is replaced; its countertop colliders, lights and support markers remain.
        foreach (Transform child in old)
        {
            var r = child.GetComponent<MeshRenderer>(); if (r) r.enabled = false;
        }
        var detail = Group(old, "Refined canteen models", Vector3.zero);
        var sectors = Enumerable.Range(0, 4).Select(i => Group(detail, "Counter quarter " + i, Vector3.zero)).ToArray();
        var bays = old.Cast<Transform>().Where(t => t.name == "Kiosk bay").ToArray();
        if (bays.Length != 16) throw new Exception("Expected sixteen original counter bays");
        for (int i = 0; i < bays.Length; i++)
        {
            var p = Group(sectors[i / 4], "Detailed coffee station " + i, bays[i].localPosition); p.localRotation = bays[i].localRotation;
            Box(p, "Recessed plinth", V(0, .07f, -.4f), V(1.54f, .14f, 1.08f), "Rubber", .018f);
            Box(p, "Cupboard carcass", V(0, .60f, -.4f), V(1.59f, .96f, 1.15f), "Wood", .025f);
            foreach (float x in new[] { -.40f, .40f })
            {
                Box(p, "Framed cupboard door", V(x, .60f, .191f), V(.765f, .88f, .035f), "Wood", .012f);
                Box(p, "Inset door panel", V(x, .60f, .216f), V(.65f, .71f, .018f), "Iron", .009f);
                Pipe(p, V(x + (x < 0 ? .25f : -.25f), .67f, .26f), V(x + (x < 0 ? .25f : -.25f), .84f, .26f), .013f, "Steel");
            }
            Box(p, "Rounded stone worktop", V(0, 1.14f, -.4f), V(1.65f, .10f, 1.20f), "Concrete", .024f);
            Box(p, "Bullnose counter edge", V(0, 1.13f, .213f), V(1.64f, .060f, .045f), "Steel", .016f);
            foreach (float x in new[] { -.76f, .76f })
            {
                Box(p, "Steel canopy upright", V(x, 2.0f, 0), V(.065f, 1.72f, .09f), "Iron", .009f);
                Box(p, "Upright foot plate", V(x, 1.23f, 0), V(.14f, .075f, .15f), "Steel", .012f);
                foreach (float dx in new[] { -.045f, .045f }) Cylinder(p, "Mounting screw", V(x + dx, 1.273f, .04f), .009f, .008f, "Steel", 8);
            }
            Box(p, "Canopy fascia without lettering", V(0, 2.9f, 0), V(1.65f, .30f, .30f), "RedPaint", .025f);
            Box(p, "Recessed warm strip", V(0, 2.65f, -.16f), V(1.36f, .025f, .035f), "Amber", .006f);
            Espresso(p, V(.2f, 1.19f, -.6f));
            Urn(p, V(-.35f, 1.19f, -.5f));
            for (int k = 0; k < 3; k++) Cup(p, V(-.55f + k * .16f, 1.19f, .09f), i * 37 + k * 53);
        }
        foreach (var sector in sectors) Batch(sector, "Canteen finish", true);
        var roof = Group(detail, "Rolled canopy and extractor", Vector3.zero);
        Lathe(roof, "Rolled circular canopy", new[] { new Vector2(0, 3.04f), new Vector2(4.44f, 3.04f), new Vector2(4.55f, 3.09f), new Vector2(4.55f, 3.20f), new Vector2(4.44f, 3.26f), new Vector2(0, 3.26f) }, "Iron", 128);
        Torus(roof, "Rolled canopy edge", V(0, 3.15f, 0), 4.53f, .040f, "Steel", Quaternion.Euler(90, 0, 0), 128);
        Lathe(roof, "Extractor hood", new[] { new Vector2(0, 3.26f), new Vector2(.78f, 3.26f), new Vector2(.73f, 3.39f), new Vector2(.49f, 3.67f), new Vector2(.36f, 3.72f), new Vector2(.36f, 4.06f), new Vector2(0, 4.06f) }, "Steel", 48);
        foreach (float y in new[] { 3.30f, 3.74f, 4.02f }) Torus(roof, "Extractor rolled seam", V(0, y, 0), y < 3.4f ? .74f : .37f, .025f, "Rust", Quaternion.Euler(90, 0, 0), 48);
        Batch(roof, "Canopy finish", true);
        // The old menu is a loose slab in front of the machine; preserve it as a hidden reference.
        var menu = atrium.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Canteen closure menu and stopped clock");
        if (menu) foreach (var r in menu.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
    }

    static void Espresso(Transform parent, Vector3 feet)
    {
        var p = Group(parent, "Twin group espresso machine", feet);
        Box(p, "Rubber base", V(0, .018f, 0), V(.46f, .036f, .40f), "Rubber", .012f);
        Box(p, "Rounded boiler housing", V(0, .31f, -.105f), V(.45f, .54f, .17f), "Steel", .035f);
        Box(p, "Brew head overhang", V(0, .49f, .035f), V(.46f, .17f, .34f), "Steel", .028f);
        Box(p, "Raised cup rail", V(0, .588f, -.05f), V(.43f, .025f, .25f), "Iron", .009f);
        Box(p, "Recessed drip basin", V(0, .045f, .09f), V(.43f, .035f, .22f), "Rubber", .008f);
        for (int j = 0; j < 11; j++) Pipe(p, V(-.19f + j * .038f, .070f, -.005f), V(-.19f + j * .038f, .070f, .195f), .005f, "Steel");
        foreach (float x in new[] { -.105f, .105f })
        {
            Cylinder(p, "Group head", V(x, .385f, .14f), .042f, .07f, "Steel", 24);
            Pipe(p, V(x, .37f, .14f), V(x, .37f, .265f), .012f, "Steel");
            Pipe(p, V(x, .37f, .25f), V(x, .37f, .34f), .019f, "Rubber");
            Cylinder(p, "Brew nozzle", V(x, .335f, .14f), .012f, .035f, "Steel", 16);
            Cylinder(p, "Control button", V(x, .50f, .212f), .018f, .013f, "Rubber", 20, Quaternion.Euler(90, 0, 0));
        }
        Cylinder(p, "Pressure gauge bezel", V(0, .535f, .212f), .032f, .012f, "Steel", 32, Quaternion.Euler(90, 0, 0));
        Cylinder(p, "Unnumbered gauge face", V(0, .535f, .22f), .027f, .003f, "Ceramic", 32, Quaternion.Euler(90, 0, 0));
        Pipe(p, V(0, .535f, .224f), V(.014f, .549f, .224f), .002f, "Rubber");
        Pipe(p, V(.225f, .43f, .04f), V(.27f, .40f, .06f), .009f, "Steel");
        Pipe(p, V(.27f, .40f, .06f), V(.27f, .20f, .12f), .009f, "Steel");
        Pipe(p, V(.27f, .20f, .12f), V(.23f, .18f, .16f), .009f, "Steel");
        for (int j = 0; j < 6; j++) Box(p, "Boiler ventilation slit", V(-.231f, .19f + j * .045f, -.08f), V(.005f, .014f, .09f), "Rubber", .002f);
    }

    static void Urn(Transform parent, Vector3 feet)
    {
        var p = Group(parent, "Detailed insulated coffee urn", feet);
        Lathe(p, "Curved urn body", new[] { new Vector2(0, .025f), new Vector2(.105f, .025f), new Vector2(.128f, .065f), new Vector2(.128f, .35f), new Vector2(.112f, .39f), new Vector2(0, .39f) }, "Steel", 40);
        Cylinder(p, "Lid", V(0, .397f, 0), .128f, .018f, "Iron", 40);
        Cylinder(p, "Lid knob", V(0, .43f, 0), .024f, .045f, "Rubber", 24);
        foreach (int sign in new[] { -1, 1 })
        {
            Pipe(p, V(sign * .12f, .32f, 0), V(sign * .185f, .32f, 0), .014f, "Steel");
            Pipe(p, V(sign * .185f, .32f, 0), V(sign * .185f, .23f, 0), .018f, "Rubber");
            Pipe(p, V(sign * .185f, .23f, 0), V(sign * .12f, .23f, 0), .014f, "Steel");
        }
        Pipe(p, V(0, .14f, .115f), V(0, .14f, .185f), .016f, "Steel");
        Pipe(p, V(0, .14f, .185f), V(0, .095f, .185f), .014f, "Steel");
        Box(p, "Tap paddle", V(0, .185f, .166f), V(.022f, .07f, .025f), "Rubber", .008f);
        for (int j = 0; j < 3; j++)
        {
            float a = j * Mathf.PI * 2 / 3;
            Cylinder(p, "Urn foot", V(Mathf.Sin(a) * .085f, .014f, Mathf.Cos(a) * .085f), .017f, .028f, "Rubber", 16);
        }
    }

    static void Cup(Transform parent, Vector3 feet, float yaw)
    {
        var p = Group(parent, "Hollow ceramic cup", feet); p.localRotation = Quaternion.Euler(0, yaw, 0);
        Lathe(p, "Glazed hollow cup", new[] { new Vector2(0, 0), new Vector2(.040f, 0), new Vector2(.048f, .012f), new Vector2(.055f, .12f), new Vector2(.054f, .139f), new Vector2(.049f, .139f), new Vector2(.047f, .12f), new Vector2(.041f, .015f), new Vector2(0, .015f) }, "Ceramic", 32);
        Torus(p, "Cup handle", V(.065f, .076f, 0), .037f, .008f, "Ceramic", Quaternion.identity, 24);
        Cylinder(p, "Dried coffee inside", V(0, .023f, 0), .041f, .002f, "Coffee", 32);
    }

    static void DoorIdentities()
    {
        var root = Group(atrium, "Wordless door identities", Vector3.zero);
        var sectors = Enumerable.Range(0, 8).Select(i => Group(root, "Plaque sector " + i, Vector3.zero)).ToArray();
        for (int i = 2; i < 48; i++)
        {
            var bay = atrium.Find("Service bay " + i);
            string identity = i == 12 ? "Classroom" : i == 36 ? "Library" : i == 24 ? "Director" : new[] { "Electrical", "Storage", "Maintenance", "Washroom" }[i % 4];
            var p = Group(sectors[i / 6], "Door identity - " + identity + " - bay " + i, atrium.InverseTransformPoint(bay.position));
            p.rotation = bay.rotation;
            bool main = i == 12 || i == 24 || i == 36;
            var plate = Group(p, "Wordless " + identity + " plaque", V(0, main ? 3.6f : 2.65f, -.39f));
            if (!main) plate.localScale = Vector3.one * .80f;
            Plaque(plate, identity);
            if (main)
            {
                var door = bay.GetComponentInChildren<OpenDoorWithKey>();
                var leaf = (Transform)new SerializedObject(door).FindProperty("door").objectReferenceValue;
                if (identity == "Library" || identity == "Director")
                    foreach (var r in leaf.GetComponentsInChildren<MeshRenderer>())
                        r.sharedMaterials = r.sharedMaterials.Select(m => m && m.name == "DoorActive" ? mats["Wood"] : m).ToArray();
                // Small inset details follow the moving leaf, avoiding detached signs when it opens.
                var inset = Group(leaf, "Room-specific door inset - " + identity, V(.115f, 2.35f, 0));
                inset.localRotation = Quaternion.Euler(0, -90, 0);
                if (identity == "Classroom")
                {
                    Box(inset, "Institutional observation frame", Vector3.zero, V(.44f, .40f, .035f), "Steel", .012f);
                    Box(inset, "Opaque safety-glass insert", V(0, 0, -.025f), V(.36f, .32f, .015f), "Classroom", .006f);
                    Box(inset, "Safety glazing divider", V(0, 0, -.04f), V(.012f, .32f, .014f), "Iron", .003f);
                }
                else if (identity == "Library")
                {
                    Box(inset, "Timber inset frame", Vector3.zero, V(.52f, .46f, .025f), "Wood", .018f);
                    var symbol = Group(inset, "Book medallion", V(0, 0, -.022f)); symbol.localScale = Vector3.one * .95f; DrawIcon(symbol, "Library");
                }
                else
                {
                    Box(inset, "Brass office inset", Vector3.zero, V(.50f, .45f, .027f), "Director", .025f);
                    var symbol = Group(inset, "Executive chair medallion", V(0, 0, -.025f)); DrawIcon(symbol, "Director");
                }
                Batch(inset, "Door inset", true);
            }
        }
        foreach (var sector in sectors) Batch(sector, "Wordless plaque finish", true);
    }

    static void Plaque(Transform p, string identity)
    {
        string finish = identity == "Classroom" || identity == "Library" || identity == "Director" ? identity : "Service";
        Box(p, "Rounded plaque rim", Vector3.zero, V(.68f, .46f, .032f), "Steel", .035f);
        Box(p, "Enamel plaque face", V(0, 0, -.025f), V(.62f, .40f, .018f), finish, .028f);
        foreach (float x in new[] { -.275f, .275f })
            Cylinder(p, "Plaque screw", V(x, 0, -.038f), .016f, .009f, "Steel", 12, Quaternion.Euler(90, 0, 0));
        var icon = Group(p, "Geometric pictogram - " + identity, V(0, 0, -.045f)); DrawIcon(icon, identity);
    }

    static void Line(Transform p, Vector2 a, Vector2 b, float width = .014f)
        => Pipe(p, V(a.x, a.y, 0), V(b.x, b.y, 0), width * .5f, "Icon");

    static void Outline(Transform p, params Vector2[] points)
    {
        for (int i = 1; i < points.Length; i++) Line(p, points[i - 1], points[i]);
    }

    static void DrawIcon(Transform p, string identity)
    {
        if (identity == "Classroom")
        {
            Outline(p, new Vector2(-.17f,.04f), new Vector2(-.17f,.15f), new Vector2(.17f,.15f), new Vector2(.17f,.04f), new Vector2(-.17f,.04f));
            Line(p, new Vector2(-.13f,-.04f), new Vector2(.13f,-.04f), .025f);
            foreach (float x in new[] { -.09f, .09f }) Line(p, new Vector2(x,-.04f), new Vector2(x,-.15f));
            Line(p, new Vector2(-.05f,-.10f), new Vector2(.05f,-.10f), .023f);
        }
        else if (identity == "Library")
        {
            Outline(p, new Vector2(-.18f,-.10f),new Vector2(-.18f,.10f),new Vector2(-.04f,.14f),new Vector2(0,.10f),new Vector2(.04f,.14f),new Vector2(.18f,.10f),new Vector2(.18f,-.10f),new Vector2(.04f,-.06f),new Vector2(0,-.10f),new Vector2(-.04f,-.06f),new Vector2(-.18f,-.10f));
            Line(p, new Vector2(0,.10f),new Vector2(0,-.10f));
            foreach (float y in new[] { -.03f, .025f, .075f })
            { Line(p,new Vector2(-.145f,y),new Vector2(-.04f,y+.025f),.008f); Line(p,new Vector2(.04f,y+.025f),new Vector2(.145f,y),.008f); }
        }
        else if (identity == "Director")
        {
            Outline(p, new Vector2(-.07f,-.025f),new Vector2(-.07f,.14f),new Vector2(.07f,.14f),new Vector2(.07f,-.025f));
            Line(p,new Vector2(-.11f,-.025f),new Vector2(.11f,-.025f),.024f);
            Line(p,new Vector2(0,-.03f),new Vector2(0,-.13f)); Line(p,new Vector2(-.09f,-.13f),new Vector2(.09f,-.13f));
            foreach (int side in new[] { -1, 1 })
                for (int i = 0; i < 5; i++)
                {
                    float y = -.10f + i * .045f, x = side * (.13f + Mathf.Sin(i / 4f * Mathf.PI) * .025f);
                    Line(p,new Vector2(x,y),new Vector2(x+side*.04f,y+.025f),.016f);
                }
        }
        else if (identity == "Electrical")
            Outline(p,new Vector2(.02f,.16f),new Vector2(-.09f,.01f),new Vector2(.015f,.01f),new Vector2(-.025f,-.15f),new Vector2(.10f,.055f),new Vector2(-.01f,.055f));
        else if (identity == "Storage")
        {
            Outline(p,new Vector2(-.13f,-.12f),new Vector2(-.13f,.10f),new Vector2(.13f,.10f),new Vector2(.13f,-.12f),new Vector2(-.13f,-.12f));
            Line(p,new Vector2(-.13f,.06f),new Vector2(.13f,.06f)); Line(p,new Vector2(0,.10f),new Vector2(0,-.12f));
        }
        else if (identity == "Washroom")
        {
            Cylinder(p,"Person head",V(0,.12f,0),.035f,.012f,"Icon",24,Quaternion.Euler(90,0,0));
            Line(p,new Vector2(0,.06f),new Vector2(0,-.055f),.045f);
            foreach(int side in new[]{-1,1}) { Line(p,new Vector2(0,.045f),new Vector2(side*.075f,-.02f)); Line(p,new Vector2(0,-.045f),new Vector2(side*.045f,-.15f)); }
        }
        else if (identity == "Entrance")
        {
            Outline(p,new Vector2(-.11f,-.13f),new Vector2(-.11f,.14f),new Vector2(.11f,.14f),new Vector2(.11f,-.13f));
            Line(p,new Vector2(-.18f,-.02f),new Vector2(.18f,-.02f),.022f);
            Line(p,new Vector2(.18f,-.02f),new Vector2(.10f,.045f),.018f); Line(p,new Vector2(.18f,-.02f),new Vector2(.10f,-.085f),.018f);
        }
        else
        {
            Line(p,new Vector2(-.10f,-.13f),new Vector2(.08f,.09f),.038f);
            Outline(p,new Vector2(.08f,.09f),new Vector2(.055f,.15f),new Vector2(.11f,.125f),new Vector2(.16f,.16f),new Vector2(.155f,.09f),new Vector2(.08f,.05f));
        }
    }

    static void Solid(Transform p, Vector3 center, Vector3 size)
    { var c = p.gameObject.AddComponent<BoxCollider>(); c.center = center; c.size = size; }

    static float BayWidth(int i)
    {
        float a = i * Mathf.PI / 24, h = Mathf.PI / 48;
        return Vector3.Distance(V(Mathf.Sin(a-h)*27.1f,0,Mathf.Cos(a-h)*19.1f),V(Mathf.Sin(a+h)*27.1f,0,Mathf.Cos(a+h)*19.1f)) + .16f;
    }

    static void FrontDoor()
    {
        var bay = atrium.Find("Service bay 0");
        var p = Group(bay, "Larger front entrance", Vector3.zero);
        float side = (BayWidth(0) - 2.70f) / 2;
        foreach(int sign in new[]{-1,1})
        {
            var flank = Group(p,"Entrance flank",V(sign*(1.35f+side/2),1.8f,-.03f));
            Box(flank,"Stone flank",Vector3.zero,V(side,3.6f,.42f),"Plaster",.014f); Solid(flank,Vector3.zero,V(side,3.6f,.42f));
            Box(p,"Wider door jamb",V(sign*1.37f,1.75f,-.15f),V(.10f,3.5f,.25f),"Steel",.015f);
            Solid(p,V(sign*1.37f,1.75f,-.15f),V(.10f,3.5f,.25f));
        }
        Box(p,"Entrance lintel",V(0,3.51f,-.15f),V(2.84f,.10f,.25f),"Steel",.018f);
        var rig = Group(p,"Interactive door - Front entrance",V(0,0,-.06f)); rig.localRotation=Quaternion.Euler(0,90,0);
        var trigger=rig.gameObject.AddComponent<BoxCollider>();trigger.center=V(0,1.8f,0);trigger.size=V(4.8f,3.6f,3.2f);trigger.isTrigger=true;
        var body=rig.gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        var leaf=Group(rig,"Door",Vector3.zero); Solid(leaf,V(0,1.735f,0),V(.16f,3.45f,2.60f));
        Box(leaf,"Wide reinforced entrance leaf",V(0,1.735f,0),V(.16f,3.45f,2.60f),"Iron",.025f);
        foreach(float z in new[]{-1.13f,1.13f})Box(leaf,"Raised stile",V(.105f,1.73f,z),V(.04f,3.25f,.075f),"Steel",.009f);
        foreach(float y in new[]{.18f,1.15f,2.40f,3.24f})Box(leaf,"Reinforcement rail",V(.11f,y,0),V(.045f,.065f,2.25f),"Steel",.010f);
        foreach(float z in new[]{-.62f,0f,.62f})
        {
            Box(leaf,"Recessed upper vision panel",V(.097f,2.76f,z),V(.035f,.55f,.48f),"Classroom",.015f);
            Box(leaf,"Vision panel safety bar",V(.13f,2.76f,z),V(.025f,.012f,.46f),"Steel",.004f);
        }
        Box(leaf,"Handle escutcheon",V(.13f,1.40f,1.02f),V(.045f,.31f,.13f),"Steel",.018f);
        Pipe(leaf,V(.20f,1.42f,.78f),V(.20f,1.42f,1.04f),.024f,"Steel");
        Batch(leaf,"Front entrance finish",true);
        var door=rig.gameObject.AddComponent<OpenDoorWithKey>();var so=new SerializedObject(door);
        so.FindProperty("door").objectReferenceValue=leaf;so.FindProperty("hingePoint").vector3Value=V(0,0,-.5f);
        so.FindProperty("openAngle").floatValue=100;so.FindProperty("animationDuration").floatValue=.8f;so.FindProperty("closeDelay").floatValue=.8f;
        so.ApplyModifiedPropertiesWithoutUndo();
        var plaque=Group(p,"Wordless Entrance plaque",V(0,3.78f,-.34f));Plaque(plaque,"Entrance");
        // Close only the adjacent former entrance opening; the central route now passes through the wide leaf.
        var adjacent=atrium.Find("Service bay 1");var infill=Group(adjacent,"Closed secondary entrance opening",Vector3.zero);
        Box(infill,"Institutional infill",V(0,1.8f,-.03f),V(BayWidth(1),3.6f,.42f),"Plaster",.015f);
        Solid(infill,V(0,1.8f,-.03f),V(BayWidth(1),3.6f,.42f));
        Box(infill,"Inset empty notice panel",V(0,2.25f,-.27f),V(1.3f,.75f,.025f),"Wood",.012f);
        leaf.gameObject.SetActive(false);
        Batch(p,"Front entrance frame",true);
        leaf.gameObject.SetActive(true);
        Batch(infill,"Infill finish",true);
    }

    static void Lathe(Transform parent, string name, Vector2[] profile, string material, int sides)
    {
        string key = name.Replace(' ', '_');
        if (!shapes.TryGetValue(key, out var mesh))
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (int row = 0; row < profile.Length; row++) for (int i = 0; i <= sides; i++)
            {
                float a = i * 2 * Mathf.PI / sides; vertices.Add(V(Mathf.Cos(a) * profile[row].x, profile[row].y, Mathf.Sin(a) * profile[row].x));
                uv.Add(new Vector2(i / (float)sides, row / (float)(profile.Length - 1)));
            }
            for (int row = 0; row < profile.Length - 1; row++) for (int i = 0; i < sides; i++)
            {
                int a = row * (sides + 1) + i, b = a + sides + 1;
                indices.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 });
            }
            mesh = new Mesh { name = key }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Output + "/" + key + ".asset"); shapes[key] = mesh;
        }
        var t = Group(parent, name, Vector3.zero); t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mats[material];
    }

    static void Torus(Transform parent, string name, Vector3 center, float radius, float tube, string material, Quaternion rotation, int sides)
    {
        string key = name.Replace(' ', '_') + "_" + radius.ToString("F3",System.Globalization.CultureInfo.InvariantCulture) + "_" + tube.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
        if (!shapes.TryGetValue(key, out var mesh))
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>(); const int ring = 8;
            for (int i = 0; i <= sides; i++) for (int j = 0; j <= ring; j++)
            {
                float a = i * Mathf.PI * 2 / sides, b = j * Mathf.PI * 2 / ring;
                var radial = V(Mathf.Cos(a), Mathf.Sin(a), 0); var n = radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                vertices.Add(radial * radius + n * tube); normals.Add(n); uv.Add(new Vector2(i / (float)sides, j / (float)ring));
            }
            for (int i = 0; i < sides; i++) for (int j = 0; j < ring; j++)
            {
                int a = i * (ring + 1) + j, b = a + ring + 1; indices.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 });
            }
            mesh = new Mesh { name = key }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Output + "/" + key + ".asset"); shapes[key] = mesh;
        }
        var t = Group(parent, name, center); t.localRotation = rotation; t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mats[material];
    }
}
