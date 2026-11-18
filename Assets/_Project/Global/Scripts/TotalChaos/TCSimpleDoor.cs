using System;
using UnityEngine;

namespace TotalChaosMaps
{
    /// <summary>
    /// Porta que sobe para dentro do lintel, como as portas do Doom.
    /// - keyId vazio  => porta comum (abre quando o jogador encosta, se autoOpen).
    /// - keyId = "1"  => trancada; só abre depois de Interact(hasKey) com a chave certa.
    /// Chaves são ids como "1".."9" (a mesma numeração das letras a..i do layout).
    /// Inimigos perseguindo também abrem portas destrancadas (autoOpen), como os
    /// monstros do Doom; destrancar é responsabilidade do jogador.
    /// </summary>
    public class TCSimpleDoor : MonoBehaviour
    {
        public string keyId = "";
        public float openHeight = 2.3f;
        public float speed = 2.5f;
        public bool autoOpen = true;
        public float autoOpenRadius = 2.4f;
        [Tooltip("0 = nunca fecha sozinha.")]
        public float autoCloseDelay = 3f;

        [Header("Tela de Reload / Shaders")]
        public bool triggerShaderReload = true;
        public string sectorName = "SETOR";
        public float reloadCooldown = 5f;

        Vector3 _closedPos, _openPos;
        bool _open, _unlocked;
        float _closeAt;
        float _lastReloadTime = -999f;
        float _nextEnemyProbe;
        static readonly Collider[] enemyHits = new Collider[16];

        public bool IsLocked => !string.IsNullOrEmpty(keyId) && !_unlocked;

        void Awake()
        {
            _closedPos = transform.position;
            _openPos = _closedPos + Vector3.up * openHeight;
        }

        /// <summary>Chame quando o jogador apertar "usar" olhando para a porta.</summary>
        public bool Interact(Func<string, bool> hasKey)
        {
            if (IsLocked)
            {
                if (hasKey == null || !hasKey(keyId)) return false;
                _unlocked = true;
            }
            Open();
            return true;
        }

        public void Open()
        {
            if (IsLocked) return;

            if (triggerShaderReload && !_open && Time.time >= _lastReloadTime + reloadCooldown)
            {
                _lastReloadTime = Time.time;
                UniDoom.UI.DoorReloadScreen.TriggerReload(sectorName);
            }

            _open = true;
            _closeAt = Time.time + autoCloseDelay;
        }

        public bool TryOpenByContact()
        {
            if (!autoOpen || !string.IsNullOrEmpty(keyId)) return false;
            Open();
            return true;
        }

        public void Close() => _open = false;

        void Update()
        {
            if (_open && autoCloseDelay > 0f && Time.time > _closeAt) _open = false;

            // Monsters open doors in Doom too. A chaser must not stay sealed behind a
            // leaf that closed right after the player walked through, so pursuing or
            // searching enemies trigger the same auto-open the player's contact does.
            // Locked doors stay shut; player-unlocked ones open for the horde as well.
            if (!_open && autoOpen && !IsLocked && Time.time >= _nextEnemyProbe)
            {
                _nextEnemyProbe = Time.time + .25f;
                if (PursuingEnemyNearby()) Open();
            }

            transform.position = Vector3.MoveTowards(
                transform.position, _open ? _openPos : _closedPos, speed * Time.deltaTime);
        }

        bool PursuingEnemyNearby()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, autoOpenRadius, enemyHits,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var enemy = enemyHits[i].GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead) continue;
                // Wandering zombies keep doors shut; only active pursuit opens them.
                if (enemy.State == Enemy.BehaviourState.Wandering) continue;
                return true;
            }
            return false;
        }
    }
}
