using UniDoom.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UniDoom.CampusBlock4
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class Block4SecretaryLetter : MonoBehaviour
    {
        [SerializeField] Texture2D document;
        [SerializeField, Min(.5f)] float interactionDistance = 2.8f;

        Camera view;
        bool inFocus;

        void Update()
        {
            inFocus = false;
            if (Time.timeScale <= 0 || GameMenus.BlocksGameplay || DeathScreen.IsVisible ||
                DoorReloadScreen.IsReloading || GameManager.SessionPlayer == null) return;

            if (!view) view = Camera.main;
            if (!view || !document) return;

            Vector3 from = view.transform.position;
            Vector3 to = transform.position;
            Vector3 flat = Vector3.ProjectOnPlane(to - from, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up);
            if (flat.sqrMagnitude < .01f || flat.sqrMagnitude > interactionDistance * interactionDistance ||
                Vector3.Dot(forward.normalized, flat.normalized) < .978f) return;

            if (Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<Block4SecretaryLetter>() != this) return;

            inFocus = true;
            if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))
                GameMenus.Instance?.TryOpenDocument(document, this);
        }

        void OnGUI()
        {
            if (!inFocus || GameMenus.BlocksGameplay || GameplayNotice.IsShowing) return;
            GameplayNotice.Draw(Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame
                ? "[X] Ler carta da secretaria" : "[E] Ler carta da secretaria");
        }

        void OnDisable()
        {
            inFocus = false;
            GameMenus.Instance?.CloseDocumentOwnedBy(this);
        }
    }
}
