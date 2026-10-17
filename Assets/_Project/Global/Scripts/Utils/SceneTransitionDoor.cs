using UnityEngine;

namespace UniDoom.Utils
{
    /// <summary>Touch volume at a doorway. Arrivals must leave the volume before using it again.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class SceneTransitionDoor : MonoBehaviour
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationSpawnId;
        [SerializeField] private string destinationLabel;
        [SerializeField] private bool requiresKey01, requiresKey02, requiresKey03;

        public string DestinationScene => destinationScene;
        public string DestinationSpawnId => destinationSpawnId;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || UI.DoorReloadScreen.IsReloading) return;
            var life = player.GetComponent<PlayerLife>();
            if (life != null && life.IsDead) return;
            var keys = player.GetComponent<PlayerPickupKey>();
            if ((requiresKey01 && (keys == null || !keys.key01)) ||
                (requiresKey02 && (keys == null || !keys.key02)) ||
                (requiresKey03 && (keys == null || !keys.key03))) return;

            UI.DoorReloadScreen.TryLoadScene(destinationScene, destinationSpawnId, destinationLabel);
        }

        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.75f, 0.15f, 0.08f, 0.35f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.55f, 0.15f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
