using System.Collections.Generic;
using UniDoom.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace UniDoom.CampusLibrary
{
    [DisallowMultipleComponent]
    public sealed class LibraryZombieSpawner : MonoBehaviour
    {
        [SerializeField] Enemy zombiePrefab;
        [SerializeField] Transform entryPoint;
        [SerializeField] Transform[] additionalEntryPoints = System.Array.Empty<Transform>();
        [SerializeField] LibraryMaintenanceSafety safeArea;
        [SerializeField, Range(1, 10)] int aliveLimit = 1;
        [SerializeField, Min(3)] float replacementDelay = 4;
        [SerializeField, Min(6)] float minimumSpawnDistance = 8;
        readonly Dictionary<Enemy, UnityAction<GameObject>> living = new();
        readonly List<Enemy> removed = new();
        float nextSpawn;
        int nextEntry;
        public int LivingCount => living.Count;
        void Start()
        {
            if (!zombiePrefab || !entryPoint || !safeArea) { Debug.LogError("CampusLibrary: configure zumbi, entrada e área segura.", this); enabled = false; return; }
            nextSpawn = Time.time + replacementDelay;
        }
        void Update()
        {
            if (!LibraryMissionController.GameplayAvailable || LibraryMissionProgress.IsLibraryGeneratorPowered) return;
            removed.Clear();
            foreach (var pair in living) if (!pair.Key || pair.Key.IsDead) removed.Add(pair.Key);
            foreach (var dead in removed) Unregister(dead);
            if (Time.time < nextSpawn || living.Count >= Mathf.Clamp(aliveLimit, 1, 10)) return;
            nextSpawn = Time.time + .5f;
            if (!FindEntry(out var hit, out var rotation)) return;
            var enemy = Instantiate(zombiePrefab, hit.position + Vector3.up, rotation);
            var restriction = enemy.GetComponent<LibraryEnemyRestriction>();
            if (!restriction) restriction = enemy.gameObject.AddComponent<LibraryEnemyRestriction>();
            restriction.Configure(safeArea);
            UnityAction<GameObject> callback = _ => Unregister(enemy);
            living.Add(enemy, callback); enemy.OnDeath.AddListener(callback);
            nextSpawn = Time.time + replacementDelay;
        }
        bool FindEntry(out NavMeshHit hit, out Quaternion rotation)
        {
            int count = 1 + additionalEntryPoints.Length;
            for (int offset = 0; offset < count; offset++)
            {
                int index = (nextEntry + offset) % count;
                var entry = index == 0 ? entryPoint : additionalEntryPoints[index - 1];
                if (!entry || !NavMesh.SamplePosition(entry.position, out hit, 1.5f, NavMesh.AllAreas) || safeArea.Contains(hit.position) ||
                    !EnemySpawnSafety.CanSpawn(hit.position, GameManager.SessionPlayer.transform, minimumSpawnDistance)) continue;
                nextEntry = (index + 1) % count; rotation = entry.rotation; return true;
            }
            hit = default; rotation = Quaternion.identity; return false;
        }
        void Unregister(Enemy enemy)
        {
            if (!living.TryGetValue(enemy, out var callback)) return;
            if (enemy) enemy.OnDeath.RemoveListener(callback);
            living.Remove(enemy); nextSpawn = Time.time + replacementDelay;
            // Mission reinforcements are unbounded in time; retire their corpse objects after the death animation.
            if (enemy && enemy.IsDead) Destroy(enemy.gameObject, 18);
        }
        void OnEnable() { foreach (var pair in living) if (pair.Key) pair.Key.OnDeath.AddListener(pair.Value); }
        void OnDisable() { foreach (var pair in living) if (pair.Key) pair.Key.OnDeath.RemoveListener(pair.Value); }
        void OnDestroy()
        {
            foreach (var pair in living) if (pair.Key) pair.Key.OnDeath.RemoveListener(pair.Value);
            living.Clear();
        }
    }
}
