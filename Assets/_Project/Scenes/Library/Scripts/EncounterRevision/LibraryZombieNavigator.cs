using UnityEngine;
using UnityEngine.AI;

// Compatibility component for the existing prefab. Enemy now owns perception and navigation.
[RequireComponent(typeof(Enemy),typeof(NavMeshAgent))]
public sealed class LibraryZombieNavigator : MonoBehaviour
{
    Enemy enemy;
    NavMeshAgent agent;
    bool deathHandled;
    void Awake(){enemy=GetComponent<Enemy>();agent=GetComponent<NavMeshAgent>();}
    void Update()
    {
        if(enemy.IsDead)
        {
            if(!deathHandled)
            {
                if(agent.enabled){if(agent.isOnNavMesh)agent.isStopped=true;agent.enabled=false;}
                // Several bodies in a narrow passage must not permanently lock the objective route.
                foreach(var collider in GetComponentsInChildren<Collider>())collider.enabled=false;
                deathHandled=true;
            }
            return;
        }
    }
}
