using UnityEngine;

namespace UniDoom.CampusLibrary
{
    [DisallowMultipleComponent]
    public sealed class LibraryGeneratorModelVariants : MonoBehaviour
    {
        static readonly string[] ResourcePaths =
        {
            "Generator/Gerador_UniDoom_0_fusiveis",
            "Generator/Gerador_UniDoom_1_fusiveis",
            "Generator/Gerador_UniDoom_2_fusiveis",
            "Generator/Gerador_UniDoom_3_fusiveis"
        };

        [SerializeField] Transform variantsRoot;
        [SerializeField] GameObject[] variants = new GameObject[4];
        bool configured;

        internal void Configure()
        {
            if (configured) return;
            configured = true;

            if (!HasPersistentVariants())
            {
                variantsRoot = new GameObject("Generator GLB model variants").transform;
                variantsRoot.SetParent(transform, false);
                variantsRoot.position = new Vector3(-24.7f, 0f, 42.3f);

                for (int i = 0; i < variants.Length; i++)
                {
                    GameObject source = Resources.Load<GameObject>(ResourcePaths[i]);
                    if (!source)
                    {
                        Debug.LogError($"CampusLibrary: modelo do gerador não importado: {ResourcePaths[i]}", this);
                        Destroy(variantsRoot.gameObject);
                        variantsRoot = null;
                        configured = false;
                        return;
                    }

                    variants[i] = Instantiate(source, variantsRoot, false);
                    variants[i].name = $"Generator model - {i} fuses";
                    variants[i].SetActive(false);
                }
            }

            HideLegacyVisuals();
            LibraryMissionProgress.Changed += Refresh;
            Refresh();
        }

        bool HasPersistentVariants()
        {
            if (!variantsRoot || variants == null || variants.Length != 4) return false;
            foreach (GameObject variant in variants) if (!variant) return false;
            return true;
        }

        void HideLegacyVisuals()
        {
            foreach (Renderer renderer in transform.root.GetComponentsInChildren<Renderer>(true))
            {
                string objectName = renderer.gameObject.name;
                if (objectName == "Library backup generator" || objectName == "Generator worn blue cover" ||
                    objectName == "Generator steel support" || objectName == "Generator ventilation grille" ||
                    objectName == "Generator ignition control pedestal" || objectName == "Generator instructions" ||
                    objectName == "Physical plate - Generator instructions" || objectName.StartsWith("Installed fuse socket "))
                    renderer.enabled = false;
            }
        }

        void Refresh()
        {
            int visible = Mathf.Clamp(LibraryMissionProgress.InstalledFuseCount, 0, 3);
            for (int i = 0; i < variants.Length; i++)
                if (variants[i]) variants[i].SetActive(i == visible);
        }

        void OnDestroy()
        {
            LibraryMissionProgress.Changed -= Refresh;
        }
    }
}
