using System;
using UnityEngine;

namespace TotalChaosMaps
{
    /// <summary>
    /// Porta que sobe para dentro do lintel, como as portas do Doom.
    /// - keyId vazio  => porta comum (abre sozinha quando o jogador chega perto, se autoOpen).
    /// - keyId = "1"  => trancada; só abre depois de Interact(hasKey) com a chave certa.
    /// Chaves são ids como "1".."9" (a mesma numeração das letras a..i do layout).
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

        public void Close() => _open = false;

        void Update()
        {
            var cam = Camera.main;
            if (autoOpen && !IsLocked && cam != null)
            {
                Vector3 p = cam.transform.position;
                var flat = new Vector3(_closedPos.x, p.y, _closedPos.z);
                if (Vector3.Distance(p, flat) < autoOpenRadius) Open();
            }

            if (_open && autoCloseDelay > 0f && Time.time > _closeAt) _open = false;

            transform.position = Vector3.MoveTowards(
                transform.position, _open ? _openPos : _closedPos, speed * Time.deltaTime);
        }
    }
}
