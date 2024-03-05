using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    CinemachineVirtualCamera _cinemachineCamController;
    [SerializeField]
    Transform _cameraPivot = null, _cameraTarget = null, _emptyGameObjectPrefab;
    Transform _cameraCurrent = null;


    GravityObject _playerGravity;
    Transform _characterOrientation;

    [Header("Parameters")]
    [SerializeField, Range(0f, 1f)]
    float bottomBorder = 0.25f, topBorder = 0.85f;
    [SerializeField, Range(0f, 30f)]
    float camReorientTime = 0.5f, camMaxSpeed = 5f;
    private Vector3 camVel = Vector3.zero;

    public const float orbitSensitivityMin = 0.5f, orbitSensitivityMax = 4f; // For the slider in menu
    [SerializeField, Range(orbitZoomSensitivityMin, orbitZoomSensitivityMax)]
    public float orbitSensitivity = 0.2f;

    public const float orbitZoomSensitivityMin = 5f, orbitZoomSensitivityMax = 50f; // For the slider in menu
    [SerializeField, Range(orbitZoomSensitivityMin, orbitZoomSensitivityMax)]
    public float orbitZoomSensitivity = 10f;

    [SerializeField, Min(0f)]
    float orbitMinRadius = 20f, orbitMaxRadius = 60f, orbitRadius = 30f;

    public bool invertY = true, invertZoom = false;


    [Header("Input")]
    [SerializeField]
    KeyCode rotationKey = KeyCode.Mouse1;
    
    private float mouseX, mouseY, mouseScroll;
    private bool rotationHeld = false, ignoreActiveHint = false;
    List<CameraHint> activeHints = new List<CameraHint>();
    int highestHintPrioIndex = -1;

    private int _validTouchID = -1;
    public static int uiTouching;

    void Start()
    {
        // Removes cursor from screen and keeps it locked to center
        _playerGravity = GetComponent<GravityObject>();
        _characterOrientation = _playerGravity.characterOrientation;
        _cameraCurrent = Instantiate(_emptyGameObjectPrefab);
        _cameraCurrent.position = _cameraTarget.position;

        // Overrides the camera's default radius
        if (_cinemachineCamController != null)
        {
            _cinemachineCamController.Follow = _cameraCurrent;
            Cinemachine3rdPersonFollow ccb = _cinemachineCamController.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (ccb != null)
            {
                ccb.CameraDistance = orbitRadius;
            }
        }

        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += DisableForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += EnableAfterCutscene;
        }
    }

    void Update()
    {
        if (_cameraTarget == null) { return; }
        if (!PauseManager.pauseActive)
        {
#if !UNITY_IOS && !UNITY_ANDROID
            mouseX = Input.GetAxisRaw("Mouse X");
            mouseY = Input.GetAxisRaw("Mouse Y");
            mouseScroll = -Input.GetAxisRaw("Mouse ScrollWheel");
            if (invertY)
            {
                mouseY = -mouseY;
            }
            
            if (invertZoom)
            {
                mouseScroll = -mouseScroll;
            }

            if (Input.GetKeyDown(rotationKey))
            {
                PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.CAMERA_PAN);
                ignoreActiveHint = true;
                rotationHeld = true;
            } 
            else if (Input.GetKeyUp(rotationKey))
            {
                PlayerCursor.GoBackToPreviousCursorType();
                rotationHeld = false;
            }
#else
            CameraMovementMobile();
#endif
        }
    }

    private void FixedUpdate()
    {
        if (rotationHeld)
        {
            ApplyInputRotation();
        } 
        else if (highestHintPrioIndex != -1 && !ignoreActiveHint)
        {
            // Apply camera hints
            activeHints[highestHintPrioIndex].ApplyHint(_cameraCurrent, _characterOrientation, ref camVel, _cinemachineCamController);
            var localIgnoreTilt = _cameraCurrent.localEulerAngles;
            localIgnoreTilt.y = 0;
            localIgnoreTilt.z = 0;
            _cameraTarget.localEulerAngles = localIgnoreTilt;
            var localIgnorePan = _cameraCurrent.localEulerAngles;
            localIgnorePan.x = 0;
            localIgnorePan.z = 0;
            _cameraPivot.localEulerAngles = localIgnorePan;
        }
        BlendToTarget();
    }

    void CameraMovementMobile()
    {
        if (uiTouching == Input.touchCount) // Prevents unneccesary looping
        {
            return;
        }

        // Loop through all touches
        foreach (Touch touch in Input.touches)
        {
            // Check if it's the beginning phase of touch
            if (touch.phase == TouchPhase.Began)
            {
                // Check if the touch is not touching a UI element
                if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    // Assign the valid touch ID
                    _validTouchID = touch.fingerId;
                }
            }

            // Check if it's the end phase of touch
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                // Check if the ended/canceled touch matches the valid touch ID
                if (touch.fingerId == _validTouchID)
                {
                    // Reset the valid touch ID
                    _validTouchID = -1;
                }
            }

            // Check if the current touch is the valid one
            if (touch.fingerId == _validTouchID)
            {
                // Perform camera movement based on touch delta
                Vector2 touchDeltaPosition = touch.deltaPosition * 0.02f;

                // Adjust rotation based on touch delta
                mouseX = touchDeltaPosition.x;
                mouseY = touchDeltaPosition.y;
                //GetRotationInput();
            }
        }
    }

    void ApplyInputRotation()
    {
        _cameraPivot.Rotate(Vector3.up, orbitSensitivity * mouseX);
        _cameraTarget.Rotate(Vector3.right, orbitSensitivity * mouseY);
        var angles = _cameraTarget.localEulerAngles;
        angles.z = 0;
        angles.y = 0;
        var angle = _cameraTarget.localEulerAngles.x;
        if (angle < 180 && angle > 60)
        {
            angles.x = 60;
        }
        else if (angle > 60 && angle < 290)
        {
            angles.x = 290;
        }
        _cameraTarget.localEulerAngles = angles;


        orbitRadius = Mathf.Clamp(orbitRadius + mouseScroll * orbitZoomSensitivity, orbitMinRadius, orbitMaxRadius);
    }

    void BlendToTarget()
    {
        if (_characterOrientation == null) { return; }
        Vector3 characterViewportPos = Camera.main.WorldToViewportPoint(_characterOrientation.position);

        float targetLocalY = _characterOrientation.InverseTransformPoint(_cameraCurrent.position).y;
        if (characterViewportPos.y > topBorder ||  characterViewportPos.y < bottomBorder) 
        {
            // Our character moved outside the bounds of the screen borders defined,
            // thus, we change the y position of the target local y
            targetLocalY = _characterOrientation.InverseTransformPoint(_cameraTarget.position).y;
        } 
        else if (_playerGravity.IsOnGround())
        {
            targetLocalY = _characterOrientation.InverseTransformPoint(_cameraTarget.position).y;
        }

        // Get the final position for camera current
        Vector3 localPoint = _characterOrientation.InverseTransformPoint(_cameraTarget.position);
        localPoint.y = targetLocalY;
        Vector3 desiredPos = _characterOrientation.TransformPoint(localPoint);

        _cameraCurrent.position = Vector3.SmoothDamp(_cameraCurrent.position, desiredPos, ref camVel, camReorientTime, camMaxSpeed);

        // Blend to the rotation of the target
        _cameraCurrent.rotation = Quaternion.Slerp(_cameraCurrent.rotation, _cameraTarget.rotation, 0.1f);

        if (_cinemachineCamController != null)
        {
            Cinemachine3rdPersonFollow ccb = _cinemachineCamController.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (ccb != null)
            {
                ccb.CameraDistance = Mathf.Lerp(ccb.CameraDistance, orbitRadius, 0.02f);
            }
        }
    }

    void DisableForCutscene(CutsceneObject s)
    {
        if (_cinemachineCamController != null)
        {
            _cinemachineCamController.gameObject.SetActive(false);
        }
    }

    void EnableAfterCutscene(CutsceneObject s)
    {
        if (_cinemachineCamController != null)
        {
            _cinemachineCamController.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null && other.gameObject.GetComponent<CameraHint>() != null) {
            activeHints.Add(other.gameObject.GetComponent<CameraHint>());
            highestHintPrioIndex = GetHighestPrioIndex();
            ignoreActiveHint = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject != null && other.gameObject.GetComponent<CameraHint>() != null)
        {
            other.gameObject.GetComponent<CameraHint>().ResetHintTime();
            activeHints.Remove(other.gameObject.GetComponent<CameraHint>());
            highestHintPrioIndex = GetHighestPrioIndex();
            ignoreActiveHint = false;
        }
    }

    int GetHighestPrioIndex()
    {
        int index = -1;
        int highest_prio = int.MinValue;
        for (int i = 0; i < activeHints.Count; i++)
        {
            if (activeHints[i].GetPriority() > highest_prio)
            {
                highest_prio = activeHints[i].GetPriority();
                index = i;
            }
        }
        return index;
    }

    void OnDrawGizmosSelected()
    {
    }
}
