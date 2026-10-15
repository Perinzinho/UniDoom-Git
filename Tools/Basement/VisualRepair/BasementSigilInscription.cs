using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Editor-only authoring tool. Vector strokes transcribed from the supplied alphabet.
// No font, TextMesh, runtime script, collider or extra light is used by the inscriptions.
public static class BasementSigilInscription
{
    const string Art = "Assets/Art/Basement/SigilInscription";
    static readonly string[] Prefabs = {
        "Assets/Art/Basement/BasementFinalSanctum.prefab",
        "Assets/Art/Basement/BasementFinalArena.prefab",
        "Assets/prefabs/Scenario/Basement.prefab"
    };
    static readonly string[] Phrases = {
        "Abra sua percepção a uma infinidade superior",
        "A sua mente torna-se invólucro inerte para poderes que te formam",
        "Descubra e perceba o real propósito da sua própria entidade"
    };
    // Coordinates use the supplied chart's orientation: x right, y down, 0..100.
    // Each semicolon starts a separate pen stroke. Curves are sampled polylines.
    static readonly Dictionary<char, string> Alphabet = new Dictionary<char, string> {
        {'A', "68,7 14,33 10,40 84,50;67,7 64,88;18,76 24,64 40,58 59,58 78,67 86,79;12,89 33,69 50,64 73,76;28,83 46,77 66,86"},
        {'B', "69,7 51,8 33,14 25,24 32,34 61,49 73,61 74,72 65,84 52,91 38,89 27,81 29,68 44,52 66,29 75,17 69,7;32,24 48,22 65,15"},
        {'C', "52,9 58,22 73,34 86,35 91,26 84,13;43,16 48,33 62,48 77,54 91,48;25,22 30,40 45,52 65,59;15,40 24,53 44,58 60,63 66,78;23,67 41,68 52,77 56,92;47,46 40,38 35,21;73,53 77,70 88,82;39,73 32,85 19,90"},
        {'D', "18,16 30,9 73,10 83,18 83,58 75,63;41,12 41,58 32,64 19,58 18,44;63,14 63,70 76,78 87,76;26,27 53,26 76,31;11,78 26,70 43,75 57,87 72,89;24,87 39,81 54,87"},
        {'E', "34,8 35,93;30,21 54,14 59,21 53,28 35,29;35,38 61,33 65,44 55,52 35,49;35,61 56,57 66,67 58,75 36,72;22,17 22,30;24,43 23,55;26,75 27,87"},
        {'F', "13,35 24,59 39,61 51,46;40,14 40,77 45,90;61,12 59,63 68,71 79,64 88,39;13,68 32,71 51,64 66,52;25,22 30,46;74,20 71,44"},
        {'G', "68,9 78,18 78,40 67,54 53,56 46,50 52,40 68,37;37,14 43,22 42,35 31,46 17,43 13,35 21,27 34,28;23,52 39,55 48,68 46,79 33,87 18,82 16,73;53,70 70,73 83,64 88,44;56,84 73,83 82,76;28,61 28,74"},
        {'H', "31,12 57,5 80,10;23,26 76,25;30,27 27,91;30,44 70,43 69,87 60,94;30,60 64,59;29,76 65,75"},
        {'I', "52,5 52,95;18,33 32,22 62,22 79,32 81,46 66,56 35,56 19,47 18,33;27,38 43,32 62,34 71,42 62,48 44,47 34,40;43,16 42,81;28,68 47,62 67,67 72,76 58,85 42,82"},
        {'J', "50,7 48,91;15,28 77,27;22,17 70,17;27,28 25,60 61,60 74,49 73,39 32,39;32,50 60,49;22,74 63,69 69,79 58,89;34,9 33,81"},
        {'K', "7,73 46,13 84,77;18,61 76,61;21,46 68,46;33,30 57,30;12,77 87,77;28,16 61,82;14,31 80,67;13,62 80,29;39,87 48,65 61,88"},
        {'L', "16,12 32,36 25,64 13,74;33,10 40,39 39,62 53,86;52,15 44,44 48,57 68,67 79,62;69,10 61,38 63,52;27,29 22,45;22,64 37,72"},
        {'M', "19,13 40,12 62,16 76,12;32,25 66,25;28,40 42,40 43,66 50,71 60,62 63,36;25,48 23,81 32,89 42,86;60,64 61,85 72,91 83,82"},
        {'N', "12,30 27,45 47,48 70,33 87,30;23,23 31,34 35,65 29,81;45,12 45,90;61,18 57,44 61,69 72,83;21,53 49,62 75,52;23,72 47,80 69,69;30,89 64,90"},
        {'O', "58,9 43,14 42,45;24,48 39,38 54,42 58,56 54,71 41,77 28,73 21,63 24,48;45,40 46,87 53,94;65,18 66,71 76,82 86,80;32,21 70,18"},
        {'P', "17,12 25,44 34,51;39,7 36,44 32,59;64,8 53,38 44,55;83,16 67,45 54,58;44,47 47,91;18,56 35,64 55,64 78,49;32,80 48,76 63,84"},
        {'Q', "11,36 31,29 50,39 70,29 89,33 71,62 53,67 34,56 11,36;29,34 30,49 43,53 53,40;66,34 62,52 73,51;52,62 64,79 76,89;27,70 40,66"},
        {'R', "27,22 14,40 23,56 46,69 60,54 83,24;45,9 29,34 34,57 56,77 69,90;68,8 52,33 53,55 36,77;21,61 37,47 62,48 85,61"},
        {'S', "19,36 40,13 30,44 13,58 23,65;69,13 58,42 40,66 37,89;91,29 72,46 62,65 70,72 84,61;28,72 48,54 69,52;46,17 41,39"},
        {'T', "68,8 50,5 32,13;23,25 63,22 77,28;28,38 54,34 70,39 73,50 62,60 44,61 33,53 37,44 57,45 63,50;53,59 52,89;30,70 35,88 45,94 57,91;20,38 16,48"},
        {'U', "22,10 41,19 62,13;15,29 30,38 49,32 66,21 80,26;25,35 22,77 30,88;46,31 51,50 48,83;74,31 68,47 70,83;22,49 40,43 64,49;36,7 37,30"},
        {'V', "21,14 11,53 30,73 55,78 82,61 87,28;32,23 27,48 41,64 64,67 74,50 68,19;36,13 55,43 76,49;14,61 36,57 55,75 78,84;32,46 43,34 57,63"},
        {'W', "8,41 73,14 85,66 48,80 8,41;23,38 44,43 69,70;38,30 38,70;57,23 58,79;16,57 79,46;73,14 70,89"},
        {'X', "28,8 46,23 70,14 80,23 73,40 58,44 39,33 28,8;20,43 41,51 64,68 68,89;39,29 36,67 27,90;76,18 59,49 51,78;15,66 36,58 63,61 82,72"},
        {'Y', "17,12 25,37 39,42 49,27 59,11;29,28 30,74 25,90;50,31 49,84;77,14 71,48 80,64 88,60;16,59 43,50 70,57;39,72 60,68 70,88"},
        {'Z', "15,24 38,10 56,14 76,29 85,49;25,30 21,69 11,83;41,19 40,79;57,23 56,72 66,83;74,31 70,64 80,79;12,82 35,75 60,81 87,79;31,92 57,89"}
    };
    static string Normalize(string phrase)
    {
        var result = new StringBuilder();
        foreach (char c in phrase.ToUpperInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c >= 'A' && c <= 'Z') result.Append(c);
            else if (c == ' ' || c == '-') result.Append(' ');
            else throw new Exception("Unsupported inscription character: " + c);
        }
        return result.ToString().Trim();
    }
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Mesh Store(string name, Mesh mesh)
    {
        mesh.name = name;
        string path = Art + "/Meshes/" + name + ".asset";
        var previous = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!previous) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, previous); Object.DestroyImmediate(mesh); return previous;
    }
    static void Path(ChaosGeometry.Builder builder, Vector3[] points, float width, int seed)
    {
        var offsets = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var before = Vector3.Cross(Vector3.up, (points[Math.Max(0, i - 1)] - points[i]).normalized);
            var after = Vector3.Cross(Vector3.up, (points[i] - points[Math.Min(points.Length - 1, i + 1)]).normalized);
            if (i == 0) before = after;
            if (i == points.Length - 1) after = before;
            var normal = (before + after).normalized;
            offsets[i] = normal * (width * .5f * (1 + .035f * Mathf.Sin(i * 2.4f + seed)) / Mathf.Max(.55f, Vector3.Dot(normal, after)));
        }
        for (int i = 0; i < points.Length - 1; i++)
            builder.Face(points[i] - offsets[i], points[i] + offsets[i], points[i + 1] + offsets[i + 1], points[i + 1] - offsets[i + 1]);
    }
    static void Ring(ChaosGeometry.Builder builder, Vector3 center, float radius, float width)
    {
        for (int i = 0; i < 256; i++)
        {
            float a = i * Mathf.PI / 128, b = (i + 1) * Mathf.PI / 128;
            builder.Face(center + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * (radius - width / 2),
                center + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * (radius + width / 2),
                center + V(Mathf.Sin(b), 0, Mathf.Cos(b)) * (radius + width / 2),
                center + V(Mathf.Sin(b), 0, Mathf.Cos(b)) * (radius - width / 2));
        }
    }
    static void Glyph(ChaosGeometry.Builder builder, char letter, Vector3 origin, Vector3 right, Vector3 up, float width, float height, int seed)
    {
        foreach (string stroke in Alphabet[letter].Split(';'))
        {
            var points = stroke.Split(' ').Select(pair => {
                var xy = pair.Split(',');
                float x = float.Parse(xy[0], CultureInfo.InvariantCulture) / 100 - .5f;
                float y = .5f - float.Parse(xy[1], CultureInfo.InvariantCulture) / 100;
                return origin + right * (x * width) + up * (y * height);
            }).ToArray();
            Path(builder, points, height * .042f, seed++);
        }
    }
    static Mesh[] Seal(string prefix, float radius, Vector3 center)
    {
        var pigment = new ChaosGeometry.Builder(); var chalk = new ChaosGeometry.Builder();
        // Restore the repaired pentagram's proportions, with one inscription band.
        float inner = 3.02f / 3.25f, star = 2.68f / 3.25f;
        Ring(pigment, center, radius, radius * (.095f / 3.25f));
        Ring(pigment, center + Vector3.up * .0004f, radius * inner, radius * (.045f / 3.25f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2 / 5, b = (i + 2) * Mathf.PI * 2 / 5;
            var p = center + V(Mathf.Sin(a) * radius * star, .0008f + i * .00025f, Mathf.Cos(a) * radius * star);
            var q = center + V(Mathf.Sin(b) * radius * star, .0008f + i * .00025f, Mathf.Cos(b) * radius * star);
            float halfWidth = radius * (.052f / 3.25f);
            Path(pigment, new[] { p, q }, halfWidth * 2, i);
            // Close the tips where the five separate strokes meet.
            var tip = center + V(Mathf.Sin(a) * radius * star, .0022f, Mathf.Cos(a) * radius * star);
            for (int j = 0; j < 16; j++)
            {
                float u = j * Mathf.PI / 8, v = (j + 1) * Mathf.PI / 8;
                pigment.Face(tip, tip + V(Mathf.Sin(u) * halfWidth, 0, Mathf.Cos(u) * halfWidth),
                    tip + V(Mathf.Sin(v) * halfWidth, 0, Mathf.Cos(v) * halfWidth));
            }
        }
        // Keep all three requested phrases, sequentially on the same circular row.
        // The longer gap distinguishes a phrase boundary from a word boundary.
        string phrase = string.Join("   ", Phrases.Select(Normalize));
        float r = radius * (inner + 1) * .5f;
        float total = phrase.Sum(c => c == ' ' ? .68f : 1f);
        float angleUnit = 345f * Mathf.Deg2Rad / total;
        float height = Mathf.Min(radius * (1 - inner) * .78f, r * angleUnit * .95f);
        float width = Mathf.Min(height * .91f, r * angleUnit * .85f);
        float cursor = 0;
        foreach (char c in phrase)
        {
            float advance = c == ' ' ? .68f : 1f;
            float angle = Mathf.PI - (7.5f * Mathf.Deg2Rad + (cursor + advance / 2) * angleUnit);
            var radial = V(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            var right = V(-Mathf.Cos(angle), 0, Mathf.Sin(angle));
            if (c != ' ') Glyph(chalk, c, center + radial * r + Vector3.up * .003f, right, -radial, width, height, (int)cursor);
            cursor += advance;
        }
        Path(pigment, new[] { center + V(0, .002f, -r + height * .18f),
            center + V(0, .002f, -r - height * .18f) }, radius * .009f, 0);
        return new[] { Store(prefix + "_Contours", pigment.Mesh(prefix + " contours")), Store(prefix + "_Phrases", chalk.Mesh(prefix + " phrases")) };
    }
    static Mesh TrimFinal(Mesh source, string name)
    {
        var vertices = source.vertices; var triangles = source.triangles; var keep = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
            var centroid = (a + b + c) / 3;
            bool floor = Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < .09f;
            bool oldSeal = new Vector2(centroid.x, centroid.z - 18.5f).magnitude < 7.22f;
            if (!floor || !oldSeal) keep.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
        }
        var mesh = Object.Instantiate(source); mesh.triangles = keep.ToArray(); mesh.RecalculateBounds();
        return Store(name, mesh);
    }
    static void Assign(Transform parent, string name, Mesh mesh, string material)
    {
        var child = parent.Find(name);
        if (!child) { child = new GameObject(name).transform; child.SetParent(parent, false); }
        var filter = child.GetComponent<MeshFilter>();
        if (!filter) filter = child.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = child.GetComponent<MeshRenderer>();
        if (!renderer) renderer = child.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Basement/VisualRepair/Materials/" + material + ".mat");
        if (!renderer.sharedMaterial) throw new Exception("Missing shared paint material");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
    public static void Build()
    {
        BasementSurfaceBuilder.RequireIsolated();
        Directory.CreateDirectory(Art + "/Meshes"); AssetDatabase.Refresh();
        var preparation = Seal("Preparation", 3.25f, V(0, .012f, 2.4f));
        // Keep the final room's original clearance above its raised stone relief.
        var sanctum = Seal("Sanctum", 7.15f, V(0, .055f, 18.5f));
        foreach (string path in Prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (path == Prefabs[0])
                {
                    var old = root.transform.Find("Expanded sacrificial seal");
                    foreach (string suffix in new[] { "Oxidized_ritual_blood", "Scraped_bone_chalk" })
                    {
                        var filter = old.Find("Sanctum_" + suffix).GetComponent<MeshFilter>();
                        var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Basement/RitualExpansion/Meshes/Sanctum_" + suffix + ".asset");
                        filter.sharedMesh = TrimFinal(source, "Sanctum_" + suffix + "_WithoutOldSeal");
                    }
                    var group = old.Find("Three encoded ritual circles");
                    if (!group) { group = new GameObject("Three encoded ritual circles").transform; group.SetParent(old, false); }
                    Assign(group, "Contours", sanctum[0], "RitualPigment"); Assign(group, "Phrases", sanctum[1], "RitualChalk");
                }
                else
                {
                    var group = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Restored floor seal");
                    Assign(group, "RitualPigment", preparation[0], "RitualPigment"); Assign(group, "RitualChalk", preparation[1], "RitualChalk");
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("../BasementSingleSigilCircle/phrases.txt", Phrases.Select((p, i) => (i + 1) + ": " + p + "\n" + Normalize(p)));
        Debug.Log("BASEMENT_SIGILS_BUILT");
    }
    static void Render(Transform room, Transform coordinates, Vector3 eye, Vector3 look, string name, bool flashlight)
    {
        typeof(BasementSurfaceBuilder).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { room, coordinates, eye, look, "Sigils-" + name, flashlight });
    }
    public static void Capture()
    {
        BasementSurfaceBuilder.RequireIsolated();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); ShaderUtil.allowAsyncCompilation = false;
        var room = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t => t.name == "Basement" && !t.parent);
        var prep = room.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Restored floor seal");
        var final = room.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Expanded sacrificial seal");
        foreach (bool isFinal in new[] { false, true })
        {
            var frame = isFinal ? final : prep; var center = isFinal ? V(0, .055f, 18.5f) : V(0, .012f, 2.4f);
            float r = isFinal ? 7.15f : 3.25f; string label = isFinal ? "Sanctum" : "Preparation";
            Render(room, frame, center + V(0, r * 1.55f, -.001f), center, label + "-Overhead", true);
            Render(room, frame, center + V(-r * .4f, 1.84f, -r - 1.5f), center, label + "-Player", true);
            Render(room, frame, center + V(0, 1.4f, -r * .92f), center + V(0, 0, -r * .76f), label + "-Detail", true);
        }
        Debug.Log("BASEMENT_SIGILS_CAPTURED");
    }
    public static void Verify()
    {
        BasementSurfaceBuilder.RequireIsolated();
        foreach (string path in Prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var inscription = path == Prefabs[0] ? root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Three encoded ritual circles")
                    : root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Restored floor seal");
                var filters = inscription.GetComponentsInChildren<MeshFilter>();
                if (filters.Length != 2 || inscription.GetComponentsInChildren<Collider>().Length != 0) throw new Exception("Inscription structure invalid");
                foreach (var f in filters)
                {
                    if (!f.sharedMesh || !AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Art)) throw new Exception("Wrong inscription mesh");
                    if (f.sharedMesh.normals.Any(n => n.y < .98f)) throw new Exception("Paint faces below floor: " + f.name);
                }
                if (root.GetComponentsInChildren<TextMesh>(true).Any(t => !string.IsNullOrEmpty(t.text))) throw new Exception("Ordinary lettering restored");
                Debug.Log("BASEMENT_SIGILS_PREFAB_OK " + path + " " + filters.Sum(f => f.sharedMesh.triangles.Length / 3) + " triangles");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Capture(); BasementVisualRepair.Verify();
        Debug.Log("BASEMENT_SIGILS_VERIFIED");
    }
}
