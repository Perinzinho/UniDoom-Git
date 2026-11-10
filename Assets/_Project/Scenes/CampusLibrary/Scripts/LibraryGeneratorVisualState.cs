using UnityEngine;

namespace UniDoom.CampusLibrary
{
    [DisallowMultipleComponent]
    public sealed class LibraryGeneratorVisualState : MonoBehaviour
    {
        [SerializeField] GameObject[] installedFuses = new GameObject[3];
        [SerializeField] Renderer[] indicatorRenderers = new Renderer[3];
        [SerializeField] Light[] indicatorLights = new Light[3];

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        void OnEnable()
        {
            LibraryMissionProgress.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => LibraryMissionProgress.Changed -= Refresh;

        void Refresh()
        {
            bool connected = LibraryMissionProgress.Stage >= LibraryMissionStage.FusesInstalled;
            Color visibleGreen = new Color(.08f, 1f, .24f, 1f);
            Color darkGreen = new Color(.018f, .055f, .025f, 1f);

            for (int i = 0; i < 3; i++)
            {
                if (i < installedFuses.Length && installedFuses[i])
                    installedFuses[i].SetActive(connected);

                if (i < indicatorRenderers.Length && indicatorRenderers[i])
                {
                    properties.Clear();
                    properties.SetColor(BaseColor, connected ? visibleGreen : darkGreen);
                    properties.SetColor(EmissionColor, connected ? visibleGreen * 4f : Color.black);
                    indicatorRenderers[i].SetPropertyBlock(properties);
                }

                if (i < indicatorLights.Length && indicatorLights[i])
                {
                    indicatorLights[i].color = visibleGreen;
                    indicatorLights[i].enabled = connected;
                }
            }
        }
    }
}
