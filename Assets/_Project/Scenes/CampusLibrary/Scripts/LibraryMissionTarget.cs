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
        [SerializeField, Min(.1f)] float touchRadius = .75f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        public LibraryMissionController Mission => mission;
        public LibraryTargetKind Kind => kind;
        public int Id => id;
        public Texture2D Document => document;
        public bool TryInteract() => mission != null && mission.TryInteract(this);

        void Awake()
        {
            // Fuses use the same touch collection as keys and supplies instead of a key press.
            if (kind != LibraryTargetKind.Fuse) return;
            var trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = touchRadius;
            trigger.center = Vector3.zero;
        }

        void OnTriggerEnter(Collider other) => TryTouchCollect(other);
        void OnTriggerStay(Collider other) => TryTouchCollect(other);
        void TryTouchCollect(Collider other)
        {
            if (kind != LibraryTargetKind.Fuse || !mission) return;
            var player = other.GetComponentInParent<PlayerMovement>();
            if (!player || player != GameManager.SessionPlayer) return;
            mission.TryCollectOnTouch(this);
        }
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
