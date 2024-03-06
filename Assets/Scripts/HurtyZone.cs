using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtyZone : MonoBehaviour
{
    // in-editor variables
    [Header("Other")]
    public bool kill;
    public Vector3 knockback;
    public int damage;

    private void OnTriggerEnter(Collider other)
    {
        // check if the collided entity is the player
        if (other.CompareTag("Player"))
        {
            if (kill)
            {
                // kill the player
                // hopefully doesnt bug out lmao
                other.GetComponentInParent<Health>().Damage(999, Vector3.zero);
            }
            else
            {
                // damage the player
                other.GetComponentInParent<Health>().Damage(damage, knockback);
            }
        }
        else if (other.gameObject != null &&  other.gameObject.GetComponentInParent<LassoTossable>() != null)
        {
            other.gameObject.GetComponentInParent<LassoTossable>().TossInDirection(Vector3.zero, Vector3.up, LassoTossable.TossStrength.WEAK);
        }

    }
}
