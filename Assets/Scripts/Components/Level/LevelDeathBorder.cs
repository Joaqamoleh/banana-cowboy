using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDeathBorder : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null)
        {
            if (other.gameObject.GetComponentInParent<Health>() != null)
            {
                other.gameObject.GetComponentInParent<Health>().Damage(999, Vector3.zero);
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }
}
