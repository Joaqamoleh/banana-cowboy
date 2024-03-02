using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHint : MonoBehaviour
{
    [SerializeField]
    Transform desiredOrientation;

    [SerializeField, Min(0f)]
    float targetHeightAbovePlayer = 3f, targetDistanceFromPlayer = 30f;
    [SerializeField]
    float camReorientTime = 0.2f, camMaxSpeed = 40f;
    [SerializeField, Range(0.01f, 10.0f)]
    float zoomSpeed = 1f, rotationSpeed = 1f;
    [SerializeField]
    int priority = 0;

    float rotationT = 0f, zoomT = 0f;

    public void ApplyHint(Transform camCurrent, Transform characterOrient, ref Vector3 camVel, CinemachineVirtualCamera cam)
    {
        Vector3 desiredPos = characterOrient.position + targetHeightAbovePlayer * characterOrient.up;
        camCurrent.position = Vector3.SmoothDamp(camCurrent.position, desiredPos, ref camVel, camReorientTime, camMaxSpeed);
        camCurrent.rotation = Quaternion.Slerp(camCurrent.rotation, desiredOrientation.rotation, rotationT);
        if (cam != null)
        {
            Cinemachine3rdPersonFollow ccb = cam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (ccb != null)
            {
                ccb.CameraDistance = Mathf.Lerp(ccb.CameraDistance, targetDistanceFromPlayer, zoomT);
            }
        }
        rotationT += rotationSpeed * Time.deltaTime;
        zoomT += zoomSpeed * Time.deltaTime;
    }

    public void ResetHintTime()
    {
        rotationT = 0f;
        zoomT = 0f;
    }

    public int GetPriority()
    {
        return priority;
    }
}
