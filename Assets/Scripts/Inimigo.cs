using UnityEngine;
using UnityEngine.Events;

public class Inimigo : MonoBehaviour, IDanificavel
{
    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;

    [Header("Movimento")]
    [SerializeField] private float velocidade = 3f;

    [Header("Ataque")]
    [SerializeField] private float danoAtaque = 10f;
    [SerializeField] private float alcanceAtaque = 1.5f;
    [SerializeField] private float cooldownAtaque = 1f;

    [Header("Eventos (opcional, pra outros scripts ouvirem)")]
    public UnityEvent<float, float> OnVidaAlterada; // (atual, maxima)
    public UnityEvent<GameObject> OnMorte;          // quem matou

    private Transform alvo;
    private float ultimoAtaque;

    public bool EstaMorto { get; private set; }

    private void Awake()
    {
        vidaAtual = vidaMaxima;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            alvo = player.transform;
    }

    private void Update()
    {
        if (EstaMorto || alvo == null) return;

        float distancia = Vector3.Distance(transform.position, alvo.position);

        if (distancia <= alcanceAtaque)
        {
            TentarAtacar();
        }
        else
        {
            MoverEmDirecaoAoAlvo();
        }
    }

    private void MoverEmDirecaoAoAlvo()
    {
        Vector3 direcao = (alvo.position - transform.position).normalized;
        transform.position += direcao * velocidade * Time.deltaTime;

        if (direcao != Vector3.zero)
            transform.forward = direcao;
    }

    private void TentarAtacar()
    {
        if (Time.time - ultimoAtaque < cooldownAtaque) return;
        ultimoAtaque = Time.time;

        if (alvo.TryGetComponent<IDanificavel>(out var alvoDanificavel))
        {
            alvoDanificavel.ReceberDano(danoAtaque, gameObject);
        }
    }

    // ---- Implementação da interface IDanificavel ----

    public void ReceberDano(float quantidade, GameObject origem = null)
    {
        if (EstaMorto || quantidade <= 0f) return;

        vidaAtual = Mathf.Max(0f, vidaAtual - quantidade);
        OnVidaAlterada?.Invoke(vidaAtual, vidaMaxima);

        if (vidaAtual <= 0f)
        {
            Morrer(origem);
        }
    }

    private void Morrer(GameObject responsavel)
    {
        EstaMorto = true;
        OnMorte?.Invoke(responsavel);
        Destroy(gameObject, 0.1f);
    }
}