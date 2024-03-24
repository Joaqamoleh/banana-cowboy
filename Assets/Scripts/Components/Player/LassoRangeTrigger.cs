using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LassoRangeTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null) return;

        LassoObject lo = other.gameObject.GetComponentInParent<LassoObject>();
        if (lo != null)
        {
            lo.isInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || other.gameObject == null) return;

        LassoObject lo =  other.gameObject.GetComponentInParent<LassoObject>();
        if (lo != null)
        {
            lo.isInRange = false;
        }
    }
}
