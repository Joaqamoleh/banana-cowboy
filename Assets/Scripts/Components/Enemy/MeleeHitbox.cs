using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{

    Health player = null;

    public bool PerformAttack(int damage, Vector3 knockbackForce)
    {
        if (player != null)
        {
            player.Damage(damage, knockbackForce);
            return true;
        }
        return false;
    }

    public bool PlayerInRange()
    {
        return player != null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.transform.parent != null && other.CompareTag("Player"))
        {
            player = other.transform.parent.GetComponentInParent<Health>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.transform.parent != null && other.transform.parent.GetComponentInParent<Health>() == player)
        {
            player = null;
        }
    }
}
