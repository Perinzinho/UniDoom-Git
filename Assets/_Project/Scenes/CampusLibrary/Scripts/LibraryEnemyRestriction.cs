using UnityEngine;
using UnityEngine.AI;

namespace UniDoom.CampusLibrary
{
    // Added only to this mission's enemies. Enemy's existing attacks check this optional guard.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class LibraryEnemyRestriction : MonoBehaviour
    {
        [SerializeField] LibraryMaintenanceSafety safeArea;
        NavMeshAgent agent;
        Rigidbody body;
        Enemy enemy;
        public void Configure(LibraryMaintenanceSafety area) { safeArea = area; KeepOutside(); }
        public bool CanTarget(Vector3 position) => !safeArea || (!safeArea.Contains(position) && !safeArea.Contains(transform.position));
        void Awake() { agent = GetComponent<NavMeshAgent>(); body = GetComponent<Rigidbody>(); enemy = GetComponent<Enemy>(); }
        void FixedUpdate() => KeepOutside();
        void LateUpdate() => KeepOutside();
        void KeepOutside()
        {
            if (!safeArea || !enemy || enemy.IsDead || !safeArea.Contains(transform.position)) return;
            // Defence in depth for externally teleported actors; authored NavMesh and the physical barrier exclude the room.
            Vector3 recovery = safeArea.RecoveryPosition;
            if (agent && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.Warp(recovery); }
            if (body) body.position = recovery;
            transform.position = recovery;
        }
    }
}
