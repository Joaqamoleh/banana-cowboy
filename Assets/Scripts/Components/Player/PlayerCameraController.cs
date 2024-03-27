using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    CinemachineVirtualCamera _cinemachineCamController;
    [SerializeField]
    Transform _cameraFocus = null, _emptyGameObjectPrefab;
    Transform _cameraCurrent = null, _focusBasis;
    GravityObject _focusGravity;
    Vector3 _focusPoint, _previousFocusPoint, cameraHalfExtends;
    Vector2 _orbitAngles = new Vector2(45f, 0f);
    Quaternion basisRot, targetBasisRot;
    float _focusHeightOffset, targetHeightOffset;

    [Header("Parameters")]
    [SerializeField, Range(0f, 1f)]
    float focusCenteringFactor = 0.5f;
    [SerializeField, Range(0f, 1f)]
    float bottomBorder = 0.25f, topBorder = 0.85f;
    [SerializeField, Range(0f, 30f)]
    float realignTime = 5f, realignRadius = 1f, defaultHeightOffset = 3f;
    [SerializeField, Range(1f, 360f)]
    float realignRotationSpeed = 90f;
    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;
    [SerializeField]
    LayerMask obstructionMask = -1;

    float lastManualInputTime;

    public const float orbitSensitivityMin = 1f, orbitSensitivityMax = 360f; // For the slider in menu
    [SerializeField, Range(orbitZoomSensitivityMin, orbitZoomSensitivityMax)]
    public float orbitSensitivity = 90f;

    public const float orbitZoomSensitivityMin = 5f, orbitZoomSensitivityMax = 50f; // For the slider in menu
    [SerializeField, Range(orbitZoomSensitivityMin, orbitZoomSensitivityMax)]
    public float orbitZoomSensitivity = 10f;

    [SerializeField, Min(0f)]
    float orbitMinRadius = 10f, orbitMaxRadius = 60f, orbitRadius = 30f;
    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    public bool invertY = true, invertZoom = false;


    [Header("Input")]
    [SerializeField]
    KeyCode rotationKey = KeyCode.Mouse1;
    public Joystick cameraJoystick;


    private float mouseX, mouseY, mouseScroll;
    private bool rotationHeld = false;
    List<CameraHint> activeHints = new List<CameraHint>();
    int highestHintPrioIndex = -1;

    private int _validTouchID = -1;
    public static int uiTouching;

    bool frozenCam = false;

    private float mobileConstant = 1f;


    private void OnValidate()
    {
        if (orbitMinRadius > orbitMaxRadius)
        {
            orbitMinRadius = orbitMaxRadius;
        }
        if (orbitRadius < orbitMinRadius)
        {
            orbitRadius = orbitMinRadius;
        }
        if (minVerticalAngle > maxVerticalAngle)
        {
            minVerticalAngle = maxVerticalAngle;
        }
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    void Start()
    {
#if UNITY_IOS || UNITY_ANDROID
        mobileConstant = 0.5f;
#endif
        _cameraCurrent = _cinemachineCamController.transform;
        if (_cameraFocus != null)
        {
            _focusPoint = _cameraFocus.position;
            _focusGravity = _cameraFocus.GetComponent<GravityObject>();
            if (_focusGravity != null)
            {
                _focusBasis = _focusGravity.characterOrientation;
                basisRot = _focusBasis.rotation;
            }
            else
            {
                basisRot = transform.rotation;
            }
            targetBasisRot = basisRot;
        }

        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += DisableForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += EnableAfterCutscene;
        }


        _focusHeightOffset = defaultHeightOffset;
        cameraHalfExtends = GetCameraHalfExtends();
        lastManualInputTime -= realignTime;
    }

    Vector3 _moveCameraInput = Vector3.zero;
    void Update()
    {
        if (_cameraFocus == null) { return; }
        if (!PauseManager.pauseActive)
        {

#if !UNITY_IOS && !UNITY_ANDROID
            mouseX = Input.GetAxisRaw("Mouse X");
            mouseY = Input.GetAxisRaw("Mouse Y");
            mouseScroll = -Input.GetAxisRaw("Mouse ScrollWheel");

            if (Input.GetKeyDown(rotationKey))
            {
                PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.CAMERA_PAN);
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
            _moveCameraInput = new Vector3(mouseX, 0, mouseY);

            if (_moveCameraInput != Vector3.zero)
            {
                rotationHeld = true;
            }
            else
            {
                rotationHeld = false;
            }
#endif
            if (invertY)
            {
                mouseY = -mouseY;
            }

            if (invertZoom)
            {
                mouseScroll = -mouseScroll;
            }
        }
    }

    private void LateUpdate()
    {
        if (frozenCam || _cameraFocus == null || PauseManager.pauseActive || !_cinemachineCamController.gameObject.activeSelf) { return; }
        UpdateFocusPoint();
        UpdateBasisRot();
        Quaternion lookRot = basisRot * Quaternion.Euler(_orbitAngles);
        if (PerformManualRotation() || PerformHintRotation() || PerformAutomaticRotation())
        {
            ConstrainAngles();
        }
        Vector3 lookDir = lookRot * Vector3.forward;
        Vector3 position = _focusPoint - lookDir * orbitRadius + basisRot * Vector3.up * _focusHeightOffset;

        Vector3 rectOffset = lookDir * Camera.main.nearClipPlane;
        Vector3 rectPosition = position + rectOffset;
        Vector3 castFrom = _cameraFocus.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(castFrom, cameraHalfExtends, castDirection, out RaycastHit hit, lookRot, castDistance, obstructionMask))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            position = rectPosition - rectOffset;
        }
        _cameraCurrent.SetPositionAndRotation(position, lookRot);
    }

    void UpdateFocusPoint()
    {
        _previousFocusPoint = _focusPoint;
        Vector3 targetPoint = _cameraFocus.position;
        Vector3 targetViewportPos = Camera.main.WorldToViewportPoint(targetPoint);
        float distance = Vector3.Distance(targetPoint, _focusPoint);
        float t = 1f;
        if (distance > 0.01f && focusCenteringFactor > 0f)
        {
            t = Mathf.Pow(1f - focusCenteringFactor, Time.unscaledDeltaTime);
        }
        if (distance > realignRadius)
        {
            t = Mathf.Min(t, realignRadius / distance);
        }
        _focusPoint = Vector3.Lerp(targetPoint, _focusPoint, t);
        _focusHeightOffset = Mathf.Lerp(targetHeightOffset, _focusHeightOffset, t);
    }

    void UpdateBasisRot()
    {
        float t = 1f;
        float roughDistance = Vector3.Distance(targetBasisRot.eulerAngles, basisRot.eulerAngles);
        if (roughDistance > 0.01f && focusCenteringFactor > 0f)
        {
            t = Mathf.Pow(1f - focusCenteringFactor, Time.unscaledDeltaTime);
        }
        if (roughDistance > 180f)
        {
            t = Mathf.Min(t, 180 / roughDistance);
        }
        basisRot = Quaternion.Slerp(basisRot, targetBasisRot, Time.unscaledDeltaTime);
    }

    bool PerformManualRotation()
    {
        if (rotationHeld)
        {
            const float e = 0.001f;
            if (mouseX > e || mouseY > e || mouseX < -e || mouseY < -e || mouseScroll > e || mouseScroll < e)
            {
                orbitRadius = Mathf.Clamp(orbitRadius + mouseScroll * orbitZoomSensitivity, orbitMinRadius, orbitMaxRadius);
                _orbitAngles = new Vector2(_orbitAngles.x + mouseY * orbitSensitivity * mobileConstant * Time.unscaledDeltaTime, _orbitAngles.y + mouseX * orbitSensitivity * mobileConstant * Time.unscaledDeltaTime);
                lastManualInputTime = Time.unscaledTime;
                return true;
            }
        }
        return false;
    }

    bool PerformAutomaticRotation()
    {
        if (_focusGravity == null) { return false; }


        if (Time.unscaledTime - lastManualInputTime < realignTime)
        {
            targetBasisRot = _focusBasis.rotation;
            return false;
        }

        Vector3 movement = _focusBasis.InverseTransformDirection(Vector3.ProjectOnPlane((_focusPoint - _previousFocusPoint), _focusBasis.up));
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.001f)
        {
            return false;
        }
        movement = movement / Mathf.Sqrt(movementDeltaSqr);
        float angle = Mathf.Acos(movement.z) * Mathf.Rad2Deg;
        angle = movement.x < 0 ? 360f - angle : angle;
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(_orbitAngles.y, angle));
        float angleChange = realignRotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if (deltaAbs < alignSmoothRange)
        {
            angleChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            angleChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        targetBasisRot = _focusBasis.rotation;
        targetHeightOffset = defaultHeightOffset;
        _orbitAngles.y = Mathf.MoveTowardsAngle(_orbitAngles.y, angle, angleChange);
        return true;
    }

    bool PerformHintRotation()
    {
        if (Time.unscaledTime - lastManualInputTime < realignTime)
        {
            return false;
        }

        if (highestHintPrioIndex == -1)
        {
            return false;
        }

        CameraHint hint = activeHints[highestHintPrioIndex];
        Transform basis = hint.GetOrientationBasis();
        float orbitDist = hint.GetOrbitDistance();
        if (basis == null)
        {
            return false;
        }
        float horizontalAngle = hint.GetOrbitRotationAngle(_cameraFocus.position);
        _orbitAngles.y = Mathf.MoveTowardsAngle(_orbitAngles.y, horizontalAngle, GetSmoothedDeltaAngle(_orbitAngles.y, horizontalAngle));

        float verticalAngle = hint.GetOrbitVertAngle();
        _orbitAngles.x = Mathf.MoveTowardsAngle(_orbitAngles.x, hint.GetOrbitVertAngle(), GetSmoothedDeltaAngle(_orbitAngles.x, verticalAngle));

        orbitRadius = Mathf.Lerp(orbitRadius, orbitDist, Time.deltaTime);
        targetBasisRot = basis.rotation;
        targetHeightOffset = hint.GetHeightOffset();
        return true;
    }

    float GetSmoothedDeltaAngle(float starting, float newAngle)
    {
        float abs = Mathf.Abs(Mathf.DeltaAngle(starting, newAngle));
        float change = realignRotationSpeed * Time.unscaledDeltaTime;
        if (abs < alignSmoothRange)
        {
            change *= abs / alignSmoothRange;
        }
        else if (180f - abs < alignSmoothRange)
        {
            change *= (180f - abs) / alignSmoothRange;
        }
        return change;
    }

    void ConstrainAngles()
    {
        _orbitAngles.x = Mathf.Clamp(_orbitAngles.x, minVerticalAngle, maxVerticalAngle);
        if (_orbitAngles.y < 0f)
        {
            _orbitAngles.y += 360f;
        }
        else if (_orbitAngles.y >= 360f)
        {
            _orbitAngles.y -= 360f;
        }
    }

    Vector3 GetCameraHalfExtends()
    {
        Vector3 halfExtends;
        halfExtends.y =
            Camera.main.nearClipPlane *
            Mathf.Tan(0.5f * Mathf.Deg2Rad * Camera.main.fieldOfView);
        halfExtends.x = halfExtends.y * Camera.main.aspect;
        halfExtends.z = 0f;
        return halfExtends;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null && other.gameObject.GetComponent<CameraHint>() != null)
        {
            activeHints.Add(other.gameObject.GetComponent<CameraHint>());
            highestHintPrioIndex = GetHighestPrioIndex();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject != null && other.gameObject.GetComponent<CameraHint>() != null)
        {
            activeHints.Remove(other.gameObject.GetComponent<CameraHint>());
            highestHintPrioIndex = GetHighestPrioIndex();
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
        _cameraCurrent.position = position;
        _cameraCurrent.rotation = orientation;
    }

    public void JumpToPlayer()
    {
        JumpTo(_focusPoint - _cameraCurrent.forward * orbitRadius, Quaternion.identity); // TODO: Fix
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
