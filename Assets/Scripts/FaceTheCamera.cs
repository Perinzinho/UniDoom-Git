using UnityEngine;

public class FaceTheCamera : MonoBehaviour
{
    //Script responsável por fazer os objetos estarem sempre virado para os players
    public Transform target;
        public bool useTagPlayer = true; 
    void Start()
    {
        if (target == null)
        {
            FindTarget();
        }
    }

     void FindTarget()
    {
        if (useTagPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }

        }
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        }

    // Update is called once per frame
    void LateUpdate()
    {
    if (target == null)
    {
        FindTarget();
        return;
    }

    transform.LookAt(target.position);
    }
}
