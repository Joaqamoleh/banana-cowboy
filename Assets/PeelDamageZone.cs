using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeelDamageZone : MonoBehaviour
{
    public int index;
    public string nameOfCondition;
    public float knockbackForce;
    public OrangeBoss orangeBoss;

    private void Start()
    {
        orangeBoss = GameObject.Find("Orange Boss").GetComponent<OrangeBoss>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 knockbackDirection = (other.gameObject.transform.position - transform.position).normalized;
            knockbackDirection.y = 0;
            other.gameObject.GetComponentInParent<Health>().Damage(1, knockbackForce * knockbackDirection);
            orangeBoss.ResetPeel(index, nameOfCondition);
            orangeBoss.PlayDialogue("Whoa, watch your step there! Looks like you couldn't handle the zest!", false);
        }
    }
}
