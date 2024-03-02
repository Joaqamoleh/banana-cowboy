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
    Transform _cameraTarget = null, _cameraCurrent = null;
    
    GravityObject _playerGravity;
    Transform _characterOrientation;

    [SerializeField, Range(0f, 1f)]
    float bottomBorder = 0.25f, topBorder = 0.85f;
    [SerializeField, Range(0f, 30f)]
    float camReorientTime = 0.5f, camMaxSpeed = 5f;
    private Vector3 camVel = Vector3.zero;


    [SerializeField, Range(0.0f, 4.0f)]
    public float orbitSensitivity = 0.2f;

    //float _orbitRotationSpeed = 10.0f, _tiltRotationSpeed = 10.0f, _orbitZoomSpeed = 10.0f;

    //[SerializeField, Range(1.0f, 20.0f)]
    //float _reorientSpeed = 8.0f;


    //[SerializeField, Range(0f, 100f)]
    //float _orbitMinDist = 15f, _orbitMaxDist = 50f, _orbitDistance = 30f;


    public bool invertY = true, invertZoom = false;
    float mouseX, mouseY;

    private int _validTouchID = -1;
    public static int uiTouching;

    void Start()
    {
        // Removes cursor from screen and keeps it locked to center
        HideCursor();
        _playerGravity = GetComponent<GravityObject>();
        _characterOrientation = _playerGravity.characterOrientation;
    }

    void Update()
    {
        if (_cameraTarget == null) { return; }
        if (!PauseManager.pauseActive)
        {
#if !UNITY_IOS && !UNITY_ANDROID
            mouseX = Input.GetAxisRaw("Mouse X");
            mouseY = Input.GetAxisRaw("Mouse Y");
#else
            CameraMovementMobile();
#endif
        }
    }

    private void FixedUpdate()
    {
        ApplyInputRotation();
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



    //void GetRotationInput()
    //{
    //    float mouseScroll = -Input.GetAxisRaw("Mouse ScrollWheel");

    //    if (invertY)
    //    {
    //        mouseY = -mouseY;
    //    }
    //    if (invertZoom)
    //    {
    //        mouseScroll = -mouseScroll;
    //    }

    //    _cameraPivot.localRotation *= Quaternion.AngleAxis(mouseX * _orbitRotationSpeed, Vector3.up);
    //    _cameraTarget.localRotation *= Quaternion.AngleAxis(mouseY * _tiltRotationSpeed, Vector3.right);

    //    var angles = _cameraTarget.localEulerAngles;
    //    angles.z = 0;
    //    angles.y = 0;

    //    var angle = _cameraTarget.localEulerAngles.x;

    //    if (angle < 180 && angle > 60)
    //    {
    //        angles.x = 60;
    //    }
    //    else if (angle > 60 && angle < 290)
    //    {
    //        angles.x = 290;
    //    }

    //    _cameraTarget.localEulerAngles = angles;

    //    _orbitDistance = Mathf.Clamp(_orbitDistance + mouseScroll * _orbitZoomSpeed, _orbitMinDist, _orbitMaxDist);
    //    if (_cinemachineCamController != null)
    //    {
    //        Cinemachine3rdPersonFollow ccb = _cinemachineCamController.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    //        if (ccb != null)
    //        {
    //            ccb.CameraDistance = _orbitDistance;
    //        }
    //    }
    //}

    void ApplyInputRotation()
    {
        _cameraTarget.Rotate(Vector3.up, orbitSensitivity * mouseX);
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

        // Bend to look down at player?
        _cameraCurrent.rotation = Quaternion.Slerp(_cameraCurrent.rotation, _cameraTarget.rotation, 0.1f);
    }

    public static void HideCursor()
    {
#if !UNITY_IOS && !UNITY_ANDROID
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
#endif
    }

    public static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void DisableForCutscene()
    {
        if (_cinemachineCamController != null)
        {
            _cinemachineCamController.gameObject.SetActive(false);
        }
    }

    public void EnableAfterCutscene()
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
