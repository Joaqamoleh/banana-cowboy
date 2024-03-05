using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeelDamageZone : MonoBehaviour
{
    public int index;
    public string nameOfCondition;
    public float knockbackForce;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 knockbackDirection = (other.gameObject.transform.position - transform.position).normalized;
            knockbackDirection.y = 0;
            other.gameObject.GetComponentInParent<Health>().Damage(1, knockbackForce * knockbackDirection);
            GameObject.Find("Orange Boss").GetComponent<OrangeBoss>().ResetPeel(index, nameOfCondition);
        }
    }
}
