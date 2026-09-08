using UnityEngine;

public class Pistol : Gun
{
    [Header("Pistol")]

    // Dano causado por um disparo da pistola.
    [SerializeField] private float damage = 20f;

    // Distância máxima que o tiro da pistola consegue atingir.
    [SerializeField] private float range = 100f;

    // Câmera usada como origem e direção do disparo.
    [SerializeField] private Camera playerCamera;


    // Awake é executado pela Unity quando o objeto é inicializado.
    protected override void Awake()
    {
        // Executa primeiro o Awake da classe pai (Gun).
        base.Awake();

        // Caso nenhuma câmera tenha sido configurada pelo Inspector, ele usa a câmera principal da cena.
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }


    // Implementação do Shoot definido como abstract na classe Gun.
    protected override void Shoot()
    {
        DebugUI.Log("Pistola disparou!");

        if (playerCamera == null)
        {
            DebugUI.LogWarning("Pistol: nenhuma câmera foi configurada.");
            return;
        }


        // Cria um Ray (raio invisível) que começa na posição da câmera e segue para a direção em que a câmera está olhando.
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );


        // Dispara o Ray e verifica se ele atingiu algum Collider.
        //
        // ray   = origem e direção do tiro
        // hit   = guarda as informações do objeto atingido
        // range = distância máxima que o tiro pode alcançar
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            DebugUI.Log(
                $"Pistola acertou: {hit.collider.name} | Dano: {damage}"
            );


            // Futuramente podemos procurar um componente Health
            // no objeto atingido e aplicar o dano da pistola.

            // hit.collider.GetComponent<Health>()?.TakeDamage(damage);
        }
    }
}