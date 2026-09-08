using UnityEngine;

public class Shotgun : Gun
{
    [Header("Shotgun")]

    // Dano causado por CADA pellet que acertar um objeto.
    [SerializeField] private float damagePerPellet = 10f;

    // Distância máxima que cada pellet consegue atingir.
    [SerializeField] private float range = 50f;

    // Quantidade de pellets disparados em um único tiro.
    // Exemplo: 8 significa que serão criados 8 Raycasts.
    [SerializeField] private int pellets = 8;

    // Controla a dispersão dos pellets.
    // Quanto maior esse valor, mais espalhados serão os tiros.
    [SerializeField] private float spread = 0.08f;

    // Câmera utilizada como origem e referência de direção do disparo.
    [SerializeField] private Camera playerCamera;


    // Awake é chamado quando o objeto é inicializado pela Unity.
    protected override void Awake()
    {
        // Executa primeiro o Awake da classe pai Gun.
        base.Awake();

        // Se nenhuma câmera foi configurada manualmente no Inspector, usa a principal da cena.
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }


    // Implementa o Shoot abstrato definido na classe Gun.
    protected override void Shoot()
    {
        DebugUI.Log("Shotgun disparou!");

        if (playerCamera == null)
        {
            DebugUI.LogWarning("Shotgun: nenhuma câmera foi configurada.");
            return;
        }


        // Repete o processo de disparo de acordo com
        // a quantidade de pellets configurada.
        //
        // Se pellets = 8, esse for executará 8 vezes,
        // criando 8 Raycasts em um único disparo.
        for (int i = 0; i < pellets; i++)
        {
            // Começamos utilizando a direção para frente da câmera.
            Vector3 direction = playerCamera.transform.forward;


            // Adiciona uma variação aleatória para a direita/esquerda.
            // Isso cria a dispersão horizontal do pellet.
            direction += playerCamera.transform.right *
                         Random.Range(-spread, spread);


            // Adiciona uma variação aleatória para cima/baixo.
            // Isso cria a dispersão vertical do pellet.
            direction += playerCamera.transform.up *
                         Random.Range(-spread, spread);


            // Cria o Ray que representa um dos pellets.
            //
            // Ele começa na posição da câmera e segue
            // na direção calculada anteriormente.
            Ray ray = new Ray(
                playerCamera.transform.position,
                direction.normalized
            );


            // Verifica se este pellet atingiu algum Collider
            // dentro da distância máxima definida em range.
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                DebugUI.Log(
                    $"Pellet acertou: {hit.collider.name} | " +
                    $"Dano: {damagePerPellet}"
                );


                // Futuramente podemos procurar um componente Health
                // no objeto atingido e aplicar o dano deste pellet.

                // hit.collider.GetComponent<Health>()?.TakeDamage(damagePerPellet);
            }
        }
    }
}