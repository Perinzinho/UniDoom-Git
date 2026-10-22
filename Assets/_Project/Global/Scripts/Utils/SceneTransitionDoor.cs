using System.Collections.Generic;
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
        [SerializeField] private OpenDoorWithKey physicalDoor;
        [SerializeField] private bool waitForPhysicalDoorToOpen;
        private readonly HashSet<Collider> waitingPlayers = new HashSet<Collider>();

        public string DestinationScene => destinationScene;
        public string DestinationSpawnId => destinationSpawnId;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            // Arrivals during a scene load are deliberately not queued: they must leave first.
            if (UI.DoorReloadScreen.IsReloading) return;
            if (other.GetComponentInParent<PlayerMovement>() == null) return;
            waitingPlayers.Add(other);
            TryTransition(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // A key can be collected or a physical door can unlock while already inside.
            if (waitingPlayers.Contains(other)) TryTransition(other);
        }

        private void OnTriggerExit(Collider other) => waitingPlayers.Remove(other);
        private void OnDisable() => waitingPlayers.Clear();

        private void TryTransition(Collider other)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || UI.DoorReloadScreen.IsReloading) return;
            var life = player.GetComponent<PlayerLife>();
            if (life != null && life.IsDead) return;
            var keys = player.GetComponent<PlayerPickupKey>();
            if ((requiresKey01 && (keys == null || !keys.key01)) ||
                (requiresKey02 && (keys == null || !keys.key02)) ||
                (requiresKey03 && (keys == null || !keys.key03))) return;
            // The player can cross a rotating door before its animation reaches 80%.
            // Waiting for IsOpen lets them leave this volume and fall outside the room.
            if (physicalDoor != null && !physicalDoor.IsUnlocked) return;
            if (waitForPhysicalDoorToOpen && physicalDoor != null && !physicalDoor.IsFullyOpen) return;

            if (UI.DoorReloadScreen.TryLoadScene(destinationScene, destinationSpawnId, destinationLabel))
                waitingPlayers.Clear();
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
