using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>
    /// Test-only map teleport menu. Delete this file and its .meta before a player build;
    /// GameMenus keeps compiling because its integration point is an optional partial method.
    /// </summary>
    public sealed partial class GameMenus
    {
        GameObject testTeleportMenu;
        Button testTeleportEntry;
        Button testTeleportFirst;

        partial void BuildTestTeleportMenu(ref Button entryButton)
        {
            testTeleportMenu = Page("Test-only teleport menu");
            testTeleportMenu.SetActive(false);

            testTeleportEntry = ColumnButton(main, "TestTeleport", "TELEPORTES (TESTE)", OpenTestTeleportMenu);
            entryButton = testTeleportEntry;
            BuildTestTeleportPage();

            var backHandler = testTeleportMenu.AddComponent<TestTeleportBackHandler>();
            backHandler.Configure(CloseTestTeleportMenu);
        }

        void BuildTestTeleportPage()
        {
            var title = MenuStyle.Text("Title", testTeleportMenu.transform, "TELEPORTES DE TESTE",
                165, 76, 1590, 72, 48, MenuStyle.Paper);
            title.fontStyle = FontStyles.Bold;
            MenuStyle.Text("Warning", testTeleportMenu.transform,
                "FERRAMENTA DE DESENVOLVIMENTO — cada destino inicia uma sessão nova.",
                165, 154, 1590, 42, 23, MenuStyle.Amber);

            var destinations = new List<Selectable>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrWhiteSpace(scenePath)) continue;
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                AddTestDestination(destinations, LabelForTestScene(sceneName),
                    () => StartTestTeleport(scenePath));

                if (!string.Equals(sceneName, "Basement", StringComparison.Ordinal)) continue;
                AddTestDestination(destinations, "PORÃO — ARENA DO BOSS",
                    () => StartTestTeleport(scenePath, startAtBoss: true));
                AddTestDestination(destinations, "PRISÃO — APÓS O BOSS",
                    () => StartTestTeleport(scenePath, startAtPrison: true));
                AddTestDestination(destinations, "TESTAR FINAL",
                    () => StartTestTeleport(scenePath, startAtPrison: true, startAtFinale: true));
            }

            var back = MenuStyle.Button(testTeleportMenu.transform, "Back", "VOLTAR  [ESC]",
                165, 848, 720, CloseTestTeleportMenu, 28, 62);
            ConfigureTestGridNavigation(destinations, back);
            if (testTeleportFirst == null) testTeleportFirst = back;

            var footer = MenuStyle.Text("Navigation", testTeleportMenu.transform,
                "↑ ↓ ← →  NAVEGAR     ENTER  TELEPORTAR     ESC  VOLTAR",
                165, 940, 1590, 38, 19, MenuStyle.Muted);
            footer.alignment = TextAlignmentOptions.Center;
        }

        void AddTestDestination(List<Selectable> destinations, string label, UnityEngine.Events.UnityAction action)
        {
            int index = destinations.Count;
            float x = 165 + index % 2 * 795;
            float y = 220 + index / 2 * 82;
            var button = MenuStyle.Button(testTeleportMenu.transform, "Teleport_" + index, label,
                x, y, 720, action, 27, 62);
            if (testTeleportFirst == null) testTeleportFirst = button;
            destinations.Add(button);
        }

        static void ConfigureTestGridNavigation(List<Selectable> destinations, Selectable back)
        {
            for (int i = 0; i < destinations.Count; i++)
            {
                bool hasLeft = i % 2 == 1;
                bool hasRight = i % 2 == 0 && i + 1 < destinations.Count;
                destinations[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i >= 2 ? destinations[i - 2] : back,
                    selectOnDown = i + 2 < destinations.Count ? destinations[i + 2] : back,
                    selectOnLeft = hasLeft ? destinations[i - 1] : hasRight ? destinations[i + 1] : null,
                    selectOnRight = hasRight ? destinations[i + 1] : hasLeft ? destinations[i - 1] : null
                };
            }

            back.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = destinations.Count > 0 ? destinations[^1] : back,
                selectOnDown = destinations.Count > 0 ? destinations[0] : back
            };
        }

        void OpenTestTeleportMenu()
        {
            if (busy || CurrentScreen != Screen.Main || testTeleportMenu == null) return;
            main.SetActive(false);
            testTeleportMenu.SetActive(true);
            SetSignLightsVisible(false);
            Select(testTeleportFirst);
        }

        void CloseTestTeleportMenu()
        {
            if (testTeleportMenu == null || !testTeleportMenu.activeSelf) return;
            testTeleportMenu.SetActive(false);
            main.SetActive(true);
            SetSignLightsVisible(true);
            Select(testTeleportEntry);
        }

        void StartTestTeleport(string scenePath, bool startAtBoss = false, bool startAtPrison = false,
            bool startAtFinale = false)
        {
            if (testTeleportMenu != null) testTeleportMenu.SetActive(false);
            StartNewGameAtScene(scenePath, startAtBoss, startAtPrison, startAtFinale);
        }

        static string LabelForTestScene(string sceneName) => sceneName switch
        {
            "EntranceGarden" => "JARDIM DE ENTRADA",
            "SampleScene" => "CAMPUS COMPLETO",
            "Cylinder" => "CILINDRO",
            "Library" => "BIBLIOTECA",
            "ClassRoom" => "SALA DE AULA",
            "DirectorRoom" => "SALA DO DIRETOR",
            "Basement" => "PORÃO — PRÉ-ARENA",
            "CampusExterior" => "CAMPUS EXTERIOR — UNIMAR",
            "LawSchool" => "BLOCO DE DIREITO",
            "CampusLibrary" => "BIBLIOTECA DO CAMPUS",
            "CampusBlock4" => "BLOCO 4",
            _ => sceneName
        };
    }

    sealed class TestTeleportBackHandler : MonoBehaviour
    {
        Action close;

        internal void Configure(Action closeAction) => close = closeAction;

        void Update()
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame ||
                    Gamepad.current.buttonEast.wasPressedThisFrame));
            if (pressed) close?.Invoke();
        }
    }
}
