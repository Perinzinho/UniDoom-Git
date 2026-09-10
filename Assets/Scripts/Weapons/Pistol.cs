using UnityEngine;

public class Pistol : Gun
{
    [Header("Pistol")]

    // Damage dealt by a single pistol shot.
    [SerializeField] private float damage = 20f;

    // Maximum distance the pistol shot can reach.
    [SerializeField] private float range = 100f;

    // Camera used as origin and direction for shooting.
    [SerializeField] private Camera playerCamera;


    // Awake is called when the object is initialized.
    protected override void Awake()
    {
        // Call parent Awake first.
        base.Awake();

        // If no camera was assigned in the Inspector, use the main camera.
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }


    // Implementation of abstract Shoot defined in Gun.
    protected override void Shoot()
    {
        DebugUI.Log("Pistol fired!");

        if (playerCamera == null)
        {
            DebugUI.LogWarning("Pistol: no camera assigned.");
            return;
        }


        // Create a ray starting at camera position and going forward.
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );


        // Cast the ray and check if it hit any collider.
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            DebugUI.Log(
                $"Pistol hit: {hit.collider.name} | Damage: {damage}"
            );


            // Future: look for a Health component on hit object and apply damage.

            // hit.collider.GetComponent<Health>()?.TakeDamage(damage);
        }
    }
}
