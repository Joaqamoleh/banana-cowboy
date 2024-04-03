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
    [SerializeField, Range(0f, 50f)]
    float heightOffset = 0f;

    [SerializeField, Tooltip("Set this only if you want to override manual orbitRotationAngle")]
    Transform focusElement;
    [SerializeField]
    bool viewGizmosAtPlayer = true;


    [SerializeField]
    bool forceReorientation = false;
    [SerializeField]
    Transform overrideCameraTarget;
    public bool topDown;
    [SerializeField]
    float forceReorientationCooldown = 8.0f;
    float lastForcedReorientationTime;

    [SerializeField]
    int priority = 0;

    private void OnDrawGizmosSelected()
    {
        Transform basis = GetOrientationBasis();
        if (basis == null) return;

        Vector3 lookPos = transform.position;
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null && viewGizmosAtPlayer)
        {
            lookPos = pc.transform.position;
        }

        Quaternion lookRot = basis.rotation * Quaternion.Euler(new Vector2(GetOrbitVertAngle(), GetOrbitRotationAngle(lookPos)));
        Vector3 lookDir = lookRot * Vector3.forward;
        Vector3 position = lookPos - lookDir * orbitDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, lookPos);
        Gizmos.DrawSphere(position, 0.3f);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(lookPos, orbitDistance);
    }

    private void Awake()
    {
        lastForcedReorientationTime = -forceReorientationCooldown;
    }

    public float GetOrbitVertAngle()
    {
        return orbitVerticalAngle;
    }

    public float GetOrbitRotationAngle(Vector3 cameraFocusPos)
    {
        if (focusElement != null)
        {
            Transform basis = GetOrientationBasis();
            Vector3 dir = basis.InverseTransformDirection(Vector3.ProjectOnPlane((focusElement.position - cameraFocusPos), basis.up)).normalized;
            float angle = Mathf.Acos(dir.z) * Mathf.Rad2Deg;
            angle = dir.x < 0 ? 360f - angle : angle;
            return angle;
        } 
        return orbitRotationAngle;
    }

    public bool ShouldPerformForcedOrientation()
    {
        return forceReorientation && Time.time - lastForcedReorientationTime > forceReorientationCooldown;
    }

    public void UpdateForceOrientationTimer()
    {
        lastForcedReorientationTime = Time.time;
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

    public Vector3 GetOverrideTargetPos()
    {
        if (overrideCameraTarget == null || !topDown)
        {
            return Vector3.zero;
        }
        else
        {
            return overrideCameraTarget.position;
        }
    }

    public float GetHeightOffset()
    {
        return heightOffset;
    }

    public int GetPriority()
    {
        return priority;
    }

    public void SetCameraValues(float distance, float vAngle, float height, bool forceRotate)
    {
        orbitDistance = distance;
        orbitVerticalAngle = vAngle;
        heightOffset = height;
        forceReorientation = forceRotate;// TODO: Not sure if need this, doesn't really do anything for now.
    }
}
