using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace UniDoom.UI
{
    public sealed partial class GameMenus
    {
        GameObject document;
        RawImage documentImage, documentBackdrop;
        Button documentClose;
        Object documentOwner;
        RenderTexture documentSnapshot;
        Vector2 documentCanvasSize;
        Rect documentSafeArea;

        public bool IsReadingDocument => CurrentScreen == Screen.Document;

        // Documents share pause ownership with the menus, including camera, HUD, audio and cursor.
        public bool TryOpenDocument(Texture2D texture, Object owner)
        {
            if (texture == null || owner == null || CurrentScreen != Screen.Gameplay || busy ||
                ownsPause || Time.timeScale <= 0 || DeathScreen.IsVisible || DoorReloadScreen.IsReloading)
                return false;
            var player = GameManager.SessionPlayer;
            if (player == null || (player.TryGetComponent<PlayerLife>(out var life) && life.IsDead)) return false;

            documentOwner = owner;
            documentImage.texture = texture;
            Canvas.ForceUpdateCanvases();
            FitDocument(true);
            documentClose.interactable = true;
            AcquirePause();
            CaptureDocumentBackdrop();
            SetScreen(Screen.Document);
            documentBackdrop.gameObject.SetActive(documentSnapshot != null);
            ApplyCursorState();
            return true;
        }

        public void CloseDocument()
        {
            if (CurrentScreen != Screen.Document) return;
            documentOwner = null;
            documentImage.texture = null;
            documentBackdrop.gameObject.SetActive(false);
            ReleaseDocumentBackdrop();
            SetScreen(Screen.Gameplay);
            ReleasePause();
        }

        public void CloseDocumentOwnedBy(Object owner)
        {
            if (documentOwner == owner) CloseDocument();
        }

        IEnumerator CloseDocumentAfterClick()
        {
            // Consume the close click while gameplay is still blocked, so it cannot fire a gun.
            documentClose.interactable = false;
            yield return null;
            CloseDocument();
        }

        void BuildDocument()
        {
            documentBackdrop = MenuStyle.Rect("Frozen document backdrop", transform, 0, 0, 0, 0)
                .gameObject.AddComponent<RawImage>();
            MenuStyle.Stretch(documentBackdrop.rectTransform);
            documentBackdrop.raycastTarget = false;
            documentBackdrop.transform.SetSiblingIndex(shade.transform.GetSiblingIndex());
            documentBackdrop.gameObject.SetActive(false);

            // Reading uses the whole viewport instead of the menus' fixed 16:9 composition.
            document = MenuStyle.Rect("Document viewer", transform, 0, 0, 0, 0).gameObject;
            MenuStyle.Stretch((RectTransform)document.transform);
            documentImage = MenuStyle.Rect("Original document", document.transform, 0, 0, 1141, 840)
                .gameObject.AddComponent<RawImage>();
            var imageRect = documentImage.rectTransform;
            imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(.5f, .5f);
            imageRect.anchoredPosition = new Vector2(0, 18);
            documentImage.raycastTarget = false;
            var shadow = documentImage.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .7f);
            shadow.effectDistance = new Vector2(4, -6);

            var border = MenuStyle.Image("Close border", document.transform, 0, 0, 282, 58,
                new Color(.53f, .38f, .21f));
            var buttonRect = border.rectTransform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(.5f, .5f);
            buttonRect.anchoredPosition = new Vector2(0, -459);
            var face = MenuStyle.Image("Worn charcoal metal", border.transform, 2, 2, 278, 54, Color.white);
            face.raycastTarget = true;
            face.gameObject.AddComponent<RectMask2D>();
            documentClose = border.gameObject.AddComponent<Button>();
            documentClose.targetGraphic = face;
            var colors = documentClose.colors;
            colors.normalColor = new Color(.075f, .073f, .066f);
            colors.highlightedColor = colors.selectedColor = new Color(.17f, .14f, .10f);
            colors.pressedColor = new Color(.26f, .20f, .12f);
            colors.fadeDuration = .1f;
            documentClose.colors = colors;
            var wearRect = MenuStyle.Rect("Metal wear", face.transform, 0, 0, 278, 54);
            wearRect.gameObject.AddComponent<CanvasRenderer>();
            var wear = wearRect.gameObject.AddComponent<HorrorLoadingBackdrop>();
            wear.raycastTarget = false;
            var label = MenuStyle.Text("Label", border.transform, "FECHAR", 2, 0, 278, 58, 26,
                new Color(.88f, .83f, .71f));
            label.alignment = TextAlignmentOptions.Center;
            documentClose.onClick.AddListener(() => StartCoroutine(CloseDocumentAfterClick()));
        }

        void FitDocument(bool force = false)
        {
            if (documentImage == null || documentImage.texture == null || documentClose == null) return;
            var canvasSize = ((RectTransform)transform).rect.size;
            var safe = UnityEngine.Screen.safeArea;
            int screenWidth = UnityEngine.Screen.width, screenHeight = UnityEngine.Screen.height;
            if (canvasSize.x <= 0 || canvasSize.y <= 0 || screenWidth <= 0 || screenHeight <= 0) return;
            if (!force && canvasSize == documentCanvasSize && safe == documentSafeArea) return;
            documentCanvasSize = canvasSize;
            documentSafeArea = safe;

            var viewport = (RectTransform)document.transform;
            viewport.anchorMin = new Vector2(safe.xMin / screenWidth, safe.yMin / screenHeight);
            viewport.anchorMax = new Vector2(safe.xMax / screenWidth, safe.yMax / screenHeight);
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            var available = Vector2.Scale(canvasSize, viewport.anchorMax - viewport.anchorMin);
            float margin = Mathf.Clamp(Mathf.Min(available.x, available.y) * .025f, 12, 32);
            float controlsScale = Mathf.Min(1, Mathf.Min(available.x / 360, available.y / 180));
            float buttonHeight = 58 * controlsScale, gap = 12 * controlsScale;
            float width = Mathf.Max(1, available.x - margin * 2);
            float height = Mathf.Max(1, available.y - margin * 2 - buttonHeight - gap);
            var texture = documentImage.texture;
            float scale = Mathf.Min(width / texture.width, height / texture.height);
            var imageSize = new Vector2(texture.width, texture.height) * scale;
            documentImage.rectTransform.sizeDelta = imageSize;
            documentImage.rectTransform.anchoredPosition = new Vector2(0, (buttonHeight + gap) * .5f);
            var closeRect = (RectTransform)documentClose.transform;
            closeRect.localScale = Vector3.one * controlsScale;
            closeRect.anchoredPosition = new Vector2(0, -(imageSize.y + gap) * .5f);
        }

        void CaptureDocumentBackdrop()
        {
            ReleaseDocumentBackdrop();
            var camera = Camera.main;
            if (camera == null) return;
            // Render the current room once at reduced resolution. Bilinear enlargement softens it
            // without copying the reference artwork or adding a full-screen postprocessing pass.
            documentSnapshot = new RenderTexture(Mathf.Max(1, UnityEngine.Screen.width / 6),
                Mathf.Max(1, UnityEngine.Screen.height / 6), 24, RenderTextureFormat.ARGB32)
                { name = "Paused document background", filterMode = FilterMode.Bilinear };
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = documentSnapshot };
                if (RenderPipeline.SupportsRenderRequest(camera, request))
                    RenderPipeline.SubmitRenderRequest(camera, request);
                else
                {
                    camera.targetTexture = documentSnapshot;
                    camera.Render();
                }
                documentBackdrop.texture = documentSnapshot;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("Could not capture the document backdrop: " + error.Message, this);
                ReleaseDocumentBackdrop();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }
        }

        void ReleaseDocumentBackdrop()
        {
            if (documentBackdrop != null) documentBackdrop.texture = null;
            if (documentSnapshot == null) return;
            documentSnapshot.Release();
            Destroy(documentSnapshot);
            documentSnapshot = null;
        }
    }
}
