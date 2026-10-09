using UnityEngine;

public class Shotgun : Gun
{
    [Header("Shotgun")]

    // Damage dealt by EACH pellet that hits an object.
    [SerializeField] private float damagePerPellet = 10f;

    // Maximum distance each pellet can reach.
    [SerializeField] private float range = 50f;

    // Number of pellets fired per shot.
    // Example: 8 means 8 Raycasts will be created.
    [SerializeField] private int pellets = 8;

    // Controls pellet spread.
    // Higher value = more scattered shots.
    [SerializeField] private float spread = 0.08f;

    // Camera used as origin and direction reference.
    [SerializeField] private Camera playerCamera;


    // Awake is called when the object is initialized by Unity.
    protected override void Awake()
    {
        // Call parent Awake first.
        base.Awake();

        // If no camera was assigned in Inspector, use the main scene camera.
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }


    // Implements abstract Shoot defined in Gun.
    protected override void Shoot()
    {
        DebugUI.Log("Shotgun fired!");

        if (playerCamera == null)
        {
            DebugUI.LogWarning("Shotgun: no camera assigned.");
            return;
        }


        // Repeat shooting process according to pellet count.
        // If pellets = 8, this loop runs 8 times, creating 8 Raycasts in one shot.
        for (int i = 0; i < pellets; i++)
        {
            // Start with camera forward direction.
            Vector3 direction = playerCamera.transform.forward;


            // Add random horizontal spread.
            direction += playerCamera.transform.right *
                         Random.Range(-spread, spread);


            // Add random vertical spread.
            direction += playerCamera.transform.up *
                         Random.Range(-spread, spread);


            // Create the Ray representing one pellet.
            Ray ray = new Ray(
                playerCamera.transform.position,
                direction.normalized
            );


            // Check if this pellet hit any Collider within range.
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                DebugUI.Log(
                    $"Pellet hit: {hit.collider.name} | " +
                    $"Damage: {damagePerPellet}"
                );


                // Future: look for Health component on hit object and apply pellet damage.

                // hit.collider.GetComponent<Health>()?.TakeDamage(damagePerPellet);
            }
        }
    }
}
