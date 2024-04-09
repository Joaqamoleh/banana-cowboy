using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DetectionTriggerHandler;

public class MeleeHitbox : MonoBehaviour
{

    Health player = null;

    private bool damagePlayer;
    private Vector3 knockbackRef;

    public void SetKnockback(Vector3 knockbackForce)
    {
        knockbackRef = knockbackForce;
    }

    public void SetDamagePlayer(bool val)
    {
        damagePlayer = val;
    }

    public bool PlayerInRange()
    {
        return player != null;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != null && other.transform.parent != null && other.CompareTag("Player"))
        {
            player = other.transform.parent.GetComponentInParent<Health>();
        }
    }

    public void PerformAttack()
    {
        if (damagePlayer)
        {
            player.Damage(0, knockbackRef);
        }
    }

    public void AttackEnded()
    {
        player = null;
    }
}
