using System;
using UnityEngine;
using UniDoom.UI;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : SFXPlayer
{
    [Header("Sons por superfície")]
    [SerializeField] private SoundBank woodSteps;
    [SerializeField] private SoundBank concreteSteps;
    [SerializeField] private SoundBank ecoSteps;
    [SerializeField] private SoundBank asphaltSteps;
    [SerializeField] private SoundBank grassSteps;

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
    private PlayerLife life;
    private readonly RaycastHit[] groundHits = new RaycastHit[24];
    public FootstepSurfaceType CurrentSurface { get; private set; }

    protected override void Awake()
    {
        base.Awake(); // cria o pool de AudioSources
        controller = GetComponent<CharacterController>();
        life = GetComponent<PlayerLife>();
        // Persist with the existing player across scenes; no second audio/player system.
        if (asphaltSteps == null || asphaltSteps.IsEmpty) asphaltSteps = LoadSteps("Pavement", .8f);
        if (grassSteps == null || grassSteps.IsEmpty) grassSteps = LoadSteps("Grass", .85f);
    }

    static SoundBank LoadSteps(string surface, float volume) => new SoundBank
    {
        clips = Resources.LoadAll<AudioClip>("Audio/Footsteps/" + surface),
        volume = volume,
        basePitch = 1f,
        pitchVariation = .06f,
        volumeVariation = .1f
    };

    void LateUpdate()
    {
        if (!controller.enabled || Time.timeScale <= 0f || GameMenus.BlocksGameplay ||
            DoorReloadScreen.IsReloading || (life != null && life.IsDead))
        {
            ResetStride();
            return;
        }
        bool grounded = controller.isGrounded;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;

        HandleLanding(grounded);

        if (!grounded)
        {
            distanceAccumulated = 0f;
            isFirstStep = true;
            return;
        }

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

    void OnDisable() => ResetStride();

    void ResetStride()
    {
        distanceAccumulated = 0f;
        isFirstStep = true;
        wasGrounded = true;
        airTime = 0f;
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

        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, distance,
            groundMask, QueryTriggerInteraction.Ignore);
        RaycastHit hit = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var candidate = groundHits[i];
            if (candidate.transform.IsChildOf(transform) || candidate.normal.y < .5f || candidate.distance >= nearest) continue;
            hit = candidate;
            nearest = candidate.distance;
        }
        if (hit.collider == null)
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
        CurrentSurface = FootstepSurface.Resolve(col);
        SoundBank bank = CurrentSurface switch
        {
            FootstepSurfaceType.Grass => grassSteps,
            FootstepSurfaceType.Asphalt => asphaltSteps,
            FootstepSurfaceType.Wood => woodSteps,
            FootstepSurfaceType.Interior => ecoSteps,
            _ => concreteSteps
        };
        return bank != null && !bank.IsEmpty ? bank : concreteSteps;
    }
}
