using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniDoom.UI
{
    internal static class MenuStyle
    {
        internal static readonly Color Paper = new(.88f, .85f, .76f);
        internal static readonly Color Muted = new(.60f, .58f, .52f);
        internal static readonly Color Rust = new(.59f, .16f, .075f);
        internal static readonly Color Amber = new(.69f, .46f, .20f);

        // All positions use the approved 1920 x 1080 design, measured from the top left.
        internal static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static Image Image(string name, Transform parent, float x, float y, float w, float h, Color tint)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        internal static TMP_Text Text(string name, Transform parent, string value, float x, float y,
            float w, float h, float size, Color color)
        {
            var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        internal static Button Button(Transform parent, string name, string label, float x, float y,
            float w, UnityAction action, float size = 46)
        {
            var bg = Image(name, parent, x, y, w, 76, Color.white);
            bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = colors.selectedColor = new Color(.21f, .105f, .065f, .92f);
            colors.pressedColor = new Color(.37f, .16f, .08f, 1);
            colors.disabledColor = new Color(.07f, .06f, .05f, .25f);
            colors.fadeDuration = .1f;
            button.colors = colors;
            var arrow = Text("Selection", bg.transform, "›", 10, 0, 30, 76, 58, Rust);
            var text = Text("Label", bg.transform, label, 50, 0, (w - 60) / .65f, 76, size, Paper);
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.localScale = new Vector3(.65f, 1, 1);
            var feedback = bg.gameObject.AddComponent<MenuButtonFeedback>();
            feedback.Marker = arrow.gameObject;
            feedback.Marker.SetActive(false);
            button.onClick.AddListener(action);
            return button;
        }

        internal static void Link(params Selectable[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = controls[(i + controls.Length - 1) % controls.Length],
                    selectOnDown = controls[(i + 1) % controls.Length] };
                controls[i].navigation = nav;
            }
        }

        internal static Sprite Artwork(string resource, Rect topRect)
        {
            var texture = Resources.Load<Texture2D>("Menus/" + resource);
            if (texture == null) { Debug.LogError("Missing menu artwork: " + resource); return null; }
            var rect = new Rect(topRect.x * texture.width, (1 - topRect.y - topRect.height) * texture.height,
                topRect.width * texture.width, topRect.height * texture.height);
            return Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }
    }

}
