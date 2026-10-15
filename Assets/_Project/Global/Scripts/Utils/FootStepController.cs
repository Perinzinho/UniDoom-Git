using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : SFXPlayer
{
    [Header("Sons por superfície")]
    [SerializeField] private SoundBank woodSteps;
    [SerializeField] private SoundBank concreteSteps;
    [SerializeField] private SoundBank ecoSteps;

    [Header("Volume por velocidade")]
    [SerializeField, Range(0f, 1f)] private float minSpeedVolume = 0.4f;

    [Header("Sincronia com o movimento")]
    [SerializeField] private float strideLength = 2.5f;
    [SerializeField] private float referenceSpeed = 6f;
    [SerializeField] private float minSpeed = 0.5f;

    [Header("Aterrissagem")]
    [SerializeField] private float landMinAirTime = 0.2f;

    [Header("Detecção de chão")]
    [SerializeField] private float rayExtraDistance = 0.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public event Action<float> OnFootstep;

    private CharacterController controller;
    private float distanceAccumulated;
    private bool isFirstStep = true;
    private bool wasGrounded = true;
    private float airTime;

    protected override void Awake()
    {
        base.Awake(); // cria o pool de AudioSources
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        bool grounded = controller.isGrounded;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;

        HandleLanding(grounded);

        if (!grounded) return;

        if (speed < minSpeed)
        {
            distanceAccumulated = 0f;
            isFirstStep = true;
            return;
        }

        distanceAccumulated += speed * Time.deltaTime;

        float threshold = isFirstStep ? strideLength * 0.5f : strideLength;

        if (distanceAccumulated >= threshold)
        {
            distanceAccumulated -= threshold;
            isFirstStep = false;

            PlayFootstep(speed);
            OnFootstep?.Invoke(speed);
        }
    }

    private void HandleLanding(bool grounded)
    {
        if (!grounded)
            airTime += Time.deltaTime;
        else
        {
            if (!wasGrounded && airTime >= landMinAirTime)
                PlayFromSurface(1.2f); // aterrissagem: mais forte

            airTime = 0f;
        }

        wasGrounded = grounded;
    }

    private void PlayFootstep(float speed)
    {
        float speedRatio = Mathf.Clamp01(speed / referenceSpeed);
        float speedVolume = Mathf.Lerp(minSpeedVolume, 1f, speedRatio);
        PlayFromSurface(speedVolume);
    }

    private void PlayFromSurface(float volumeMultiplier)
    {
        Vector3 origin = controller.bounds.center;
        float distance = controller.bounds.extents.y + rayExtraDistance;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance,
                             groundMask, QueryTriggerInteraction.Ignore))
        {
            if (debugLogs) Debug.LogWarning("Footstep: raycast não acertou nada");
            return;
        }

        SoundBank bank = GetBankForSurface(hit.collider);
        if (bank == null || bank.IsEmpty)
        {
            if (debugLogs) Debug.LogWarning($"Footstep: tag '{hit.collider.tag}' não reconhecida ou vazia");
            return;
        }

        if (debugLogs) Debug.Log($"Footstep: '{hit.collider.name}' ({hit.collider.tag})");

        Play(bank, volumeMultiplier);
    }

    protected virtual SoundBank GetBankForSurface(Collider col)
    {
        if (col.CompareTag("Wood"))     return woodSteps;
        if (col.CompareTag("Concrete")) return concreteSteps;
        if (col.CompareTag("Eco"))      return ecoSteps;
        return null;
    }
}