using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonSafeZoneTrigger : MonoBehaviour
{
    [SerializeField]
    CannonController controller;

    bool hasBeenActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != null && other.gameObject.CompareTag("Player") && !hasBeenActivated)
        {
            hasBeenActivated = true;
            controller.ShouldFireBombs(false);
        }    
    }
}
