using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(GravityObject))]
public class PlayerController : MonoBehaviour
{
    public enum State
    {
        IDLE,   // Accept move input
        AIR,    // Accept move input, air control ratio applied
        WALK,   // Accept move input
        RUN,    // Accept move input
        SWING,  // Accept move input, jump or click to cancel
    }
    State _state = State.IDLE;
    bool disabledForCutscene = false;

    public delegate void PlayerStateChange(State newState);
    public event PlayerStateChange OnStateChanged;

    public delegate void PlayerLanded(Rigidbody hitGround);
    public event PlayerLanded OnLanded;
    public delegate void PlayerLeftGround();
    public event PlayerLeftGround OnLeftGround;
    public event PlayerLeftGround OnJumpPressed;


    public enum LassoState
    {
        NONE,       // Hangs by the player's hip
        THROWN,     // Lasso is being thrown at a target. Can be canceled?
        SWING,      // Lasso is locked onto a swing object.
        PULL,       // Pulling object towards player
        HOLD,       // Holding object
        TOSS,       // Tossing object out
        RETRACT     // Retracting back to the hip. Acts as the recharge time to use the lasso again
    }
    LassoState _lassoState = LassoState.NONE;
    LassoState _transitionState = LassoState.NONE; // The state to change to

    public delegate void LassoStateChange(LassoState newState);
    public event LassoStateChange OnLassoStateChange;

    public delegate void LassoTossedDelegate(LassoTossable.TossStrength s);
    public event LassoTossedDelegate OnLassoTossed;

    [Header("References")]
    [SerializeField]
    Transform model;
    [SerializeField]
    Transform lassoSwingCenter;
    [SerializeField]
    Rigidbody controlPlayerConnection;

    private Transform _camera;
    private GravityObject _gravObject;
    private Rigidbody _rigidbody;
    private Health _health;
    private PlayerCursor _cursor;
    private UIManager _playerUI;
    private LassoRenderer _lassoRenderer;

    [Header("Input")]
    [SerializeField]
    KeyCode lassoKey = KeyCode.Mouse0;
    [SerializeField]
    KeyCode runKey = KeyCode.LeftShift, jumpKey = KeyCode.Space;

    [Header("Mobile Input")]
    public GameObject mobileUI;
    public Joystick joystick;
    PauseManager pauseMenu;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    float walkSpeed = 10f;
    [SerializeField, Min(0f)]
    float runSpeed = 19f, accelSpeed = (50f * 1f) / 19f, deccelSpeed = (50f * 1f) / 19f, jumpImpulseForce = 30f, coyoteTime = 0.2f, jumpBufferTime = 0.3f;
    [SerializeField, Range(0f, 1f)]
    float airControlRatio = 0.8f;

    private Vector3 _moveInput = Vector3.zero;
    private bool _jumpHeld = false, _jumping = false, _running = false, _detectLanding = false, _jumpBuffered = false;
    private float _lastTimeOnGround = 0f, _currentJumpBufferTime = 0f;

    [Header("Lasso")]
    [SerializeField, Min(0f)]
    float lassoThrowSpeed = 12.5f, timeToPullTarget = 0.4f, timeToToss = 0.1f, timeToRetract = 1.0f, lassoTossMinigameSpeed = 3f;

    private LassoObject _hoveredObject = null, _lassoObject = null;
    private float lassoCurTime, lassoMaxTime;

    private static bool hasPunchAbility = false; // TODO: For testing, turn to false when building for game

    Transform _attachedJoint = null;

    private void Awake()
    {
        StartCoroutine(SetSpawnPos());
    }
    private void Start()
    {
        if (_camera == null)
        {
            _camera = Camera.main.transform;
        }
        _gravObject = GetComponent<GravityObject>();
        Debug.Assert(_gravObject != null);
        _rigidbody = GetComponent<Rigidbody>();
        Debug.Assert(_rigidbody != null);
        _health = GetComponent<Health>();
        Debug.Assert(_health != null);
        _cursor = GetComponentInChildren<PlayerCursor>();
        Debug.Assert(_cursor != null);
        _playerUI = GetComponentInChildren<UIManager>();
        Debug.Assert(_playerUI != null);
        _lassoRenderer = GetComponentInChildren<LassoRenderer>();
        Debug.Assert(_lassoRenderer != null);
        Debug.Assert(controlPlayerConnection != null);
        controlPlayerConnection.gameObject.SetActive(false);

        BananaPunch punchAbility = GetComponent<BananaPunch>();
        Debug.Assert(punchAbility != null);
        punchAbility.canPunch = hasPunchAbility;

        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += DisableCharacterForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += EnableCharacterAfterCutscene;
        }
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);

#if UNITY_IOS || UNITY_ANDROID
        mobileUI.SetActive(true);
        pauseMenu = GameObject.Find("Pause Manager").GetComponent<PauseManager>();
#endif
    }
    private void Update()
    {
        if (!PauseManager.pauseActive && _health.health > 0)
        {
            if (!disabledForCutscene)
            {
                GetMoveInput();
                GetLassoInput();
            }
            else
            {
                if (_attachedJoint != null)
                {
                    model.rotation = _attachedJoint.transform.rotation;
                } 
                else
                {
                    // Stops the buggy rotation model. IDK why it's happening, so this is a patchwork fix
                    model.rotation = Quaternion.FromToRotation(model.up, _gravObject.characterOrientation.up) * model.rotation;
                }
            }
            DetectStateChange();
        }
    }
    private void FixedUpdate()
    {
        if (_state == State.SWING)
        {
            Swing();
        }
        else
        {
            Run();
        }
        if (!disabledForCutscene && !PauseManager.pauseActive)
        {
            Jump();
            Lasso();
        }
    }
    private void LateUpdate()
    {
    }

    // ****************************** Helpers ************************* //
    IEnumerator SetSpawnPos()
    {
        yield return new WaitForEndOfFrame();
        if (GameObject.Find("CheckpointManager") != null)
        {
            transform.position = LevelData.GetRespawnPos();
        }
        else
        {
            transform.position = Vector3.zero;
        }
    }

    public void AttachToRigidbody(Rigidbody rb)
    {
        print("attached joint");
        FixedJoint joint = gameObject.AddComponent<FixedJoint>();
        if (joint != null)
        {
            if (_attachedJoint != null)
            {
                Destroy(_attachedJoint);
            }
            _attachedJoint = rb.transform;
            _rigidbody.isKinematic = true;
            transform.position = rb.position;
            model.rotation = rb.rotation;
            _rigidbody.isKinematic = false;
            joint.connectedBody = rb;
        }
    }

    public void DetachFromRigidbody(Rigidbody rb)
    {
        print("Destroy Attached joint");
        FixedJoint[] joints = GetComponents<FixedJoint>();
        foreach (FixedJoint joint in joints) { 
            if (joint.connectedBody == rb) {
                Destroy(joint);
            }
        }
        _attachedJoint = null;
    }

    // ************************* State Updates ************************ //
    void UpdateState(State newState)
    {
        if (_state != newState)
        {
            print("State updated to " + newState);
            OnStateChanged?.Invoke(newState);
            if (newState == State.AIR)
            {
                _detectLanding = true;
                OnLeftGround?.Invoke();
            }
            _state = newState;
        }
    }

    void UpdateLassoState(LassoState newState)
    {
        if (_lassoState != newState)
        {
            _lassoState = newState;

            switch (_lassoState)
            {
                case LassoState.SWING:
                    SwingableObject swingable = _lassoObject as SwingableObject;
                    if (swingable == null)
                    {
                        // Invalid state, just retract
                        _lassoState = LassoState.RETRACT;
                    }
                    else
                    {
                        controlPlayerConnection.gameObject.SetActive(true);
                        swingable.SwingEnd += EndLassoSwing;
                        swingable.AttachToSwingable(controlPlayerConnection, _gravObject.characterOrientation.up);
                        UpdateState(State.SWING);
                    }
                    break;
                case LassoState.TOSS:
                    LassoTossable toss = _lassoObject as LassoTossable;
                    if (toss == null)
                    {
                        // Invalid state, just retract
                        _lassoState = LassoState.RETRACT;
                    }
                    else if (!toss.IsTossed())
                    {
                        LassoTossable.TossStrength strength = _playerUI.GetTossIndicatorStrength();
                        Vector3 dirOfToss = GetLassoTossDir();
                        toss.TossInDirection(dirOfToss, _gravObject.characterOrientation.up, strength);
                        dirOfToss = _gravObject.characterOrientation.InverseTransformDirection(dirOfToss);
                        Quaternion targetRot = Quaternion.Euler(0f, dirOfToss.x < 0 ? 360f - Mathf.Acos(dirOfToss.z) * Mathf.Rad2Deg : Mathf.Acos(dirOfToss.z) * Mathf.Rad2Deg, 0f);
                        model.localRotation = targetRot;

                        OnLassoTossed?.Invoke(strength);
                    }
                    break;
                case LassoState.THROWN:
                    if (_lassoObject == null)
                    {
                        // Invalid state, just retract
                        _lassoState = LassoState.RETRACT;
                    }
                    else
                    {
                        Vector3 dirOfThrown = Vector3.ProjectOnPlane((_lassoObject.GetLassoCenterPos() - transform.position).normalized, _gravObject.characterOrientation.up).normalized;
                        dirOfThrown = _gravObject.characterOrientation.InverseTransformDirection(dirOfThrown);
                        Quaternion targetRot = Quaternion.Euler(0f, dirOfThrown.x < 0 ? 360f - Mathf.Acos(dirOfThrown.z) * Mathf.Rad2Deg : Mathf.Acos(dirOfThrown.z) * Mathf.Rad2Deg, 0f); //_gravObject.characterOrientation.rotation*
                        model.localRotation = targetRot;
                    }
                    break;
                default:
                    break;
            }

            lassoCurTime = 0.0f;
            switch (_lassoState)
            {
                case LassoState.RETRACT:
                    lassoMaxTime = timeToRetract; break;
                case LassoState.TOSS:
                    lassoMaxTime = timeToToss; break;
                case LassoState.PULL:
                    lassoMaxTime = timeToPullTarget; break;
                case LassoState.THROWN:
                    lassoMaxTime = Vector3.Distance(transform.position, _lassoObject.transform.position) / lassoThrowSpeed;
                    break;
            }

            if (_lassoState == LassoState.HOLD || _lassoState == LassoState.TOSS)
            {
                _playerUI.ShowThrowBar();
            }
            else
            {
                _playerUI.HideThrowBar();
            }

            OnLassoStateChange?.Invoke(_lassoState);

            LassoRenderer.LassoInfo info = new LassoRenderer.LassoInfo
            {
                o = _lassoObject,
                state = _lassoState,
                timeForAction = lassoMaxTime,
                lassoSwingCenter = lassoSwingCenter,
            };

            _lassoRenderer.SetLassoRenderState(info);
        }
    }

    void DetectStateChange()
    {
        _lastTimeOnGround -= Time.deltaTime;

        if (_state == State.SWING) { return; }

        if (_gravObject.IsOnGround())
        {
            if (_detectLanding)
            {
                OnLand();
            }
            _jumping = false;
            _lastTimeOnGround = coyoteTime;
        }
        if (_lastTimeOnGround < 0)
        {
            UpdateState(State.AIR);
        }
        else
        {
            if (_moveInput.magnitude > 0)
            {
                if (_running)
                {
                    UpdateState(State.RUN);
                }
                else
                {
                    UpdateState(State.WALK);
                }
            }
            else
            {
                UpdateState(State.IDLE);
            }
        }
    }

    void OnLand()
    {
        _detectLanding = false;
        OnLanded?.Invoke(_gravObject.GetGround());
        _gravObject.gravityMult = 1.0f;

        // Jump buffering, have a bool flag set to true to perform it on the next Jump() call
        if (_currentJumpBufferTime > 0f)
        {
            _jumpBuffered = true;
        }
    }

    // ************************* INPUT ************************ //
    void GetMoveInput()
    {
#if UNITY_IOS || UNITY_ANDROID
        // TODO: Later
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        _moveInput = new Vector3(horizontal, 0, vertical);

        // Apply damping when releasing the joystick
        if (_moveInput == Vector3.zero)
        {
            _moveInput = Vector3.Lerp(_moveInput, Vector3.zero, 0.1f * Time.deltaTime);
        }
#else
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        _moveInput = new Vector3(horizontal, 0, vertical);
        if (Input.GetKeyDown(runKey))
        {
            _running = true;
        }
        else if (Input.GetKeyUp(runKey))
        {
            _running = false;
        }

        // Decrement buffer time
        _currentJumpBufferTime -= Time.deltaTime;
        if (Input.GetKeyDown(jumpKey))
        {
            _jumpHeld = true;
            // Jump buffer time if we press Jump while in the air
            if (!_gravObject.IsOnGround())
            {
                _currentJumpBufferTime = jumpBufferTime;
            }
        }
        else if (Input.GetKeyUp(jumpKey))
        {
            _jumpHeld = false;
        }

#endif
    }

    // mobile buttons
    public void JumpMobileButtonPressed()
    {
        _jumpHeld = true;

        // Jump buffer time if we press Jump while in the air
        if (!_gravObject.IsOnGround())
        {
            _currentJumpBufferTime = jumpBufferTime;
        }
    }
    
    public void JumpMobileButtonRelease()
    {
        _jumpHeld = false;
    }

    public void RunMobileButtonPressed()
    {
        _running = !_running;
        if (_running)
        {
            GameObject.Find("Run").GetComponent<Image>().color = Color.red;
        }
        else
        {
            GameObject.Find("Run").GetComponent<Image>().color = Color.white;
        }
    }

    public void SettingsMobileButtonPressed()
    {
        pauseMenu.PauseGame();
    }

    void GetLassoInput()
    {

        if (_moveInput.magnitude > 0)
        {
            // Cancel lasso toss state.
            if (_lassoState == LassoState.TOSS)
            {
                UpdateLassoState(LassoState.RETRACT);
            }
        }

        if (Input.GetKeyDown(lassoKey))
        {
            switch (_lassoState)
            {
                case LassoState.RETRACT:
                case LassoState.NONE:
                    // Do lasso action based on what is selected, update to thrown
                    _hoveredObject = _cursor.GetHoveredLassoObject();
                    if (_hoveredObject != null && _hoveredObject.isLassoable && !_hoveredObject.currentlyLassoed && _hoveredObject.isInRange)
                    {
                        _lassoObject = _hoveredObject;
                        UpdateLassoState(LassoState.THROWN);
                    }
                    break;
                case LassoState.PULL:
                    // Do nothing (maybe toss the enemy immediately?)
                case LassoState.THROWN: 
                    // DO NOT DO ANYTHING, I think it will mess things up bad to accept input here
                case LassoState.TOSS:
                    // Do nothing
                    break;
                case LassoState.SWING:
                    // Buffer next swing if clicked on a swingable object.
                    break;

                case LassoState.HOLD:
                    // Show the throw indicator and throw when the player clicks
#if !(UNITY_IOS || UNITY_ANDROID)
                    UpdateLassoState(LassoState.TOSS);
#else
                    if (Input.touchCount > 0)
                    {
                        foreach (var touch in Input.touches)
                        {
                            if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                            {
                                UpdateLassoState(LassoState.TOSS);
                                break;
                            }
                        }
                    }
#endif
                    break;

            }
        }

    }

    // ********************** INPUT PROCESSING ************************* //
    void Run()
    {

        // Transform the move input relative to the camera
        _moveInput = _camera.TransformDirection(_moveInput);
        // Transform the move input relative to the player
        _moveInput =
            Vector3.Dot(_gravObject.characterOrientation.right, _moveInput) * _gravObject.characterOrientation.right
            + Vector3.Dot(_gravObject.characterOrientation.forward, _moveInput) * _gravObject.characterOrientation.forward;
        Vector3 targetVelocity = _moveInput.normalized * (_running ? runSpeed : walkSpeed);

        float accelRate;
        // Gets an acceleration value based on if we are accelerating (includes turning) 
        // or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (_lastTimeOnGround > 0)
            accelRate = (Vector3.Dot(targetVelocity, _gravObject.GetMoveVelocity()) > 0) ? accelSpeed : deccelSpeed;
        else
            accelRate = (Vector3.Dot(targetVelocity, _gravObject.GetMoveVelocity()) > 0) ? accelSpeed * airControlRatio : deccelSpeed * airControlRatio;


        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (_gravObject.GetMoveVelocity().magnitude > targetVelocity.magnitude &&
            Vector3.Dot(targetVelocity, _gravObject.GetMoveVelocity()) > 0 &&
            targetVelocity.magnitude > 0.01f && _lastTimeOnGround < 0)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }

        Vector3 speedDiff = targetVelocity - _gravObject.GetMoveVelocity();
        Vector3 movement = speedDiff * accelRate;
        _rigidbody.AddForce(movement);

        // Spin player model and orientation to right direction to face
        if (_moveInput.magnitude > 0 && model != null)
        {
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(targetVelocity.normalized, _gravObject.characterOrientation.up), Time.deltaTime * 8f);
        }
        else if (!_gravObject.IsOnGround())
        {
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.FromToRotation(model.up, _gravObject.characterOrientation.up) * model.rotation, Time.deltaTime * 8f);
        }
    }

    void Jump()
    {
        if (_state == State.SWING)
        {
            if (_jumpHeld)
            {
                EndLassoSwing((_lassoObject as SwingableObject).EndSwing());
                _jumpHeld = false;
                _jumping = false;
                _jumpBuffered = false;
            }
            return;
        }

        if ((_jumpHeld && _lastTimeOnGround > 0.0f && !_jumping) || _jumpBuffered)
        {
            if (_gravObject.GetFallingVelocity().magnitude > 0)
            {
                _gravObject.SetFallingVelocity(0);
            }
            _rigidbody.AddForce(_gravObject.characterOrientation.up * jumpImpulseForce, ForceMode.Impulse);
            _jumping = true;
            _jumpBuffered = false;
            _currentJumpBufferTime = 0f; // Prevents repeats of jumpBuffering if jumpBufferTime is too long
            OnJumpPressed?.Invoke();
        }
        else if (!_gravObject.IsOnGround() && !_jumpHeld && Vector3.Dot(_gravObject.GetFallingVelocity(), _gravObject.GetGravityDirection()) < 0)
        {
            _gravObject.gravityMult = 1.5f;
        }

    }
    void Swing()
    {
        if (model != null && (_lassoObject as SwingableObject) != null)
        {
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation((_lassoObject as SwingableObject).GetApproximateSwingDirection(), _gravObject.characterOrientation.up), Time.deltaTime * 8);
        }
    }
    void Lasso()
    {
        LassoState prev = _lassoState;
        switch (_lassoState)
        {
            case LassoState.NONE:
                break;
            case LassoState.TOSS:
                if (lassoCurTime > lassoMaxTime)
                {
                    UpdateLassoState(LassoState.RETRACT);
                }
                break;
            case LassoState.THROWN:
                if (_lassoObject == null)
                {
                    UpdateLassoState(LassoState.RETRACT);
                }
                if (lassoCurTime > lassoMaxTime)
                {
                    if (_lassoObject as LassoTossable != null && _lassoObject.isLassoable)
                    {
                        _lassoObject.Grab();
                        UpdateLassoState(LassoState.PULL);
                    }
                    else if (_lassoObject as SwingableObject != null && _lassoObject.isLassoable)
                    {
                        _lassoObject.Grab();
                        UpdateLassoState(LassoState.SWING);
                    }
                    else
                    {
                        UpdateLassoState(LassoState.RETRACT);
                    }
                }
                break;
            case LassoState.SWING:
                // TODO: Swinging stuff :P
                break;
            case LassoState.PULL:
                // Perform pulling process
                LassoTossable tossable = (_lassoObject as LassoTossable);
                if (tossable == null)
                {
                    // Somehow became a null reference, just retract the lasso
                    UpdateLassoState(LassoState.RETRACT);
                }
                else
                {
                    if (lassoCurTime > lassoMaxTime)
                    {
                        UpdateLassoState(LassoState.HOLD);
                    }
                    else
                    {
                        tossable.PullLassoTossableTowards(lassoSwingCenter.position + tossable.GetSwingHeight() * lassoSwingCenter.up, lassoCurTime / lassoMaxTime);
                    }
                }
                break;
            case LassoState.HOLD:
                LassoTossable held = (_lassoObject as LassoTossable);
                if (held == null)
                {
                    // Somehow became a null reference, just retract the lasso
                    UpdateLassoState(LassoState.RETRACT);
                }
                else
                {
                    held.SwingLassoTossableAround(lassoSwingCenter, lassoCurTime);
                    _playerUI.SetThrowIndicatorPos(Mathf.Sin(lassoCurTime * lassoTossMinigameSpeed));
                    _lassoRenderer.SetTossArrowDirection(GetLassoTossDir(), _playerUI.GetTossIndicatorStrength());
                }
                break;
            case LassoState.RETRACT:
                if (lassoCurTime > lassoMaxTime)
                {
                    UpdateLassoState(LassoState.NONE);
                }
                break;
        }
        if (prev == _lassoState)
        {
            lassoCurTime += Time.deltaTime;
        }
    }


    // ************************ Callbacks / Event Handlers *************************** //
    Vector3 GetLassoTossDir()
    {

        Vector3 playerViewport = Camera.main.WorldToViewportPoint(transform.position);
        Vector3 cursorPosViewport = Camera.main.WorldToViewportPoint(_cursor.GetCursorRay().origin);
        playerViewport.z = 0;
        cursorPosViewport.z = 0;

        Vector3 viewportDirection = (cursorPosViewport - playerViewport);
        float vertScale = ((cursorPosViewport.y - 0.65f) / ((1.0f - 0.65f) * 2f));
        if (viewportDirection.y < 0)
        {
            vertScale = ((0.2f - cursorPosViewport.y) / 0.8f);
        }
        Vector3 rightBasis = Vector3.ProjectOnPlane(Camera.main.transform.right, _gravObject.characterOrientation.up);
        Vector3 forwardBasis = Vector3.ProjectOnPlane(Camera.main.transform.forward, _gravObject.characterOrientation.up);
        Vector3 tossDir = forwardBasis * viewportDirection.y + rightBasis * viewportDirection.x;
        tossDir = Vector3.Lerp(tossDir, _gravObject.characterOrientation.up, Mathf.Clamp(vertScale, 0.0f, 0.5f));
        tossDir.Normalize();
        Debug.DrawLine(transform.position, Camera.main.ViewportToWorldPoint(cursorPosViewport), Color.magenta);
        Debug.DrawRay(transform.position + transform.up * 3f, tossDir * 10f, Color.yellow);
        return tossDir;
    }

    void EndLassoSwing(Vector3 endingVel)
    {
        SwingableObject s = (_lassoObject as SwingableObject);
        s.SwingEnd -= EndLassoSwing;
        s.Release();

        UpdateState(State.AIR);
        UpdateLassoState(LassoState.RETRACT);
        controlPlayerConnection.gameObject.SetActive(false);
        _rigidbody.AddForce(endingVel + (transform.position - s.transform.position).normalized * 2f, ForceMode.VelocityChange);
    }

    int lastCutsceneIndex = -1;
    void DisableCharacterForCutscene(CutsceneObject activeScene)
    {
        disabledForCutscene = true;
        UpdateState(State.IDLE);
        _moveInput = Vector3.zero;
        _jumpHeld = false;
        _playerUI.HideThrowBar();
        lastCutsceneIndex = activeScene.index;
    }

    void EnableCharacterAfterCutscene(CutsceneObject activeScene)
    {
        if (activeScene.index < lastCutsceneIndex) { return; }
        disabledForCutscene = false;
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
        if (_lassoState == LassoState.HOLD)
        {
            _playerUI.ShowThrowBar();
        }
    }
}