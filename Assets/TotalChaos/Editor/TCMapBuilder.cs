using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace TotalChaosMaps.EditorTools
{
    // ======================================================================
    //  Layout (JSON)  —  veja SKILL.md para a legenda dos caracteres do grid
    // ======================================================================
    [Serializable]
    public class TCMapLayout
    {
        public string name = "TC_Map";
        public string theme = "apartments";   // apartments | tunnels | streets | facility
        public float cellSize = 2f;           // metros por célula (2 = corredor claustrofóbico)
        public float wallHeight = 3f;
        public float doorHeight = 2.3f;
        public bool ceiling = true;
        public bool autoShell = true;         // vazio ao lado de área andável vira parede
        public float textureMeters = 3f;      // quantos metros uma repetição de textura cobre
        public int seed = 1337;
        public string[] grid;
    }

    // ======================================================================
    //  Grid com padding de 1 célula (espelha scripts/tc_layout.py)
    // ======================================================================
    class TCGrid
    {
        public readonly int cols, rows, origRows;
        readonly char[,] c; // [x, r]

        public const string Markers = "PEILCOXabcdefghi";

        public TCGrid(string[] lines, bool shell)
        {
            origRows = lines.Length;
            int oc = 0;
            foreach (var l in lines) oc = Mathf.Max(oc, l.Length);
            cols = oc + 2;
            rows = origRows + 2;
            c = new char[cols, rows];
            for (int r = 0; r < rows; r++) for (int x = 0; x < cols; x++) c[x, r] = ' ';
            for (int r = 0; r < lines.Length; r++)
                for (int x = 0; x < lines[r].Length; x++)
                    c[x + 1, r + 1] = lines[r][x];
            if (shell) ApplyShell();
        }

        public char At(int x, int r) => (x < 0 || r < 0 || x >= cols || r >= rows) ? ' ' : c[x, r];

        public static bool Walkable(char ch) => ch != '#' && ch != ' ';
        public static bool IsDoor(char ch) => ch == 'D' || (ch >= '1' && ch <= '9');
        public static bool IsMarker(char ch) => Markers.IndexOf(ch) >= 0;

        void ApplyShell()
        {
            var add = new List<Vector2Int>();
            for (int r = 0; r < rows; r++)
                for (int x = 0; x < cols; x++)
                {
                    if (c[x, r] != ' ') continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1 && !near; dx++)
                            if ((dx != 0 || dy != 0) && Walkable(At(x + dx, r + dy))) near = true;
                    if (near) add.Add(new Vector2Int(x, r));
                }
            foreach (var p in add) c[p.x, p.y] = '#';
        }

        // célula ao ar livre: '_' ou marcador cercado por '_' (>= 2 vizinhos)
        public bool IsOutdoor(int x, int r)
        {
            char ch = At(x, r);
            if (ch == '_') return true;
            if (!IsMarker(ch)) return false;
            int n = 0;
            if (At(x + 1, r) == '_') n++;
            if (At(x - 1, r) == '_') n++;
            if (At(x, r + 1) == '_') n++;
            if (At(x, r - 1) == '_') n++;
            return n >= 2;
        }
    }

    // ======================================================================
    //  Acumulador de malha: caixas com UV em coordenadas de mundo (textura não estica)
    // ======================================================================
    class MeshAcc
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> nrm = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();

        public bool Empty => v.Count == 0;

        public void Box(Vector3 mn, Vector3 mx, float s)
        {
            float x0 = mn.x, y0 = mn.y, z0 = mn.z, x1 = mx.x, y1 = mx.y, z1 = mx.z;
            Quad(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), Vector3.up, 2, s);      // +Y
            Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), Vector3.down, 2, s);    // -Y
            Quad(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0), Vector3.back, 1, s);    // -Z
            Quad(new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1), Vector3.forward, 1, s); // +Z
            Quad(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0), Vector3.left, 0, s);    // -X
            Quad(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1), Vector3.right, 0, s);   // +X
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, int mode, float s)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) nrm.Add(n);
            uv.Add(UV(a, mode, s)); uv.Add(UV(b, mode, s)); uv.Add(UV(c, mode, s)); uv.Add(UV(d, mode, s));
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        static Vector2 UV(Vector3 p, int mode, float s)
        {
            if (mode == 0) return new Vector2(p.z / s, p.y / s);
            if (mode == 1) return new Vector2(p.x / s, p.y / s);
            return new Vector2(p.x / s, p.z / s);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(nrm);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    // ======================================================================
    //  Builder
    // ======================================================================
    public static class TCMapBuilder
    {
        const string ExampleJson = @"{
  ""name"": ""TC_Exemplo_Minimo"", ""theme"": ""apartments"", ""cellSize"": 2.0, ""wallHeight"": 3.0, ""doorHeight"": 2.3,
  ""grid"": [
    ""P..... ...L.."",
    ""..L.C.D.....E"",
    ""...... ..O..."",
    "".I.... ......"",
    ""         D   "",
    ""       ...L.."",
    ""       .E...."",
    ""       ....X.""
  ]
}";

        [MenuItem("Tools/Total Chaos Maps/Build Map From JSON...")]
        static void BuildFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Layout JSON do Total Chaos Maps", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            Build(JsonUtility.FromJson<TCMapLayout>(File.ReadAllText(path)));
        }

        [MenuItem("Tools/Total Chaos Maps/Build Example Map (smoke test)")]
        static void BuildExample() => Build(JsonUtility.FromJson<TCMapLayout>(ExampleJson));

        static Color KeyColor(int n)
        {
            switch (n)
            {
                case 1: return new Color32(176, 48, 48, 255);
                case 2: return new Color32(48, 96, 176, 255);
                case 3: return new Color32(200, 170, 40, 255);
                case 4: return new Color32(60, 150, 70, 255);
                default: return new Color32(130, 70, 160, 255);
            }
        }

        static void Runs(TCGrid g, Func<int, int, bool> pred, Action<int, int, int> emit)
        {
            for (int r = 0; r < g.rows; r++)
            {
                int x = 0;
                while (x < g.cols)
                {
                    if (!pred(x, r)) { x++; continue; }
                    int s = x;
                    while (x < g.cols && pred(x, r)) x++;
                    emit(s, x - 1, r);
                }
            }
        }

        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Build(TCMapLayout L)
        {
            if (L == null || L.grid == null || L.grid.Length == 0)
            {
                Debug.LogError("[TC] Layout vazio ou JSON inválido (campo 'grid' ausente).");
                return null;
            }

            var theme = TCTheme.Get(L.theme);
            var g = new TCGrid(L.grid, L.autoShell);
            float cs = L.cellSize, wh = L.wallHeight;
            float dh = Mathf.Min(L.doorHeight, wh - 0.1f);
            float tm = Mathf.Max(0.5f, L.textureMeters);
            var rng = new Random(L.seed);

            // coordenadas de mundo: norte (linha 0 do JSON) = +Z, padding de 1 célula descontado
            Func<int, float> WX = x => (x - 1) * cs;
            Func<int, float> WZ = r => (L.grid.Length - r) * cs;
            Func<int, int, Vector3> Center = (x, r) => new Vector3(WX(x) + cs * 0.5f, 0f, WZ(r) + cs * 0.5f);

            var old = GameObject.Find(L.name);
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(L.name);
            Undo.RegisterCreatedObjectUndo(root, "Build Total Chaos map");

            // ---------- materiais ----------
            var wallMat = TCAssets.Surface("Wall_" + theme.id, theme.wall, 11, 0.9f, 0.5f, true);
            var floorMat = TCAssets.Surface("Floor_" + theme.id, theme.floor, 23, 0.4f, 0.8f, true);
            var ceilMat = TCAssets.Surface("Ceil_" + theme.id, theme.ceiling, 37, 0.7f, 0.9f, false);
            var doorMat = TCAssets.Surface("Door_" + theme.id, new Color32(75, 64, 51, 255), 51, 0.6f, 0.9f, false);
            var crateMat = TCAssets.Surface("Crate", new Color32(90, 74, 53, 255), 61, 0.3f, 0.8f, false);
            var barrelMat = TCAssets.Surface("Barrel", new Color32(106, 61, 40, 255), 71, 0.8f, 0.8f, false);
            var metalMat = TCAssets.Surface("Metal", new Color32(42, 44, 43, 255), 81, 0.9f, 0.6f, false);
            var lampMat = TCAssets.Glow("Lamp_" + theme.id, theme.lamp);

            // ---------- geometria estática (uma malha por superfície) ----------
            var wallAcc = new MeshAcc();
            var floorAcc = new MeshAcc();
            var ceilAcc = new MeshAcc();

            Runs(g, (x, r) => g.At(x, r) == '#',
                (s, e, r) => wallAcc.Box(new Vector3(WX(s), -0.2f, WZ(r)), new Vector3(WX(e) + cs, wh + 0.2f, WZ(r) + cs), tm));

            Runs(g, (x, r) => TCGrid.Walkable(g.At(x, r)),
                (s, e, r) => floorAcc.Box(new Vector3(WX(s), -0.2f, WZ(r)), new Vector3(WX(e) + cs, 0f, WZ(r) + cs), tm));

            if (L.ceiling)
                Runs(g, (x, r) => TCGrid.Walkable(g.At(x, r)) && !g.IsOutdoor(x, r),
                    (s, e, r) => ceilAcc.Box(new Vector3(WX(s), wh, WZ(r)), new Vector3(WX(e) + cs, wh + 0.2f, WZ(r) + cs), tm));

            // lintel acima das portas
            Runs(g, (x, r) => TCGrid.IsDoor(g.At(x, r)),
                (s, e, r) => wallAcc.Box(new Vector3(WX(s), dh, WZ(r)), new Vector3(WX(e) + cs, wh + 0.2f, WZ(r) + cs), tm));

            var geo = Child(root.transform, "Geometry");
            Action<string, MeshAcc, Material> makeMesh = (n, acc, mat) =>
            {
                if (acc.Empty) return;
                var mesh = acc.ToMesh(L.name + "_" + n);
                TCAssets.SaveMesh(mesh);
                var go = new GameObject(n);
                go.transform.SetParent(geo, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.isStatic = true;
            };
            makeMesh("Walls", wallAcc, wallMat);
            makeMesh("Floor", floorAcc, floorMat);
            makeMesh("Ceiling", ceilAcc, ceilMat);

            // ---------- objetos por célula ----------
            var doorsT = Child(root.transform, "Doors");
            var lightsT = Child(root.transform, "Lights");
            var markersT = Child(root.transform, "Markers");
            var propsT = Child(root.transform, "Props");
            int nLights = 0, nDoors = 0, nMarkers = 0;

            Action<TCMarkerType, string, Vector3> marker = (type, label, pos) =>
            {
                var m = new GameObject(string.IsNullOrEmpty(label) ? type.ToString() : type + "_" + label);
                m.transform.SetParent(markersT, false);
                m.transform.position = pos + Vector3.up * 0.1f;
                var mk = m.AddComponent<TCMapMarker>();
                mk.type = type;
                mk.label = label;
                nMarkers++;
            };

            for (int r = 0; r < g.rows; r++)
            {
                for (int x = 0; x < g.cols; x++)
                {
                    char ch = g.At(x, r);
                    if (!TCGrid.Walkable(ch) || ch == '.' || ch == '_') continue;
                    Vector3 ctr = Center(x, r);

                    if (TCGrid.IsDoor(ch))
                    {
                        bool locked = ch != 'D';
                        bool alongZ = g.At(x - 1, r) == '#' && g.At(x + 1, r) == '#'; // passagem N-S => painel largo em X
                        Vector3 size = alongZ ? new Vector3(cs, dh, 0.25f) : new Vector3(0.25f, dh, cs);
                        var mat = locked
                            ? TCAssets.Surface("DoorLocked_" + ch, KeyColor(ch - '0'), 51, 0.6f, 0.9f, false)
                            : doorMat;
                        var d = Prim(PrimitiveType.Cube, locked ? "LockedDoor_" + ch : "Door", doorsT,
                                     ctr + Vector3.up * (dh * 0.5f), size, mat, true);
                        var rb = d.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        var door = d.AddComponent<TCSimpleDoor>();
                        door.keyId = locked ? ch.ToString() : "";
                        door.openHeight = dh;
                        nDoors++;
                    }
                    else if (ch == 'P') marker(TCMarkerType.PlayerStart, "", ctr);
                    else if (ch == 'E') marker(TCMarkerType.EnemySpawn, "", ctr);
                    else if (ch == 'I') marker(TCMarkerType.ItemSpawn, "", ctr);
                    else if (ch == 'X') marker(TCMarkerType.Exit, "", ctr);
                    else if (ch >= 'a' && ch <= 'i')
                    {
                        int n = ch - 'a' + 1;
                        marker(TCMarkerType.ItemSpawn, "key_" + n, ctr);
                        Prim(PrimitiveType.Cube, "KeyVisual_" + n, propsT, ctr + Vector3.up * 0.9f,
                             new Vector3(0.18f, 0.28f, 0.05f), TCAssets.Glow("Key" + n, KeyColor(n)), false);
                    }
                    else if (ch == 'C')
                    {
                        float sx = 0.8f + (float)rng.NextDouble() * 0.3f, sy = 0.7f + (float)rng.NextDouble() * 0.4f, sz = 0.8f + (float)rng.NextDouble() * 0.3f;
                        var cr = Prim(PrimitiveType.Cube, "Crate", propsT, ctr + Vector3.up * (sy * 0.5f), new Vector3(sx, sy, sz), crateMat, true);
                        cr.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 40f - 20f, 0f);
                        cr.isStatic = true;
                    }
                    else if (ch == 'O')
                    {
                        var b = Prim(PrimitiveType.Cylinder, "Barrel", propsT, ctr + Vector3.up * 0.5f, new Vector3(0.7f, 0.5f, 0.7f), barrelMat, true);
                        b.isStatic = true;
                    }
                    else if (ch == 'L')
                    {
                        bool outdoor = g.IsOutdoor(x, r);
                        float poleH = wh + 0.4f;
                        float ly;
                        if (outdoor)
                        {
                            Prim(PrimitiveType.Cylinder, "StreetLampPole", propsT, ctr + Vector3.up * (poleH * 0.5f),
                                 new Vector3(0.14f, poleH * 0.5f, 0.14f), metalMat, true).isStatic = true;
                            Prim(PrimitiveType.Cube, "StreetLampHead", propsT, ctr + Vector3.up * (poleH + 0.05f),
                                 new Vector3(0.5f, 0.1f, 0.3f), lampMat, false);
                            ly = poleH - 0.1f;
                        }
                        else
                        {
                            Prim(PrimitiveType.Cube, "CeilingLamp", propsT, ctr + Vector3.up * (wh - 0.04f),
                                 new Vector3(0.6f, 0.08f, 0.3f), lampMat, false);
                            ly = wh - 0.35f;
                        }

                        var lg = new GameObject("Light");
                        lg.transform.SetParent(lightsT, false);
                        lg.transform.position = ctr + Vector3.up * ly;
                        var li = lg.AddComponent<Light>();
                        li.type = LightType.Point;
                        li.color = theme.lamp;
                        li.intensity = theme.lightIntensity * (0.85f + 0.3f * (float)rng.NextDouble());
                        li.range = theme.lightRange * (outdoor ? 1.4f : 1f);
                        li.shadows = LightShadows.None; // ative sombras só nas luzes-chave (custo alto)
                        if (rng.NextDouble() < theme.flickerChance) lg.AddComponent<TCFlickerLight>();
                        nLights++;
                    }
                }
            }

            TCAtmosphere.Apply(theme);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log($"[TC] Mapa '{L.name}' gerado: {g.cols - 2}x{g.rows - 2} células, {nLights} luzes, {nDoors} portas, {nMarkers} marcadores. " +
                      "Próximos passos: posicionar o jogador em PlayerStart, bake do NavMesh e ajustar luzes.");
            return root;
        }
    }
}
