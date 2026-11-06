using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UniDoom.UI
{
    public sealed partial class GameMenus
    {
        UnityEngine.Object interactionOwner;
        GameObject interactionPanel;
        Action interactionClosed;

        public bool TryOpenInteraction(GameObject panel, UnityEngine.Object owner, Action closed)
        {
            if (panel == null || owner == null || CurrentScreen != Screen.Gameplay || busy || ownsPause ||
                Time.timeScale <= 0 || DeathScreen.IsVisible || DoorReloadScreen.IsReloading) return false;
            var player = GameManager.SessionPlayer;
            if (player == null || (player.TryGetComponent<PlayerLife>(out var life) && life.IsDead)) return false;
            interactionPanel = panel;
            interactionOwner = owner;
            interactionClosed = closed;
            panel.transform.SetParent(transform, false);
            AcquirePause();
            SetScreen(Screen.Interaction);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            // The transition fade must still cover an interaction.
            fade.transform.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(null);
            ApplyCursorState();
            return true;
        }

        public void CloseInteractionOwnedBy(UnityEngine.Object owner)
        {
            if (interactionOwner == owner) CloseInteraction();
        }

        void CloseInteraction()
        {
            if (CurrentScreen != Screen.Interaction) return;
            var callback = interactionClosed;
            interactionOwner = null;
            interactionClosed = null;
            if (interactionPanel != null) interactionPanel.SetActive(false);
            interactionPanel = null;
            EventSystem.current?.SetSelectedGameObject(null);
            SetScreen(Screen.Gameplay);
            ReleasePause();
            callback?.Invoke();
        }
    }
}
