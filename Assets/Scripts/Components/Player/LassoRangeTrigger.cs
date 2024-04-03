using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LassoRangeTrigger : MonoBehaviour
{
    [SerializeField]
    DetectionTriggerHandler enterRange, exitRange;

    private void Awake()
    {
        Debug.Assert(enterRange != null);
        Debug.Assert(exitRange != null);
        enterRange.OnTriggerEntered += TriggerEntered;
        exitRange.OnTriggerExited += TriggerExited;
    }
    private void TriggerEntered(Collider other)
    {
        if (other == null || other.gameObject == null) return;

        LassoObject lo = other.gameObject.GetComponentInParent<LassoObject>();
        if (lo != null)
        {
            lo.isInRange = true;
        }
    }

    private void TriggerExited(Collider other)
    {
        if (other == null || other.gameObject == null) return;

        LassoObject lo =  other.gameObject.GetComponentInParent<LassoObject>();
        if (lo != null)
        {
            lo.isInRange = false;
        }
    }
}
