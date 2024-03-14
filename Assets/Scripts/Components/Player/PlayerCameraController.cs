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
    Transform _cameraPivot = null, _cameraTilt = null, _emptyGameObjectPrefab;
    Transform _cameraCurrent = null, _cameraTarget = null;

    Cinemachine3rdPersonFollow _ccb;
    GravityObject _playerGravity;
    Transform _characterOrientation;

    [Header("Parameters")]
    [SerializeField, Range(0f, 1f)]
    float bottomBorder = 0.25f, topBorder = 0.85f;
    [SerializeField, Range(0f, 30f)]
    float camReorientTime = 0.5f, timeToRefocus = 5f, camMaxSpeed = 5f;
    float lastManualInputTime;
    private Vector3 camVel = Vector3.zero, _previousTargetPosition;

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
    public Joystick cameraJoystick;
    
    private float mouseX, mouseY, mouseScroll;
    private bool rotationHeld = false, ignoreActiveHint = false, changedHint = false;
    List<CameraHint> activeHints = new List<CameraHint>();
    int highestHintPrioIndex = -1;

    private int _validTouchID = -1;
    public static int uiTouching;

    bool frozenCam = false;

    void Start()
    {
        // Removes cursor from screen and keeps it locked to center
        _playerGravity = GetComponent<GravityObject>();
        _characterOrientation = _playerGravity.characterOrientation;
        _cameraCurrent = Instantiate(_emptyGameObjectPrefab);
        _cameraCurrent.position = _cameraTilt.position;

        _cameraTarget = Instantiate(_emptyGameObjectPrefab);
        _cameraTarget.position = _cameraTilt.position;
        _previousTargetPosition = _cameraTarget.position;

        // Overrides the camera's default radius
        if (_cinemachineCamController != null)
        {
            _cinemachineCamController.Follow = _cameraCurrent;
            _ccb = _cinemachineCamController.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (_ccb != null)
            {
                _ccb.CameraDistance = orbitRadius;
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
        if (_cameraTilt == null) { return; }
        if (!PauseManager.pauseActive)
        {
#if !UNITY_IOS && !UNITY_ANDROID
            mouseX = Input.GetAxisRaw("Mouse X");
            mouseY = Input.GetAxisRaw("Mouse Y");
            mouseScroll = -Input.GetAxisRaw("Mouse ScrollWheel");

            if (Input.GetKeyDown(rotationKey))
            {
                PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.CAMERA_PAN);
                ignoreActiveHint = true;
                rotationHeld = true;
            } 
            else if (Input.GetKeyUp(rotationKey))
            {
                PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
                rotationHeld = false;
            }
#else
            mouseX = cameraJoystick.Horizontal;
            mouseY = cameraJoystick.Vertical;
            rotationHeld = true;
#endif
            if (invertY)
            {
                mouseY = -mouseY;
            }
            
            if (invertZoom)
            {
                mouseScroll = -mouseScroll;
            }
            //CameraMovementMobile();
        }
    }

    Vector3 angles;
    Vector3 tiltangles;
    private void FixedUpdate()
    {
        if (frozenCam) { return; }
        if (rotationHeld)
        {
            ApplyInputRotation();
        } 
        else
        {
            ApplyMovementRotation();
        }
        //else
        //{
        //    if (highestHintPrioIndex != -1)
        //    {
        //        // Apply camera hints
        //        if (ignoreActiveHint)
        //        {
        //            if (Vector3.Dot(_characterOrientation.up, _cameraCurrent.forward) > 0f && changedHint)
        //            {
        //                ignoreActiveHint = false;
        //                changedHint = false;
        //            }
        //        } 
        //        else
        //        {
        //            activeHints[highestHintPrioIndex].ApplyHint(_cameraTarget, _characterOrientation, ref camVel, _cinemachineCamController);
        //            changedHint = false;
        //        }

        //    }
        //    _cameraPivot.rotation = _cameraTarget.rotation;
        //    angles = _cameraPivot.localEulerAngles;
        //    angles.x = 0;
        //    angles.z = 0;
        //    _cameraPivot.localEulerAngles = angles;
        //    _cameraTilt.rotation = _cameraTarget.rotation;
        //    tiltangles = _cameraTilt.localEulerAngles;
        //    tiltangles.y = 0;
        //    _cameraTilt.localEulerAngles = tiltangles;
        //}
        UpdateTargetToPlayer();
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
        _cameraTilt.Rotate(Vector3.right, orbitSensitivity * mouseY);
        angles = _cameraTilt.localEulerAngles;
        angles.z = 0;
        angles.y = 0;
        var angle = _cameraTilt.localEulerAngles.x;
        if (angle < 180 && angle > 60)
        {
            angles.x = 60;
        }
        else if (angle > 60 && angle < 290)
        {
            angles.x = 290;
        }
        _cameraTilt.localEulerAngles = angles;
        _cameraTarget.rotation = _cameraTilt.rotation;
        _cameraCurrent.rotation = _cameraTilt.rotation;

        orbitRadius = Mathf.Clamp(orbitRadius + mouseScroll * orbitZoomSensitivity, orbitMinRadius, orbitMaxRadius);
    }

    void ApplyMovementRotation()
    {
        if ((_previousTargetPosition - _cameraTarget.position).magnitude < 0.2f) { return; }
        Vector3 movement = Vector3.ProjectOnPlane(_cameraPivot.InverseTransformDirection(_cameraTarget.position - _previousTargetPosition), Vector3.up).normalized;
        float targetAngle = Vector3.Angle(movement, Vector3.forward);
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(targetAngle, _cameraPivot.localEulerAngles.y));
        Debug.DrawRay(_cameraTarget.position, (_cameraTarget.position - _previousTargetPosition).normalized * 10f, Color.blue);
        if (deltaAbs < 45f)
        {
            var angles = _cameraPivot.localEulerAngles;
            angles.y = targetAngle;
            _cameraPivot.localEulerAngles = angles;
        }
        else if (180f - deltaAbs < 45f)
        {
            var angles = _cameraPivot.localEulerAngles;
            angles.y = 180f - targetAngle;
            _cameraPivot.localEulerAngles = angles;
        }
        _cameraTarget.rotation = _cameraTilt.rotation;
        _previousTargetPosition = _cameraTarget.position;
    }

    Vector3 localPoint;
    void UpdateTargetToPlayer()
    {
        if (_characterOrientation == null) { return; }
        Vector3 characterViewportPos = Camera.main.WorldToViewportPoint(_characterOrientation.position);

        float targetLocalY = _characterOrientation.InverseTransformPoint(_cameraCurrent.position).y;
        if (characterViewportPos.y > topBorder || characterViewportPos.y < bottomBorder)
        {
            // Our character moved outside the bounds of the screen borders defined,
            // thus, we change the y position of the target local y
            targetLocalY = _characterOrientation.InverseTransformPoint(_cameraTilt.position).y;
        }
        else if (_playerGravity.IsOnGround())
        {
            targetLocalY = _characterOrientation.InverseTransformPoint(_cameraTilt.position).y;
        }

        // Get the final position for camera current
        localPoint = _characterOrientation.InverseTransformPoint(_cameraTilt.position);
        localPoint.y = targetLocalY;
        _cameraTarget.position = _characterOrientation.TransformPoint(localPoint);
    }

    void BlendToTarget()
    {
        Vector3 pos = Vector3.SmoothDamp(_cameraCurrent.position, _cameraTarget.position, ref camVel, camReorientTime, camMaxSpeed);
        pos = _cameraTarget.position;
        Quaternion rot = Quaternion.Slerp(_cameraCurrent.rotation, _cameraTarget.rotation, 0.01f);
        _cameraCurrent.SetPositionAndRotation(pos, rot);

        if (_ccb != null)
        {
            _ccb.CameraDistance = Mathf.Lerp(_ccb.CameraDistance, orbitRadius, 0.02f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null && other.gameObject.GetComponent<CameraHint>() != null) {
            activeHints.Add(other.gameObject.GetComponent<CameraHint>());
            int prev = highestHintPrioIndex;
            highestHintPrioIndex = GetHighestPrioIndex();
            changedHint = prev != highestHintPrioIndex;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject != null && other.gameObject.GetComponent<CameraHint>() != null)
        {
            other.gameObject.GetComponent<CameraHint>().ResetHintTime();
            activeHints.Remove(other.gameObject.GetComponent<CameraHint>());
            int prev = highestHintPrioIndex;
            highestHintPrioIndex = GetHighestPrioIndex();
            changedHint = prev != highestHintPrioIndex;

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

    public void FreezeCamera()
    {
        frozenCam = true;
    }

    public void UnfreezeCamera()
    {
        frozenCam = false;
    }

    public void JumpTo(Vector3 position, Quaternion orientation)
    {
        Camera.main.GetComponent<CinemachineBrain>().enabled = false;
        Camera.main.transform.position = position;
        Camera.main.transform.rotation = orientation;
        Camera.main.GetComponent<CinemachineBrain>().enabled = true;
        _cameraTarget.position = position;
        _cameraTarget.rotation = orientation;
        _cameraCurrent.position = position;
        _cameraCurrent.rotation = orientation;
    }

    public void JumpToPlayer()
    {
        JumpTo(_cameraTilt.position, _cameraTilt.rotation);
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

    void OnDrawGizmosSelected()
    {
    }
}
