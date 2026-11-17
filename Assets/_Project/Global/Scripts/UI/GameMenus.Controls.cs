using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniDoom.UI
{
    public sealed partial class GameMenus
    {
        static readonly Color ControlLine = new(.93f, .55f, .22f, .9f);
        static readonly Color ControlCombat = new(.89f, .38f, .20f, .95f);

        void BuildControls()
        {
            var background = MenuStyle.Image("Black background", controls.transform, 0, 0, 0, 0,
                new Color(.014f, .012f, .011f, 1f));
            MenuStyle.Stretch(background.rectTransform);

            var title = MenuStyle.Text("Controls title", controls.transform, "CONTROLES",
                190, 91, 1080, 112, 78, MenuStyle.Paper);
            title.fontStyle = FontStyles.Bold;
            var brand = MenuStyle.Text("UniDoom mark", controls.transform, "UNIDOOM",
                1380, 126, 350, 50, 34, MenuStyle.Rust);
            brand.alignment = TextAlignmentOptions.MidlineRight;
            brand.fontStyle = FontStyles.Bold;
            MenuStyle.Text("Introduction", controls.transform,
                "Conheça os comandos antes de entrar.",
                194, 207, 1450, 47, 27, MenuStyle.Muted);

            MenuStyle.Image("Header line", controls.transform, 190, 278, 1540, 2,
                new Color(.25f, .14f, .10f));
            MenuStyle.Image("Rust accent", controls.transform, 190, 277, 188, 4, MenuStyle.Rust);

            var art = MenuStyle.Rect("Keyboard and mouse artwork", controls.transform,
                190, 302, 1540, 560).gameObject.AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("Menus/ControlsKeyboardMouse");
            art.uvRect = new Rect(0, .18f, 1, .65f);
            art.raycastTarget = false;
            if (art.texture == null) Debug.LogError("Missing controls artwork: Menus/ControlsKeyboardMouse");

            var highlights = MenuStyle.Rect("Highlighted controls", controls.transform, 0, 0, 0, 0);
            var lines = MenuStyle.Rect("Control arrows", controls.transform, 0, 0, 0, 0);
            var labels = MenuStyle.Rect("Control labels", controls.transform, 0, 0, 0, 0);
            MenuStyle.Stretch(highlights);
            MenuStyle.Stretch(lines);
            MenuStyle.Stretch(labels);

            HighlightKey(highlights, "ESC", 282, 413, 50, 43, ControlLine);
            HighlightKey(highlights, "1", 283, 487, 42, 43, ControlCombat);
            HighlightKey(highlights, "2", 347, 487, 42, 43, ControlCombat);
            HighlightKey(highlights, "W", 375, 554, 42, 43, ControlLine);
            HighlightKey(highlights, "A", 397, 619, 42, 43, ControlLine);
            HighlightKey(highlights, "S", 459, 619, 42, 43, ControlLine);
            HighlightKey(highlights, "D", 521, 619, 42, 43, ControlLine);
            HighlightKey(highlights, "E", 500, 554, 42, 43, ControlLine);
            HighlightKey(highlights, "R", 562, 554, 42, 43, ControlCombat);
            HighlightKey(highlights, "F", 584, 619, 42, 43, ControlLine);

            ControlCallout(lines, labels, "ESC", "Pausar / menu", 194, 317, 215, 65,
                new Vector2(286, 382), new Vector2(282, 413), ControlLine);
            ControlCallout(lines, labels, "1", "Equipar machado", 421, 317, 210, 65,
                new Vector2(435, 382), new Vector2(283, 487), ControlCombat);
            ControlCallout(lines, labels, "2", "Equipar arma", 643, 317, 205, 65,
                new Vector2(657, 382), new Vector2(347, 487), ControlCombat);
            ControlCallout(lines, labels, "E", "Interagir", 783, 471, 215, 65,
                new Vector2(783, 510), new Vector2(500, 554), ControlLine);
            ControlCallout(lines, labels, "R", "Recarregar", 805, 570, 215, 65,
                new Vector2(805, 600), new Vector2(562, 554), ControlCombat);
            ControlCallout(lines, labels, "F", "Lanterna", 821, 678, 215, 65,
                new Vector2(821, 710), new Vector2(584, 619), ControlLine);
            ControlCallout(lines, labels, "W A S D", "Mover-se", 195, 757, 240, 65,
                new Vector2(255, 757), new Vector2(375, 554), ControlLine);
            DrawArrow(lines, "A", new Vector2(310, 757), new Vector2(397, 619), ControlLine);
            DrawArrow(lines, "S", new Vector2(370, 757), new Vector2(459, 619), ControlLine);
            DrawArrow(lines, "D", new Vector2(425, 757), new Vector2(521, 619), ControlLine);
            ControlCallout(lines, labels, "CLIQUE ESQUERDO", "Atacar / disparar", 1315, 317, 247, 65,
                new Vector2(1450, 382), new Vector2(1520, 449), ControlCombat);
            ControlCallout(lines, labels, "RODA DO MOUSE", "Alternar armas", 1307, 536, 247, 65,
                new Vector2(1554, 566), new Vector2(1575, 450), ControlCombat);
            ControlCallout(lines, labels, "MOUSE", "Olhar ao redor", 1307, 754, 247, 65,
                new Vector2(1554, 775), new Vector2(1571, 663), ControlLine);

            MenuStyle.Text("Door hint", controls.transform,
                "As setas também movem. Portas comuns se abrem ao avançar contra elas.",
                190, 884, 1450, 42, 22, MenuStyle.Muted);
            MenuStyle.Image("Footer line", controls.transform, 190, 937, 1540, 2,
                new Color(.25f, .14f, .10f));

            var back = MenuStyle.Button(controls.transform, "Back", "VOLTAR  [ESC]",
                190, 964, 320, Back, 27, 68);
            controlsStart = MenuStyle.Button(controls.transform, "Start game", "INICIAR JOGO",
                1000, 953, 730, StartFromControls, 38, 84);
            var colors = controlsStart.colors;
            colors.normalColor = new Color(.31f, .11f, .07f, 1f);
            colors.highlightedColor = colors.selectedColor = new Color(.48f, .18f, .10f, 1f);
            colors.pressedColor = new Color(.60f, .23f, .12f, 1f);
            controlsStart.colors = colors;
            var startLabel = controlsStart.transform.Find("Label").GetComponent<TMP_Text>();
            startLabel.alignment = TextAlignmentOptions.Center;
            startLabel.fontStyle = FontStyles.Bold;
            startLabel.rectTransform.offsetMin = Vector2.zero;
            startLabel.rectTransform.offsetMax = Vector2.zero;

            MenuStyle.Link(controlsStart, back);
            var startNavigation = controlsStart.navigation;
            startNavigation.selectOnLeft = back;
            controlsStart.navigation = startNavigation;
            var backNavigation = back.navigation;
            backNavigation.selectOnRight = controlsStart;
            back.navigation = backNavigation;
        }

        static void HighlightKey(Transform parent, string key, float x, float y,
            float width, float height, Color color)
        {
            MenuStyle.Image(key + " highlight", parent, x - width / 2, y - height / 2,
                width, height, new Color(color.r, color.g, color.b, .46f));
            var label = MenuStyle.Text(key + " on keyboard", parent, key,
                x - width / 2, y - height / 2, width, height, key.Length > 1 ? 16 : 25, Color.white);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
        }

        static void ControlCallout(Transform arrows, Transform labels, string key, string action,
            float x, float y, float width, float height, Vector2 from, Vector2 target, Color color)
        {
            DrawArrow(arrows, key, from, target, color);
            MenuStyle.Image(key + " callout", labels, x, y, width, height,
                new Color(.025f, .020f, .017f, .94f));
            MenuStyle.Image(key + " callout accent", labels, x, y, 3, height, color);
            var keyLabel = MenuStyle.Text(key + " name", labels, key, x + 14, y + 5,
                width - 23, 26, key.Length > 13 ? 19 : 22, color);
            keyLabel.fontStyle = FontStyles.Bold;
            MenuStyle.Text(key + " action", labels, action, x + 14, y + 31,
                width - 23, 29, 20, MenuStyle.Paper);
        }

        static void DrawArrow(Transform parent, string name, Vector2 from, Vector2 to, Color color)
        {
            DrawLine(parent, name + " line", from, to, color, 2);
            var direction = (to - from).normalized;
            var normal = new Vector2(-direction.y, direction.x);
            DrawLine(parent, name + " arrow left", to - direction * 14 + normal * 6, to, color, 2);
            DrawLine(parent, name + " arrow right", to - direction * 14 - normal * 6, to, color, 2);
        }

        static void DrawLine(Transform parent, string name, Vector2 from, Vector2 to, Color color, float thickness)
        {
            var delta = to - from;
            var line = MenuStyle.Image(name, parent, from.x, from.y, delta.magnitude, thickness, color);
            line.rectTransform.pivot = new Vector2(0, .5f);
            line.rectTransform.anchoredPosition = new Vector2(from.x, -from.y);
            line.rectTransform.localRotation = Quaternion.Euler(0, 0,
                -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        void StartFromControls()
        {
            if (CurrentScreen != Screen.Controls || busy) return;
            StartNewGameAtScene(SceneUtility.GetScenePathByBuildIndex(0));
        }
    }
}
