using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiltingPlatform : MonoBehaviour
{
    [SerializeField]
    Transform pivotTransform;

    public enum RotationDireciton
    {
        X, Y, Z
    }

    [SerializeField]
    RotationDireciton rotationDirection;
    [SerializeField]
    float delayToActivate = 1f, rotationAmount = 90f, rotationSpeed = 0.01f, rotationReactivationSpeed = 0.03f, delayToReactivate = 1.3f;

    bool activated = false;
    bool reseting = false;

    private void Update()
    {
        if (activated)
        {
            Quaternion targetRot = Quaternion.AngleAxis(rotationAmount, GetRotationAxis()) * Quaternion.identity;
            pivotTransform.rotation = Quaternion.Slerp(pivotTransform.rotation, targetRot, rotationSpeed);
            if (pivotTransform.rotation == targetRot)
            {
                Invoke("ResetTilt", delayToReactivate);
                activated = false;
            }
        }
        else if (reseting)
        {
            Quaternion targetRot = Quaternion.AngleAxis(0f, GetRotationAxis()) * Quaternion.identity;
            pivotTransform.rotation = Quaternion.Slerp(pivotTransform.rotation, targetRot, rotationReactivationSpeed);
            if (pivotTransform.rotation == targetRot)
            {
                reseting = false;
            }
        }
    }

    void Activate()
    {
        activated = true;
    }

    void ResetTilt()
    {
        reseting = true;
    }
    
    Vector3 GetRotationAxis()
    {
        switch(rotationDirection)
        {
            case RotationDireciton.X:
                return Vector3.right;
            case RotationDireciton.Y:
                return Vector3.right;
            case RotationDireciton.Z:
                return Vector3.forward;
            default:
                return Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject != null && collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            Invoke("Activate", delayToActivate);
        }
    }
}
