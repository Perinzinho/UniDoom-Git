using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniDoom.UI
{
    public sealed partial class GameMenus
    {
        void BuildControls()
        {
            var background = MenuStyle.Image("Black background", controls.transform, 0, 0, 0, 0,
                new Color(.014f, .012f, .011f, 1f));
            MenuStyle.Stretch(background.rectTransform);

            var title = MenuStyle.Text("Controls title", controls.transform, "CONTROLES",
                190, 118, 1430, 120, 82, MenuStyle.Paper);
            title.fontStyle = FontStyles.Bold;
            var brand = MenuStyle.Text("UniDoom mark", controls.transform, "UNIDOOM",
                1380, 157, 350, 50, 34, MenuStyle.Rust);
            brand.alignment = TextAlignmentOptions.MidlineRight;
            brand.fontStyle = FontStyles.Bold;
            MenuStyle.Text("Introduction", controls.transform,
                "Antes de entrar, conheça os comandos essenciais.",
                194, 254, 1450, 48, 28, MenuStyle.Muted);

            MenuStyle.Image("Header line", controls.transform, 190, 340, 1540, 2,
                new Color(.25f, .14f, .10f));
            MenuStyle.Image("Rust accent", controls.transform, 190, 339, 188, 4, MenuStyle.Rust);

            var exploration = MenuStyle.Text("Exploration", controls.transform, "EXPLORAÇÃO",
                190, 383, 690, 44, 29, MenuStyle.Amber);
            exploration.fontStyle = FontStyles.Bold;
            var combat = MenuStyle.Text("Combat", controls.transform, "COMBATE",
                1000, 383, 690, 44, 29, MenuStyle.Amber);
            combat.fontStyle = FontStyles.Bold;

            ControlRow(controls.transform, "W A S D", "Mover-se (setas também)", 190, 446);
            ControlRow(controls.transform, "MOUSE", "Olhar ao redor", 190, 518);
            ControlRow(controls.transform, "E", "Interagir quando indicado", 190, 590);
            ControlRow(controls.transform, "F", "Acender / apagar lanterna", 190, 662);
            ControlRow(controls.transform, "ESC", "Pausar / abrir menu", 190, 734);

            ControlRow(controls.transform, "1", "Equipar machado", 1000, 446);
            ControlRow(controls.transform, "2", "Equipar arma de fogo", 1000, 518);
            ControlRow(controls.transform, "CLIQUE ESQUERDO", "Atacar / disparar", 1000, 590);
            ControlRow(controls.transform, "R", "Recarregar a arma", 1000, 662);
            ControlRow(controls.transform, "RODA DO MOUSE", "Alternar entre armas", 1000, 734);

            MenuStyle.Text("Door hint", controls.transform,
                "Portas comuns se abrem ao avançar contra elas.",
                190, 819, 1450, 42, 22, MenuStyle.Muted);
            MenuStyle.Image("Footer line", controls.transform, 190, 876, 1540, 2,
                new Color(.25f, .14f, .10f));

            var back = MenuStyle.Button(controls.transform, "Back", "VOLTAR  [ESC]",
                190, 904, 320, Back, 27, 68);
            controlsStart = MenuStyle.Button(controls.transform, "Start game", "INICIAR JOGO",
                1000, 895, 730, StartFromControls, 38, 84);
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

        static void ControlRow(Transform parent, string key, string action, float x, float y)
        {
            MenuStyle.Image(key + " key", parent, x, y, 250, 58,
                new Color(.12f, .095f, .078f, 1f));
            var keyText = MenuStyle.Text(key + " label", parent, key,
                x + 10, y, 230, 58, key.Length > 11 ? 22 : 26, MenuStyle.Paper);
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.fontStyle = FontStyles.Bold;
            MenuStyle.Text(action, parent, action, x + 274, y, 470, 58, 26, MenuStyle.Paper);
        }

        void StartFromControls()
        {
            if (CurrentScreen != Screen.Controls || busy) return;
            StartNewGameAtScene(SceneUtility.GetScenePathByBuildIndex(0));
        }
    }
}
