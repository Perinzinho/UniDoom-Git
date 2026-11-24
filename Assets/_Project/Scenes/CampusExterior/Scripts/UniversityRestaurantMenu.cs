using TMPro;
using UnityEngine;

namespace UniDoom.CampusExterior
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UniversityRestaurantMenu : MonoBehaviour
    {
        [Header("Identidade")]
        [SerializeField] string establishmentTitle = "REFEITÓRIO EMERGENCIA";
        [SerializeField] string statusText = "ÚLTIMO TURNO • 16:00";

        [Header("Cardápio")]
        [SerializeField, TextArea(3, 6)]
        string lanches = "X-UNIMAR ........ R$ 8\nSANDUÍCHE ........ R$ 6\nDOG DO CAMPO ..... R$ 7";
        [SerializeField, TextArea(3, 6)]
        string salgados = "Coxinha .......... R$ 4\nPastel ........... R$ 5\nEmpada ........... R$ 5";
        [SerializeField, TextArea(3, 6)]
        string bebidas = "Café ............. R$ 3\nSuco ............. R$ 5\nÁgua .............. R$ 2";

        [Header("Aparência")]
        [SerializeField] Color ink = new(0.78f, 0.82f, 0.72f);
        [SerializeField] Color accent = new(0.78f, 0.34f, 0.16f);
        [SerializeField, Min(0.1f)] float textSize = 0.28f;

        const string BoardName = "Post-apocalyptic menu board";

        Transform menuRoot;
        Material boardMaterial;

        void OnEnable() => Refresh();

        void OnValidate()
        {
            if (isActiveAndEnabled) Refresh();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureCurrentScene()
        {
            foreach (var root in FindObjectsByType<UniversityRestaurantMenu>(FindObjectsInactive.Exclude))
                root.Refresh();

            if (FindObjectsByType<UniversityRestaurantMenu>(FindObjectsInactive.Exclude).Length > 0) return;

            foreach (var root in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (root.name.IndexOf("UniversityRestaurant", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    root.name.IndexOf("University restaurant", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                root.gameObject.AddComponent<UniversityRestaurantMenu>();
                break;
            }
        }

        public void Refresh()
        {
            // menuRoot is not serialized, so after a scene reload it is null while the
            // boards baked by earlier editor runs are still children. Remove every
            // board by name, otherwise each Refresh stacks another copy on top.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != BoardName) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            menuRoot = new GameObject(BoardName).transform;
            menuRoot.SetParent(transform, false);
            CreateHeader();
            CreateCategory("Menu 1", "LANCHES", lanches, 0);
            CreateCategory("Menu 2", "SALGADOS", salgados, 1);
            CreateCategory("Menu 3", "BEBIDAS", bebidas, 2);
        }

        void OnDestroy()
        {
            if (boardMaterial == null) return;
            if (Application.isPlaying) Destroy(boardMaterial);
            else DestroyImmediate(boardMaterial);
        }

        void CreateHeader()
        {
            CreatePlate("Title plate", new Vector3(0f, 3.55f, 5.05f), new Vector3(5.8f, .62f, .09f));
            var header = CreateWorldText("Custom menu header",
                new Vector3(0f, 3.55f, 5.105f), 5.5f, .55f);
            header.text = $"<b>{establishmentTitle}</b>\n<size=60%><color=#{ColorUtility.ToHtmlStringRGB(accent)}>{statusText}</color></size>";
            header.fontSize = textSize * 1.8f;
            header.lineSpacing = -18f;
        }

        void CreateCategory(string panelName, string heading, string items, int index)
        {
            float x = -2f + index * 2f;
            CreatePlate(panelName, new Vector3(x, 2.62f, 5.10f), new Vector3(1.82f, 1.35f, .09f));

            var label = CreateWorldText($"Custom {panelName}",
                new Vector3(x, 2.62f, 5.16f), 1.65f, 1.18f);
            label.text = $"<color=#{ColorUtility.ToHtmlStringRGB(accent)}><b>{heading}</b></color>\n<size=68%>{items}</size>";
            label.fontSize = textSize;
            label.lineSpacing = -8f;
        }

        void CreatePlate(string name, Vector3 localPosition, Vector3 scale)
        {
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = name;
            plate.transform.SetParent(menuRoot, false);
            plate.transform.localPosition = localPosition;
            plate.transform.localScale = scale;
            var collider = plate.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            var renderer = plate.GetComponent<Renderer>();
            if (boardMaterial == null)
            {
                boardMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                boardMaterial.name = BoardName + " material";
                boardMaterial.color = new Color(.055f, .065f, .060f);
                boardMaterial.SetFloat("_Smoothness", .12f);
            }
            renderer.sharedMaterial = boardMaterial;
        }

        TMP_Text CreateWorldText(string name, Vector3 localPosition, float width, float height)
        {
            var text = new GameObject(name).AddComponent<TextMeshPro>();
            text.transform.SetParent(menuRoot, false);
            text.transform.localPosition = localPosition;
            text.transform.localRotation = Quaternion.identity;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.color = ink;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            text.extraPadding = true;
            return text;
        }
    }
}
