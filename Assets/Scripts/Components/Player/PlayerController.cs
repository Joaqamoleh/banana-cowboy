using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    Transform lassoSwingCenter;

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
    [SerializeField]
    LayerMask clickTossMask;

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
    float lassoThrowSpeed = 12.5f, timeToPullTarget = 0.4f, timeToToss = 0.1f, timeToRetract = 1.0f, lassoTossMinigameSpeed = 3f;

    private bool _hasStartedTransition = false; // This initializes a transition. When set to false, will perform the initial action for the transition.
    private float _lassoParamAccumTime = 0f, _lassoParamMaxTime = 0f;
    private LassoObject _hoveredObject = null, _lassoObject = null;

    private void Awake()
    {
        StartCoroutine(SetSpawnPos());
    }
    IEnumerator SetSpawnPos()
    {
        yield return new WaitForEndOfFrame();
        if (GameObject.Find("CheckpointManager") != null)
        {
            print("Normal Levels");
            transform.position = LevelData.GetRespawnPos();
        }
        else
        {
            print("No checkpoints needed");
            transform.position = Vector3.zero;
        }
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
        _lassoRenderer = GetComponentInChildren<LassoRenderer>();
        Debug.Assert(_lassoRenderer != null);
        _lassoRenderer.StartRenderLasso();

        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += DisableCharacterForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += EnableCharacterAfterCutscene;
        }
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
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
                // Stops the buggy rotation model. IDK why it's happening, so this is a patchwork fix
                model.rotation = Quaternion.FromToRotation(model.up, _gravObject.characterOrientation.up) * model.rotation;
            }
            DetectStateChange();
        }
    }
    private void FixedUpdate()
    {
        Run();
        if (!disabledForCutscene && !PauseManager.pauseActive) {            
            Jump();
            Lasso();
        }
    }
    private void LateUpdate()
    {
        RenderLasso();
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
        SoundManager.Instance().PlaySFX("PlayerLand");
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
            print("Hovered object is: " + _hoveredObject + " in lasso state " + _lassoState);
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
        if (_lastTimeOnGround > 0 && _moveInput != Vector3.zero) // Check if the player is on the ground and moving
        {
            SoundManager.Instance().PlaySFX(_running ? "PlayerRun" : "PlayerWalk");
        }
        else
        {
            SoundManager.Instance().StopSFX("PlayerRun");
            SoundManager.Instance().StopSFX("PlayerWalk");
        }


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
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(targetVelocity.normalized, _gravObject.characterOrientation.up), Time.deltaTime * 8);
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
            SoundManager.Instance().PlaySFX("PlayerJump");
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
        else
        {
            PerformLassoActive();
        }
    }

    void PerformLassoTransition()
    {
        if (!_hasStartedTransition)
        {
            _hasStartedTransition = true;
            UpdateLassoState(_transitionState);

            print("Transitioning to new state: " + _transitionState);

            switch (_transitionState)
            {
                case LassoState.NONE:
                    break;
                case LassoState.RETRACT:
                    _lassoParamAccumTime = 0f;
                    _lassoParamMaxTime = timeToRetract;
                    break;
                case LassoState.THROWN:
                    _lassoParamAccumTime = 0f;
                    _lassoParamMaxTime = Vector3.Distance(transform.position, _lassoObject.transform.position) / lassoThrowSpeed;
                    break;
                case LassoState.SWING:
                    // Todo, perform init for swing, transition the player by pulling them towards the swing position based on the swing radius
                    break;
                case LassoState.HOLD:
                case LassoState.PULL:
                    // Perform init for pull and hold
                    LassoTossable pull = _lassoObject as LassoTossable;
                    if (pull == null)
                    {
                        UpdateLassoTransitionState(LassoState.NONE);
                    } 
                    else
                    {
                        _lassoParamAccumTime = 0f;
                        _lassoParamMaxTime = timeToPullTarget;
                    }
                    break;
                case LassoState.TOSS:
                    LassoTossable toss = _lassoObject as LassoTossable;
                    if (toss == null)
                    {
                        // Invalid state, just retract
                        UpdateLassoTransitionState(LassoState.RETRACT);
                    }
                    else if (!toss.IsTossed())
                    {
                        Ray r = _cursor.GetCursorRay();
                        Vector3 tossDir = _camera.forward;
                        RaycastHit hit;
                        if (Physics.Raycast(r, out hit, 100f, clickTossMask, QueryTriggerInteraction.Ignore))
                        {
                            tossDir = (hit.point - transform.position).normalized;
                        }
                        toss.TossInDirection(tossDir, _gravObject.characterOrientation.up, _playerUI.GetThrowIndicatorStrength());
                    }
                    _lassoParamAccumTime = 0.0f;
                    _lassoParamMaxTime = timeToToss;
                    break;
            }
        }
    }

    void PerformLassoActive()
    {
        switch (_lassoState)
        {
            case LassoState.NONE:
                break;
            case LassoState.TOSS:
                if (_lassoParamAccumTime > _lassoParamMaxTime)
                {
                    EndLassoTossState();
                }
                break;
            case LassoState.THROWN:
                if (_lassoObject == null)
                {
                    UpdateLassoTransitionState(LassoState.RETRACT);
                } 
                else
                {
                    if (_lassoParamAccumTime <= _lassoParamMaxTime)
                    {
                        Quaternion targetRot = Quaternion.Slerp(model.rotation, Quaternion.LookRotation((_lassoObject.GetLassoCenterPos() - transform.position).normalized, _gravObject.characterOrientation.up), Time.deltaTime * 8);
                        model.rotation = targetRot;
                    } 
                    else
                    {
                        EndLassoThrownState();
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
                    UpdateLassoTransitionState(LassoState.RETRACT);
                }
                else
                {
                    if (_lassoParamAccumTime > _lassoParamMaxTime)
                    {
                        UpdateLassoTransitionState(LassoState.HOLD);
                    } 
                    else
                    {
                        tossable.PullLassoTossableTowards(lassoSwingCenter.position + tossable.GetSwingHeight() * lassoSwingCenter.up, _lassoParamAccumTime / _lassoParamMaxTime);
                    }
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
                    held.SwingLassoTossableAround(lassoSwingCenter, _lassoParamAccumTime);
                    _playerUI.SetThrowIndicatorPos(Mathf.Sin(_lassoParamAccumTime * lassoTossMinigameSpeed));
                }
                break;
            case LassoState.RETRACT:
                if (_lassoParamAccumTime > _lassoParamMaxTime)
                {
                    EndLassoRetractState();
                }
                break;
        }
        _lassoParamAccumTime += Time.deltaTime;
    }

    void RenderLasso()
    {
        //print("Rendering Lasso with state: " + _lassoState);
        switch(_lassoState)
        {
            case LassoState.NONE:
                _lassoRenderer.RenderLassoAtHip();
                break;
            case LassoState.RETRACT:
                _lassoRenderer.RenderLassoRetract(_lassoParamAccumTime / _lassoParamMaxTime, lassoSwingCenter, _lassoParamMaxTime);
                break;
            case LassoState.THROWN:
                float angleRatio = 0;
                if (!_gravObject.IsInSpace())
                {
                    Vector3 dir = (transform.position - _lassoObject.transform.position).normalized;
                    Vector3 dirProj = Vector3.ProjectOnPlane(dir, _gravObject.characterOrientation.up).normalized;
                    angleRatio = Mathf.Abs(Vector3.Dot(dir, dirProj));
                }
                SoundManager.Instance().PlaySFX("LassoThrow");
                _lassoRenderer.RenderThrowLasso(_lassoParamAccumTime / _lassoParamMaxTime, _lassoObject, angleRatio);
                break;
            case LassoState.SWING:

                break;
            case LassoState.PULL:
                LassoTossable pulled = (_lassoObject as LassoTossable);
                if (pulled != null)
                {
                    _lassoRenderer.RenderLassoPull(_lassoParamAccumTime / _lassoParamMaxTime, pulled, lassoSwingCenter);
                }
                break;
            case LassoState.HOLD:
                LassoTossable held = (_lassoObject as LassoTossable);
                if (held != null) {
                    SoundManager.Instance().PlaySFX("LassoSpin");
                    _lassoRenderer.RenderLassoHold(_lassoParamAccumTime, held, lassoSwingCenter);
                }
                break;
            case LassoState.TOSS:
                SoundManager.Instance().StopSFX("LassoSpin");
                LassoTossable tossed = (_lassoObject as LassoTossable);
                if (tossed != null)
                {
                    _lassoRenderer.RenderLassoToss(_lassoParamAccumTime / _lassoParamMaxTime, tossed, lassoSwingCenter);
                }
                break;
        }
    }


    // ************************ Callbacks / Event Handlers *************************** //
    void OnLassoableEnterHover(LassoObject hovered)
    {
        //print("Hover enter on " + hovered);
        if (_hoveredObject != null)
        {
            _hoveredObject.Deselect();
        }
        _hoveredObject = hovered;
        _hoveredObject.Select();

    }
    void OnLassoableExitHovered(LassoObject hovered)
    {
        //print("Hover exit on " + hovered);
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
            if (_lassoObject as LassoTossable != null && _lassoObject.isLassoable)
            {
                _lassoObject.Grab();
                UpdateLassoTransitionState(LassoState.PULL);
            }
            else if (_lassoObject as SwingableObject != null && _lassoObject.isLassoable)
            {
                _lassoObject.Grab();
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

    void EndLassoPullState()
    {

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
        UpdateState(State.IDLE);
        _moveInput = Vector3.zero;
        _jumpHeld = false;
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.HIDDEN);
    }

    void EnableCharacterAfterCutscene(CutsceneObject activeScene)
    {
        disabledForCutscene = false;
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
    }
}