using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

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

    [Header("References")]
    [SerializeField]
    Transform model;
    [SerializeField]
    Transform lassoHand;

    private Transform _camera;
    private GravityObject _gravObject;
    private Rigidbody _rigidbody;
    private Health _health;
    private PlayerCursor _cursor;
    private UIManager _playerUI;

    [Header("Input")]
    [SerializeField]
    KeyCode lassoKey = KeyCode.Mouse0;
    [SerializeField]
    KeyCode runKey = KeyCode.LeftShift, jumpKey = KeyCode.Space;

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
    float lassoRange = 20f;
    [SerializeField, Min(0f)]
    float timeToHitTarget = 0.2f, timeToPullTarget = 1f, timeToToss = 0.1f, timeToRetract = 1.0f;

    private bool _hasStartedTransition = false; // This initializes a transition. When set to false, will perform the initial action for the transition.
    private float _lassoParamT = 0f, _lassoParamAccumTime = 0f;
    private LassoObject _hoveredObject = null, _lassoObject = null;

    private void Awake()
    {

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
        _cursor.OnHoverLassoableEnter += OnLassoableEnterHover;
        _cursor.OnHoverLassoableExit += OnLassoableExitHovered;
        _playerUI = GetComponentInChildren<UIManager>();
        Debug.Assert(_playerUI != null);

        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += DisableCharacterForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += EnableCharacterAfterCutscene;
        }
    }
    private void Update()
    {
        if (!disabledForCutscene && !PauseManager.pauseActive && _health.health > 0)
        {
            GetMoveInput();
            GetLassoInput();
            DetectStateChange();
        }
    }
    private void FixedUpdate()
    {
        Run();
        Jump();
        Lasso();
    }
    private void LateUpdate()
    {
    }

    // ************************* State Updates ************************ //
    void UpdateState(State newState)
    {
        if (_state != newState)
        {
            OnStateChanged?.Invoke(newState);
            if (newState == State.AIR)
            {
                _detectLanding = true;
            }

            _state = newState;
        }
    }

    void UpdateLassoState(LassoState newState)
    {
        if (_lassoState != newState)
        {
            OnLassoStateChange?.Invoke(newState);
            _lassoState = newState;
            switch (newState)
            {
                case LassoState.HOLD:
                case LassoState.TOSS:
                    _playerUI.ShowThrowBar();
                    break;
                default:
                    _playerUI.HideThrowBar();
                    break;
            }
        }
    }

    void UpdateLassoTransitionState(LassoState newState)
    {
        _hasStartedTransition = false;
        _transitionState = newState;
    }

    void DetectStateChange()
    {
        _lastTimeOnGround -= Time.deltaTime;
        if (_gravObject.IsOnGround())
        {
            if (_detectLanding)
            {
                OnLand();
            }
            _lastTimeOnGround = coyoteTime;
        }
        if (_lastTimeOnGround < 0)
        {
            if (_state != State.SWING)
            {
                UpdateState(State.AIR);
            }
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
        _jumping = false;
        OnLanded?.Invoke(_gravObject.GetGround());

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

    void GetLassoInput()
    {
#if UNITY_IOS || UNITY_ANDROID
    //TODO: Later
#else
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
                case LassoState.NONE:
                    // Do lasso action based on what is selected, update to thrown
                    if (_hoveredObject != null) // TODO: Also check range relative to the player
                    {
                        UpdateLassoTransitionState(LassoState.THROWN);
                        _lassoObject = _hoveredObject;
                    }
                    break;
                case LassoState.THROWN: // DO NOT DO ANYTHING, I think it will mess things up bad to accept input here
                case LassoState.RETRACT:
                case LassoState.TOSS:
                    // Do nothing
                    break;
                case LassoState.SWING:
                    // End the swing
                    break;
                case LassoState.PULL:
                    // Do nothing (maybe toss the enemy immediately?)
                    break;
                case LassoState.HOLD:
                    // Show the throw indicator and throw when the player clicks
                    UpdateLassoTransitionState(LassoState.TOSS);
                    break;

            }
        }
#endif
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
        Debug.DrawRay(transform.position, movement, Color.magenta);

        // Spin player model and orientation to right direction to face
        if (_moveInput.magnitude > 0 && model != null)
        {
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(targetVelocity.normalized, model.transform.up), Time.deltaTime * 8);
        }
    }

    void Jump()
    {
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
        }

    }

    void Lasso()
    {
        // We need to transition to a new state, perform the initialization / transition operation
        if (_lassoState != _transitionState)
        {
            PerformLassoTransition();
        }
        
        // No need to perform a transition, do the active state
        if (_lassoState == _transitionState)
        {
            PerformLassoActive();
        }
    }

    void PerformLassoTransition()
    {
        // Perform transition
        switch (_transitionState)
        {
            case LassoState.NONE:
                if (!_hasStartedTransition)
                {
                    _hasStartedTransition = true;
                    UpdateLassoState(_transitionState); // Nothing to do here for now, just go to the state
                }
                break;
            case LassoState.RETRACT:
                if (!_hasStartedTransition)
                {
                    _hasStartedTransition = true;
                    UpdateLassoState(_transitionState);
                    Invoke("EndLassoRetractState", timeToRetract);
                }
                break;
            case LassoState.THROWN:
                // Entered either from NONE or RETRACT (i.e. _lassoState == NONE || _lassoState == RETRACT)
                if (!_hasStartedTransition)
                {
                    // Perform entering this state initialization
                    _hasStartedTransition = true;
                    UpdateLassoState(_transitionState);
                    Invoke("EndLassoThrownState", timeToHitTarget);
                }
                break;
            case LassoState.SWING:
                // Todo, perform init for swing, transition the player by pulling them towards the swing position based on the swing radius
                break;
            case LassoState.HOLD:
            case LassoState.PULL:
                if (!_hasStartedTransition)
                {
                    // Perform init for pull and hold
                    _lassoParamAccumTime = 0f;
                    _lassoParamT = 0f;
                    _hasStartedTransition = true;
                    UpdateLassoState(_transitionState);
                }
                break;
            case LassoState.TOSS:
                if (!_hasStartedTransition)
                {
                    LassoTossable toss = _lassoObject as LassoTossable;
                    if (toss == null)
                    {
                        // Invalid state, just retract
                        UpdateLassoTransitionState(LassoState.RETRACT);
                    }
                    else if (!toss.IsTossed())
                    {
                        toss.TossInDirection(model.forward, _gravObject.characterOrientation.up, _playerUI.GetThrowIndicatorStrength());
                    }
                    _hasStartedTransition = true;
                    UpdateLassoState(_transitionState);
                    Invoke("EndLassoTossState", timeToToss);
                }
                break;
        }
    }

    void PerformLassoActive()
    {
        switch (_lassoState)
        {
            case LassoState.NONE:
            case LassoState.THROWN:
            case LassoState.TOSS:
                // Nothing to do here
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
                    UpdateLassoTransitionState(LassoState.RETRACT);
                }
                else
                {
                    if (_lassoParamT > 1f)
                    {
                        UpdateLassoTransitionState(LassoState.HOLD);
                    }
                    tossable.PullLassoTossableTowards(lassoHand.position
                        + tossable.GetSwingHeight() * lassoHand.up
                        + tossable.GetSwingRadius() * lassoHand.forward,
                        _lassoParamT);
                    _lassoParamAccumTime += Time.deltaTime;
                    _lassoParamT = _lassoParamAccumTime / timeToPullTarget;
                }
                break;
            case LassoState.HOLD:
                LassoTossable held = (_lassoObject as LassoTossable);
                if (held == null)
                {
                    // Somehow became a null reference, just retract the lasso
                    UpdateLassoTransitionState(LassoState.RETRACT);
                }
                else
                {
                    held.SwingLassoTossableAround(lassoHand, _lassoParamT);
                    _lassoParamAccumTime += Time.deltaTime;
                    _lassoParamT = _lassoParamAccumTime / timeToPullTarget;
                    _playerUI.SetThrowIndicatorPos(Mathf.Sin(_lassoParamT));
                }
                break;
        }
    }

    // ************************ Callbacks / Event Handlers *************************** //
    void OnLassoableEnterHover(LassoObject hovered)
    {
        if (_hoveredObject != null)
        {
            _hoveredObject.Deselect();
        }
        _hoveredObject = hovered;
        _hoveredObject.Select();

    }
    void OnLassoableExitHovered(LassoObject hovered)
    {
        if (_hoveredObject == hovered)
        {
            if (_hoveredObject != null)
            {
                _hoveredObject.Deselect();
                _hoveredObject = null;
            }
        }
    }
    void EndLassoThrownState()
    {
        if (_lassoObject != null)
        {
            if (_lassoObject as LassoTossable != null)
            {
                _lassoObject.currentlyLassoed = true; // TODO: Very important, turn these back to false when not lasso-ed anymore
                UpdateLassoTransitionState(LassoState.PULL);
            }
            else if (_lassoObject as SwingableObject != null)
            {
                _lassoObject.currentlyLassoed = true;
                UpdateLassoTransitionState(LassoState.SWING);
            }
            else
            {
                UpdateLassoTransitionState(LassoState.RETRACT);
            }
        }
        else
        {
            UpdateLassoTransitionState(LassoState.RETRACT);
        }
    }
    void EndLassoTossState()
    {
        UpdateLassoTransitionState(LassoState.RETRACT);
    }

    void EndLassoRetractState()
    {
        UpdateLassoTransitionState(LassoState.NONE);
    }

    void DisableCharacterForCutscene(CutsceneObject activeScene)
    {
        disabledForCutscene = true;
    }

    void EnableCharacterAfterCutscene(CutsceneObject activeScene)
    {
        disabledForCutscene = false;
    }
}