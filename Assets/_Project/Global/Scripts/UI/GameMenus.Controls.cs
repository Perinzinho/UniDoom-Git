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
            MenuStyle.Text("Door hint", controls.transform,
                "As setas também movem. Portas comuns se abrem ao avançar contra elas.",
                194, 241, 1450, 30, 19, MenuStyle.Muted);

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
            MenuStyle.Stretch(highlights);

            HighlightKey(highlights, "ESC", 282, 413, 50, 43, ControlLine);
            HighlightKey(highlights, "1", 283, 487, 42, 43, ControlCombat);
            HighlightKey(highlights, "2", 347, 487, 42, 43, ControlCombat);
            HighlightKey(highlights, "W", 443, 554, 42, 43, ControlLine);
            HighlightKey(highlights, "A", 397, 619, 42, 43, ControlLine);
            HighlightKey(highlights, "S", 459, 619, 42, 43, ControlLine);
            HighlightKey(highlights, "D", 521, 619, 42, 43, ControlLine);
            HighlightKey(highlights, "E", 500, 554, 42, 43, ControlLine);
            HighlightKey(highlights, "R", 562, 554, 42, 43, ControlCombat);
            HighlightKey(highlights, "F", 584, 619, 42, 43, ControlLine);

            // Keep the legend below the photographed keys, so descriptions never cover them.
            MenuStyle.Image("Control legend background", controls.transform, 190, 838, 1540, 102,
                new Color(.025f, .020f, .017f, .96f));
            MenuStyle.Image("Control legend rule", controls.transform, 190, 838, 1540, 2,
                new Color(.25f, .14f, .10f));
            ControlLegend("ESC", "Pausar / menu", 202, 842, 280, ControlLine);
            ControlLegend("W A S D", "Mover-se", 510, 842, 280, ControlLine);
            ControlLegend("E", "Interagir", 818, 842, 280, ControlLine);
            ControlLegend("F", "Lanterna", 1126, 842, 280, ControlLine);
            ControlLegend("MOUSE", "Olhar ao redor", 1434, 842, 280, ControlLine);
            ControlLegend("1", "Equipar machado", 202, 889, 280, ControlCombat);
            ControlLegend("2", "Equipar arma", 510, 889, 280, ControlCombat);
            ControlLegend("R", "Recarregar", 818, 889, 280, ControlCombat);
            ControlLegend("CLIQUE ESQUERDO", "Atacar / disparar", 1126, 889, 280, ControlCombat);
            ControlLegend("RODA DO MOUSE", "Alternar armas", 1434, 889, 280, ControlCombat);
            MenuStyle.Image("Footer line", controls.transform, 190, 942, 1540, 2,
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

        void ControlLegend(string key, string action, float x, float y, float width, Color color)
        {
            var name = MenuStyle.Text(key + " legend", controls.transform, key,
                x, y, width, 22, key.Length > 13 ? 17 : 19, color);
            name.fontStyle = FontStyles.Bold;
            MenuStyle.Text(key + " action", controls.transform, action,
                x, y + 22, width, 22, 18, MenuStyle.Paper);
        }

        void StartFromControls()
        {
            if (CurrentScreen != Screen.Controls || busy) return;
            StartNewGameAtScene(SceneUtility.GetScenePathByBuildIndex(0));
        }
    }
}
