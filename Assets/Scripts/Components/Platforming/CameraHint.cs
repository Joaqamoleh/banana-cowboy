using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHint : MonoBehaviour
{
    [SerializeField]
    Transform orientationBasis;

    [SerializeField, Min(0f)]
    float orbitDistance = 30f;
    [SerializeField, Range(-89f, 89)]
    float orbitVerticalAngle = 45f;
    [SerializeField, Range(0f, 359f)]
    float orbitRotationAngle = 0f;
    [SerializeField]
    int priority = 0;

    private void OnDrawGizmosSelected()
    {
        Transform basis = orientationBasis;
        if (basis == null)
        {
            basis = transform;
        }
        Vector3 lookPos = transform.position;
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            lookPos = pc.transform.position;
        }

        Quaternion lookRot = basis.rotation * Quaternion.Euler(new Vector2(orbitVerticalAngle, orbitRotationAngle));
        Vector3 lookDir = lookRot * Vector3.forward;
        Vector3 position = lookPos - lookDir * orbitDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, lookPos);
        Gizmos.DrawSphere(position, 0.3f);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(lookPos, orbitDistance);
    }

    public float GetOrbitVertAngle()
    {
        return orbitVerticalAngle;
    }

    public float GetOrbitRotationAngle()
    {
        return orbitRotationAngle;
    }

    public float GetOrbitDistance()
    {
        return orbitDistance;
    }
    public Transform GetOrientationBasis()
    {
        if (orientationBasis == null)
        {
            return transform;
        }
        return orientationBasis;
    }

    public int GetPriority()
    {
        return priority;
    }
}
