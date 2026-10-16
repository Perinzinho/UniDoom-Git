using UnityEngine;
using UnityEngine.AI;

// Local adapter: retain the existing enemy's health, weapons interaction and damage.
[RequireComponent(typeof(Enemy),typeof(NavMeshAgent))]
public sealed class LibraryZombieNavigator : MonoBehaviour
{
    Enemy enemy;
    NavMeshAgent agent;
    Transform target;
    float nextPathUpdate;
    bool deathHandled;
    void Awake(){enemy=GetComponent<Enemy>();agent=GetComponent<NavMeshAgent>();}
    void Start(){var player=GameObject.FindGameObjectWithTag("Player");if(player)target=player.transform;}
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
        if(!target||!agent.enabled||!agent.isOnNavMesh)return;
        if(Time.time>=nextPathUpdate){nextPathUpdate=Time.time+.25f;agent.SetDestination(target.position);}
        bool blocked=Physics.Linecast(transform.position+Vector3.up*.35f,target.position+Vector3.up, out var hit,~0,QueryTriggerInteraction.Ignore)
            &&!hit.transform.IsChildOf(transform)&&hit.transform!=target&&!hit.transform.IsChildOf(target);
        // The original enemy attacks by distance; suppress its update when furniture blocks contact.
        enemy.enabled=!blocked;
    }
}
