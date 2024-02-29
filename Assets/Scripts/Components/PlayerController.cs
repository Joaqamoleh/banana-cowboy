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

    public delegate void PlayerLanded();
    public event PlayerLanded OnLanded;

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

    public enum TossStrength
    {
        WEAK,
        MEDIUM,
        STRONG
    }

    [Header("References")]
    [SerializeField]
    Transform model;

    private Transform _camera;
    private GravityObject _gravObject;
    private Rigidbody _rigidbody;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    float walkSpeed = 10f;
    [SerializeField, Min(0f)]
    float runSpeed = 19f, accelSpeed = (50f * 1f) / 19f, deccelSpeed = (50f * 1f) / 19f, jumpImpulseForce = 30f;
    [SerializeField, Range(0f, 1f)]
    float airControlRatio = 0.8f;

    private Vector3 _moveInput = Vector3.zero;
    private bool _jumpHeld = false, _jumping = false, _running = false, _detectLanding = false;
    private float _lastTimeOnGround = 0f;

    [Header("Input")]
    [SerializeField]
    KeyCode lassoKey = KeyCode.Mouse0, runKey = KeyCode.LeftShift, jumpKey = KeyCode.Space;


    private void Awake()
    {
        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += DisableCharacterForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += EnableCharacterAfterCutscene;
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
    }
    private void Update()
    {
        if (!disabledForCutscene && !PauseManager.pauseActive)
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
        }
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
            _lastTimeOnGround = 0.3f;
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
        OnLanded?.Invoke();
    }

    // ************************* INPUT ************************ //
    void GetMoveInput()
    {
    #if UNITY_IOS || UNITY_ANDROID

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

        if (Input.GetKeyDown(jumpKey))
        {
            _jumpHeld = true;
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

    #else
        if (_moveInput.magnitude > 0)
        {
            // Cancel lasso thrown state.
            // Took out b/c probably not intuitive
            //if (_lassoState == LassoState.THROWN)
            //{
            //    UpdateLassoState(LassoState.RETRACT);
            //}
        }

        if (Input.GetKeyDown(lassoKey))
        {
            switch (_lassoState)
            {
                case LassoState.NONE: 
                    // Do lasso action based on what is selected, update to thrown
                    break;
                case LassoState.THROWN: 
                    // Do nothing
                    break;
                case LassoState.SWING: 
                    // End the swing
                    break;
                case LassoState.PULL:
                    // Do nothing
                    break;
                case LassoState.HOLD:
                    break;
                case LassoState.RETRACT: 
                    // Do nothing
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

        //if (_moveInput.magnitude > 0)
        //{
        //    _gravObject.SetCharacterForward(targetVelocity.normalized);
        //}

        //// Spin player model and orientation to right direction to face
        if (_moveInput.magnitude > 0 && model != null)
        {
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(targetVelocity.normalized, model.transform.up), Time.deltaTime * 8);
        }
    }

    void Jump()
    {
        if (_jumpHeld && _lastTimeOnGround > 0.0f && !_jumping) {
            if (_gravObject.GetFallingVelocity().magnitude > 0)
            {
                _gravObject.SetFallingVelocity(0);
            }
            _rigidbody.AddForce(_gravObject.characterOrientation.up * jumpImpulseForce, ForceMode.Impulse);
            _jumping = true;
        }
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