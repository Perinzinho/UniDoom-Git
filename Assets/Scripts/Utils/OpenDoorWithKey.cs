using System.Collections.Generic;
using UnityEngine;

public class OpenDoorWithKey : MonoBehaviour
{
    [Header("Chaves necessárias para abrir esta porta")]
    [SerializeField] private bool requiresKey01;
    [SerializeField] private bool requiresKey02;
    [SerializeField] private bool requiresKey03;

    [Header("Animação da porta")]
    [SerializeField] private Transform door;
    [Tooltip("Posição da dobradiça como fração do tamanho do collider da folha.")]
    [SerializeField] private Vector3 hingePoint = new Vector3(0f, 0f, -0.5f);
    [Range(0f, 170f)]
    [SerializeField] private float openAngle = 90f;
    [Min(0.01f)]
    [SerializeField] private float animationDuration = 0.6f;
    [Min(0f)]
    [SerializeField] private float closeDelay = 0.3f;

    private readonly HashSet<Collider> nearbyPlayers = new HashSet<Collider>();
    private Vector3 closedCenter;
    private Transform hinge;
    private BoxCollider proximityTrigger;
    private float openProgress;
    private float closeTimer;
    private bool unlocked;
    private float openingDirection = 1f;

    private void Awake()
    {
        if (door == null) door = transform.Find("Door");

        if (door == null || door == transform)
        {
            Debug.LogError("A porta precisa de uma folha filha para animar sem mover o trigger.", this);
            enabled = false;
            return;
        }

        proximityTrigger = GetComponent<BoxCollider>();
        if (proximityTrigger == null || !proximityTrigger.isTrigger)
        {
            Debug.LogError("A porta precisa de um BoxCollider trigger no objeto pai.", this);
            enabled = false;
            return;
        }

        BoxCollider doorCollider = door.GetComponent<BoxCollider>();
        if (doorCollider == null)
        {
            Debug.LogError("A folha da porta precisa de um BoxCollider para posicionar a dobradiça.", this);
            enabled = false;
            return;
        }

        closedCenter = transform.InverseTransformPoint(door.TransformPoint(doorCollider.center));
        Vector3 hingePosition = door.TransformPoint(doorCollider.center +
            Vector3.Scale(doorCollider.size, hingePoint));
        // Compensa a escala antes da rotação para a folha não deformar ao girar.
        Transform motionRoot = new GameObject("DoorMotionRoot").transform;
        motionRoot.SetParent(transform, false);
        Vector3 parentScale = transform.lossyScale;
        motionRoot.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        hinge = new GameObject("DoorHinge").transform;
        hinge.SetParent(motionRoot, false);
        hinge.position = hingePosition;
        door.SetParent(hinge, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Também detecta quem recebe a chave ou habilita a porta já dentro do trigger.
        TrackPlayer(other);
    }

    private void OnTriggerExit(Collider other)
    {
        nearbyPlayers.Remove(other);
    }

    private void TrackPlayer(Collider other)
    {
        if (other.GetComponentInParent<PlayerPickupKey>() != null)
            nearbyPlayers.Add(other);
    }

    private void Update()
    {
        // Teleportes e colliders desabilitados/destruídos podem não enviar OnTriggerExit.
        nearbyPlayers.RemoveWhere(IsOutsideTrigger);

        if (!unlocked)
        {
            foreach (Collider other in nearbyPlayers)
            {
                PlayerPickupKey player = other.GetComponentInParent<PlayerPickupKey>();
                if (player != null && HasRequiredKey(player))
                {
                    unlocked = true;
                    break;
                }
            }
        }

        bool shouldOpen = unlocked && nearbyPlayers.Count > 0;
        if (shouldOpen && openProgress <= 0f)
        {
            // A direção só muda com a porta fechada, evitando saltos durante a passagem.
            foreach (Collider other in nearbyPlayers)
            {
                PlayerPickupKey player = other.GetComponentInParent<PlayerPickupKey>();
                if (player == null) continue;
                float side = transform.InverseTransformPoint(player.transform.position).x - closedCenter.x;
                openingDirection = side >= 0f ? -1f : 1f;
                break;
            }
        }
        if (shouldOpen)
            closeTimer = closeDelay;
        else
            closeTimer = Mathf.Max(0f, closeTimer - Time.deltaTime);

        float target = shouldOpen || closeTimer > 0f ? 1f : 0f;
        openProgress = Mathf.MoveTowards(openProgress, target,
            Time.deltaTime / Mathf.Max(0.01f, animationDuration));

        if (hinge != null)
        {
            float easedProgress = Mathf.SmoothStep(0f, 1f, openProgress);
            hinge.localRotation = Quaternion.Euler(0f, openingDirection * openAngle * easedProgress, 0f);
        }
    }

    private bool HasRequiredKey(PlayerPickupKey player)
    {
        return (!requiresKey01 && !requiresKey02 && !requiresKey03) ||
            (requiresKey01 && player.key01) ||
            (requiresKey02 && player.key02) ||
            (requiresKey03 && player.key03);
    }

    private bool IsOutsideTrigger(Collider other)
    {
        return other == null || !other.enabled || !other.gameObject.activeInHierarchy ||
            !Physics.ComputePenetration(proximityTrigger, proximityTrigger.transform.position,
                proximityTrigger.transform.rotation, other, other.transform.position,
                other.transform.rotation, out _, out _);
    }

    private void OnDisable()
    {
        nearbyPlayers.Clear();
        openProgress = 0f;
        closeTimer = 0f;
        if (hinge != null) hinge.localRotation = Quaternion.identity;
    }
}
