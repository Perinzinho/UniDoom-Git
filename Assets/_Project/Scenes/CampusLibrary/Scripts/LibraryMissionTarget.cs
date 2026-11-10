using UnityEngine;
using UniDoom.UI;

namespace UniDoom.CampusLibrary
{
    public enum LibraryTargetKind { Generator, Fuse, Button, Clue, FuseSocket }

    [DisallowMultipleComponent]
    public sealed class LibraryMissionTarget : MonoBehaviour
    {
        [SerializeField] LibraryMissionController mission;
        [SerializeField] LibraryTargetKind kind;
        [SerializeField] int id;
        [SerializeField] Renderer indicator;
        [SerializeField] Texture2D document;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        public LibraryMissionController Mission => mission;
        public LibraryTargetKind Kind => kind;
        public int Id => id;
        public Texture2D Document => document;
        public bool TryInteract() => mission != null && mission.TryInteract(this);
        void OnDisable() { if (document) GameMenus.Instance?.CloseDocumentOwnedBy(this); }
        internal void SetIndicator(Color color)
        {
            if (!indicator) return;
            block ??= new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block); block.SetColor(BaseColor, color); block.SetColor(Emission, color * .5f);
            indicator.SetPropertyBlock(block);
        }
    }
}
