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
        [SerializeField] LibraryMaintenanceSafety safeArea;
        [SerializeField, Range(1, 2)] int aliveLimit = 1;
        [SerializeField, Min(3)] float replacementDelay = 4;
        [SerializeField, Min(6)] float minimumSpawnDistance = 8;
        readonly Dictionary<Enemy, UnityAction<GameObject>> living = new();
        readonly List<Enemy> removed = new();
        float nextSpawn;
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
            if (Time.time < nextSpawn || living.Count >= Mathf.Clamp(aliveLimit, 1, 2)) return;
            nextSpawn = Time.time + .5f;
            if (!EnemySpawnSafety.CanSpawn(entryPoint.position, GameManager.SessionPlayer.transform, minimumSpawnDistance)) return;
            if (!NavMesh.SamplePosition(entryPoint.position, out var hit, 1.5f, NavMesh.AllAreas) || safeArea.Contains(hit.position)) return;
            var enemy = Instantiate(zombiePrefab, hit.position + Vector3.up, entryPoint.rotation);
            var restriction = enemy.GetComponent<LibraryEnemyRestriction>();
            if (!restriction) restriction = enemy.gameObject.AddComponent<LibraryEnemyRestriction>();
            restriction.Configure(safeArea);
            UnityAction<GameObject> callback = _ => Unregister(enemy);
            living.Add(enemy, callback); enemy.OnDeath.AddListener(callback);
            nextSpawn = Time.time + replacementDelay;
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
