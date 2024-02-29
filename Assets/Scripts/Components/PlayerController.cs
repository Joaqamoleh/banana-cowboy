using UnityEngine;
using UnityEngine.Playables;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

[RequireComponent(typeof(GravityObject))]
public class PlayerController : MonoBehaviour
{
    bool disabledForCutscene = false;
    public enum ThrowStrength
    {
        WEAK,
        MEDIUM,
        STRONG
    }
    bool lassoObjectHeld = false;
    [Header("References")]
    Transform playerCamera;

    private GravityObject _gravObject;
    private Rigidbody _rigidbody;

    [Header("Movement")]
    [SerializeField]
    float walkSpeed = 10f;
    [SerializeField]
    float runSpeed = 19f, accelSpeed = (50f * 1f) / 19f, deccelSpeed = (50f * 1f) / 19f, airControlRatio = 0.8f;


    private Vector3 _moveInput = Vector3.zero;

    private bool _running = false;
    private float _lastTimeOnGround = 0f;

    [Header("Input")]
    [SerializeField]
    KeyCode lassoKey = KeyCode.Mouse0;
    [SerializeField]
    KeyCode runKey = KeyCode.LeftShift;

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
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
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
        }
    }
    private void FixedUpdate()
    {
        Run();
    }
    private void LateUpdate()
    {
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
        _lastTimeOnGround -= Time.deltaTime;
        if (_gravObject.IsOnGround())
        {
            _lastTimeOnGround = 0.1f;
        }
    #endif
    }
    void GetLassoInput()
    {
    #if UNITY_IOS || UNITY_ANDROID

    #else
        if (Input.GetKeyDown(lassoKey))
        {
            if (lassoObjectHeld)
            {
            } 
            else
            {
            }
        }
    #endif
    }

    // ********************** INPUT PROCESSING ************************* //

    void Run()
    {
        // Transform the move input relative to the camera
        _moveInput = playerCamera.TransformDirection(_moveInput);
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
        //if (_moveInput.magnitude > 0 && model != null)
        //{
        //    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(targetVelocity.normalized, model.transform.up), Time.deltaTime * 8);
        //}
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