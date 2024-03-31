using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionTriggerHandler : MonoBehaviour
{
    public delegate void TriggerCollision(Collider c);
    public event TriggerCollision OnTriggerEntered;
    public event TriggerCollision OnTriggerExited;

    public void OnTriggerEnter(Collider other)
    {
        OnTriggerEntered?.Invoke(other);
    }

    public void OnTriggerExit(Collider other)
    {
        OnTriggerExited?.Invoke(other);
    }
}
