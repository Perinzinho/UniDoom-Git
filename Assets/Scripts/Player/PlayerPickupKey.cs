using System;
using UnityEngine;

public class PlayerPickupKey : MonoBehaviour
{
    public bool key01 = false;
    public bool key02 = false;
    public bool key03 = false;


    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Key01")
        {
            key01 = true;
            Destroy(other.gameObject);
        }

        if (other.tag == "Key02")
        {
            key02 = true;
            Destroy(other.gameObject);
        }

        if (other.tag == "Key03")
        {
            key03 = true;
            Destroy(other.gameObject);
        }
    }
}
