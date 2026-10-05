using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : MonoBehaviour
{
    [Header("Sons por superfície")]
    [SerializeField] private AudioClip[] woodSteps;
    [SerializeField] private AudioClip[] concreteSteps;
    [SerializeField] private AudioClip[] ecoSteps;

    [Header("Áudio")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.9f;
    [SerializeField, Range(0f, 0.5f)] private float volumeVariation = 0.15f;
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.08f;
    [SerializeField, Range(0f, 1f)] private float minSpeedVolume = 0.4f; // volume quando anda bem devagar

    [Header("Sincronia com o movimento")]
    [Tooltip("Distância (em unidades) entre um passo e o próximo. Menor = passos mais rápidos.")]
    [SerializeField] private float strideLength = 2.5f;
    [Tooltip("Velocidade considerada 'máxima' (a mesma do PlayerMovement). Usada para escalar o volume.")]
    [SerializeField] private float referenceSpeed = 6f;
    [SerializeField] private float minSpeed = 0.5f; // abaixo disso conta como parado

    [Header("Aterrissagem")]
    [SerializeField] private float landMinAirTime = 0.2f; // tempo mínimo no ar para tocar som de queda

    [Header("Detecção de chão")]
    [SerializeField] private float rayExtraDistance = 0.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    /// <summary>Disparado a cada passo. Útil para sincronizar head bob, partículas, etc. (parâmetro = velocidade)</summary>
    public event Action<float> OnFootstep;

    private CharacterController controller;

    private float distanceAccumulated;
    private bool isFirstStep = true;

    private bool wasGrounded = true;
    private float airTime;

    private ShuffleBag woodBag;
    private ShuffleBag concreteBag;
    private ShuffleBag ecoBag;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();

        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.spatialBlend = 0f; // 2D

        woodBag     = new ShuffleBag(woodSteps);
        concreteBag = new ShuffleBag(concreteSteps);
        ecoBag      = new ShuffleBag(ecoSteps);
    }

    void Update()
    {
        bool grounded = controller.isGrounded;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;

        HandleLanding(grounded);

        if (!grounded) return; // no ar não tem passo (mantém a distância acumulada)

        if (speed < minSpeed)
        {
            // parou: o próximo movimento recomeça o ciclo de passos
            distanceAccumulated = 0f;
            isFirstStep = true;
            return;
        }

        // Acumula a distância realmente percorrida neste frame
        distanceAccumulated += speed * Time.deltaTime;

        // O 1º passo toca na metade da passada (o pé leva meia passada para pousar)
        float threshold = isFirstStep ? strideLength * 0.5f : strideLength;

        if (distanceAccumulated >= threshold)
        {
            distanceAccumulated -= threshold; // guarda a sobra, sem perder precisão
            isFirstStep = false;

            PlayFootstep(speed);
            OnFootstep?.Invoke(speed);
        }
    }

    private void HandleLanding(bool grounded)
    {
        if (!grounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            if (!wasGrounded && airTime >= landMinAirTime)
                PlayLanding();

            airTime = 0f;
        }

        wasGrounded = grounded;
    }

    private void PlayFootstep(float speed)
    {
        // Passos mais lentos = mais suaves; correndo = mais fortes
        float speedRatio = Mathf.Clamp01(speed / referenceSpeed);
        float speedVolume = Mathf.Lerp(minSpeedVolume, 1f, speedRatio);
        float vol = volume * speedVolume * (1f - UnityEngine.Random.Range(0f, volumeVariation));

        Play(vol, 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation));
    }

    private void PlayLanding()
    {
        // Mais forte e mais grave que um passo comum
        Play(1f, 0.9f + UnityEngine.Random.Range(-pitchVariation, pitchVariation));

        if (debugLogs) Debug.Log($"Footstep: aterrissagem após {airTime:F2}s no ar");
    }

    private void Play(float vol, float pitch)
    {
        Vector3 origin = controller.bounds.center;
        float distance = controller.bounds.extents.y + rayExtraDistance;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask))
        {
            if (debugLogs) Debug.LogWarning("Footstep: raycast não acertou nada");
            return;
        }

        ShuffleBag bag = GetBagForSurface(hit.collider);
        if (bag == null || bag.IsEmpty)
        {
            if (debugLogs) Debug.LogWarning($"Footstep: tag '{hit.collider.tag}' não reconhecida ou lista vazia");
            return;
        }

        AudioClip clip = bag.Next();

        if (debugLogs) Debug.Log($"Footstep: '{hit.collider.name}' ({hit.collider.tag}) vol {vol:F2}");

        footstepSource.pitch = pitch;
        footstepSource.PlayOneShot(clip, vol);
    }

    private ShuffleBag GetBagForSurface(Collider col)
    {
        if (col.CompareTag("Wood"))     return woodBag;
        if (col.CompareTag("Concrete")) return concreteBag;
        if (col.CompareTag("Eco"))      return ecoBag;
        return null;
    }

    // Toca todos os clips em ordem aleatória antes de repetir qualquer um,
    // e nunca repete o mesmo clip duas vezes seguidas.
    private class ShuffleBag
    {
        private readonly AudioClip[] clips;
        private readonly List<int> order = new List<int>();
        private int position;
        private int lastIndex = -1;

        public bool IsEmpty => clips == null || clips.Length == 0;

        public ShuffleBag(AudioClip[] clips)
        {
            this.clips = clips;
            Refill();
        }

        public AudioClip Next()
        {
            if (clips.Length == 1) return clips[0];

            if (position >= order.Count)
                Refill();

            lastIndex = order[position++];
            return clips[lastIndex];
        }

        private void Refill()
        {
            order.Clear();
            position = 0;

            if (IsEmpty) return;

            for (int i = 0; i < clips.Length; i++)
                order.Add(i);

            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            if (order.Count > 1 && order[0] == lastIndex)
            {
                int j = UnityEngine.Random.Range(1, order.Count);
                (order[0], order[j]) = (order[j], order[0]);
            }
        }
    }
}