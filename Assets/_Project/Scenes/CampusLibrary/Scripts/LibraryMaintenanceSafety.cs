using UnityEngine;
using UnityEngine.AI;

namespace UniDoom.CampusLibrary
{
    [DisallowMultipleComponent]
    public sealed class LibraryMaintenanceSafety : MonoBehaviour
    {
        [SerializeField] BoxCollider safeVolume;
        [SerializeField] Collider enemyBarrier;
        [SerializeField] Transform recoveryPoint;
        CharacterController player;
        public Vector3 RecoveryPosition => recoveryPoint.position;
        public bool Contains(Vector3 position)
        {
            if (!safeVolume) return false;
            Vector3 point = safeVolume.transform.InverseTransformPoint(position) - safeVolume.center;
            Vector3 half = safeVolume.size * .5f;
            return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.z) <= half.z && Mathf.Abs(point.y) <= half.y;
        }
        void Start()
        {
            if (!safeVolume || !enemyBarrier || !recoveryPoint) { Debug.LogError("CampusLibrary: volume seguro, barreira ou recuperação ausentes.", this); enabled = false; }
        }
        void Update()
        {
            var current = GameManager.SessionPlayer ? GameManager.SessionPlayer.GetComponent<CharacterController>() : null;
            if (current == player) return;
            player = current;
            if (player) Physics.IgnoreCollision(player, enemyBarrier, true);
        }
    }
}
