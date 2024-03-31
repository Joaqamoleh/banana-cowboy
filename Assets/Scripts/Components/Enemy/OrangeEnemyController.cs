using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(GravityObject))]
public class OrangeEnemyController : EnemyController
{
    enum OrangeState
    {
        IDLE,
        COOLDOWN,
        PLAYER_SPOTTED,
        REV_UP,
        CHARGE,
        SLOW_DOWN,
        BOUNCE_BACK,
        DIZZY,
        HELD,
        TOSSED,
    }

    public enum OrangeSubStates
    {
        ROAM,
        TALK, 
        SLEEP,
        SWAY,
        STANDUP
    }

    private OrangeState _state = OrangeState.TOSSED;
    public OrangeSubStates subState;
    public GameObject[] destinationPoints = null;

    SoundPlayer _soundPlayer;
    GravityObject _gravObj;
    LassoableEnemy _lassoComp;
    Rigidbody _rb;

    [SerializeField]
    DetectionTriggerHandler sightRange, loseSightRange;

    [SerializeField]
    Collider bodyCollider;
    List<Obstacle> ignoredObstacles = new List<Obstacle>();

    [SerializeField]
    Animator orangeEnemyAnimator;

    [SerializeField]
    Transform model, partner;

    Transform target;
    Vector3 chargeTarget, moveDir;

    [SerializeField, Min(0f)]
    float timeToRoam = 3.0f, timeRoam = 2.0f, timeSleep = 5f, timeSpot = 0.3f, timeRevUp = 1.5f, timeCharge = 2.5f, timeSlowDown = 0.5f, timeDizzy = 2.5f, attackCooldown = 1.5f, timeToStandup = 1.4f;
    float timeStateChanged = 0f, timeToStateEnd = 1.0f, timeLastAttackEnd = 0f;
    
    [SerializeField, Range(0f, 100f)]
    float chanceToRoam = 0f, chanceToSleep = 10f, chanceToWake = 40f;

    [SerializeField, Min(0f)]
    float knockbackForce = 10f, walkSpeed = 20f, chargeSpeed = 80f;

    [SerializeField]
    LayerMask roamCollisionMask = -1;

    private void OnValidate()
    {
        if (chanceToRoam + chanceToSleep > 100f)
        {
            chanceToSleep = 100f - chanceToRoam;
        }
    }

    public void Awake()
    {
        sightRange.OnTriggerEntered += OnEnterSight;
        loseSightRange.OnTriggerExited += OnExitSight;
        _lassoComp = GetComponent<LassoableEnemy>();
        _lassoComp.isLassoable = false;
        _gravObj = GetComponent<GravityObject>();
        _soundPlayer = GetComponent<SoundPlayer>();
        _rb = GetComponent<Rigidbody>();

        if (subState == OrangeSubStates.ROAM && chanceToRoam <= 0f)
        {
            chanceToRoam = 20f;
        }
    }

    public void Start()
    {
        UpdateState(OrangeState.IDLE);
        UpdateSubState(subState);
    }

    public void Update()
    {
        if (enemyAIDisabled) { return; }
        if (_lassoComp.currentlyLassoed) 
        { 
            UpdateState(OrangeState.HELD); 
        }

        switch (_state)
        {
            case OrangeState.IDLE:
                if (target != null && subState != OrangeSubStates.STANDUP)
                {
                    UpdateState(OrangeState.COOLDOWN);
                    subState = OrangeSubStates.SWAY;
                }
                else if (Time.time - timeStateChanged > timeToStateEnd)
                {
                    switch (subState)
                    {
                        case OrangeSubStates.ROAM:
                            UpdateSubState(OrangeSubStates.SWAY);
                            break;
                        case OrangeSubStates.TALK:
                            break;
                        case OrangeSubStates.SLEEP:
                            if (UnityEngine.Random.Range(0f, 100f) < chanceToWake)
                            {
                                UpdateSubState(OrangeSubStates.SWAY);
                            }
                            break;
                        case OrangeSubStates.SWAY:
                            float rand = UnityEngine.Random.Range(0f, 100f);
                            if (rand < chanceToRoam)
                            {
                                UpdateSubState(OrangeSubStates.ROAM);
                            }
                            else if (rand < chanceToRoam + chanceToSleep)
                            {
                                UpdateSubState(OrangeSubStates.SLEEP);
                            }
                            break;
                        case OrangeSubStates.STANDUP:
                            UpdateSubState(OrangeSubStates.SWAY);
                            break;
                    }
                }
                else
                {
                    PerformSubstate();
                }
                break;
            case OrangeState.COOLDOWN:
                if (target == null)
                {
                    UpdateState(OrangeState.IDLE);
                }
                else if (Time.time - timeLastAttackEnd > attackCooldown)
                {
                    UpdateState(OrangeState.PLAYER_SPOTTED);
                }
                else
                {
                    // Look at player
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position), _gravObj.characterOrientation.up).normalized;
                    model.rotation = Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up);
                }
                break;
            case OrangeState.PLAYER_SPOTTED:
                if (Time.time - timeStateChanged > timeToStateEnd)
                {
                    UpdateState(OrangeState.REV_UP);
                }
                break;
            case OrangeState.REV_UP:
                if (target == null)
                {
                    UpdateState(OrangeState.IDLE);
                }
                else if (Time.time - timeStateChanged > timeToStateEnd)
                {
                    UpdateState(OrangeState.CHARGE);
                }
                else
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position), _gravObj.characterOrientation.up).normalized;
                    model.rotation = Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up);
                }
                break;
            case OrangeState.CHARGE:
                if (Vector3.Distance(transform.position, chargeTarget) < 6f || Time.time - timeStateChanged > timeToStateEnd)
                {
                    UpdateState(OrangeState.SLOW_DOWN);
                }
                else
                {
                    if (_gravObj.GetMoveVelocity().magnitude < chargeSpeed)
                    {
                        float t = ((Time.time - timeStateChanged) / timeToStateEnd) * 3f;
                        float speed = Mathf.Lerp(walkSpeed, chargeSpeed, t);
                        moveDir = Vector3.ProjectOnPlane(moveDir, _gravObj.characterOrientation.up).normalized;
                        model.rotation = Quaternion.LookRotation(moveDir, _gravObj.characterOrientation.up);
                        _rb.AddForce(moveDir * speed, ForceMode.Force);
                    }
                }
                break;
            case OrangeState.SLOW_DOWN:
                if (Time.time - timeStateChanged > timeToStateEnd)
                {
                    UpdateState(OrangeState.IDLE);
                }
                else
                {
                    float t = 1f - (Time.time - timeStateChanged) / timeToStateEnd;
                    Vector3 vel = _gravObj.GetMoveVelocity() * t;
                    _rb.AddForce(-vel, ForceMode.Force);
                }
                break;
            case OrangeState.BOUNCE_BACK:
                if (_gravObj.IsOnGround() && Time.time - timeStateChanged > timeToStateEnd)
                {
                    UpdateState(OrangeState.IDLE);
                }
                break;
            case OrangeState.DIZZY:
                if (Time.time - timeStateChanged > timeToStateEnd)
                {
                    subState = OrangeSubStates.STANDUP;
                    UpdateState(OrangeState.IDLE);

                }
                break;
            case OrangeState.HELD:
                if (!_lassoComp.currentlyLassoed)
                {
                    UpdateState(OrangeState.TOSSED);
                }
                break;
            default: 
                return;
        }
    }

    void PerformSubstate()
    {
        switch (subState)
        {
            case OrangeSubStates.ROAM:
                moveDir = Vector3.ProjectOnPlane(moveDir, _gravObj.characterOrientation.up).normalized;
                // Raycast to forward and down sure it is walkable and not off a ledge
#if DEBUG
                if (!Physics.Raycast(bodyCollider.transform.position, moveDir, walkSpeed * 0.5f, roamCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(bodyCollider.transform.position, moveDir * walkSpeed * 0.5f, Color.green);
                }
                else
                {
                    Debug.DrawRay(bodyCollider.transform.position, moveDir * walkSpeed * 0.5f, Color.red);
                }

                if (!Physics.SphereCast(bodyCollider.transform.position + moveDir * walkSpeed * 0.5f, 2f, -_gravObj.characterOrientation.up, out _, 6f, roamCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(bodyCollider.transform.position + moveDir * walkSpeed * 0.5f, -_gravObj.characterOrientation.up * 6f, Color.green);
                }
                else
                {
                    Debug.DrawRay(bodyCollider.transform.position + moveDir * walkSpeed * 0.5f, -_gravObj.characterOrientation.up * 6f, Color.red);
                }
#endif
                if (Physics.Raycast(bodyCollider.transform.position, moveDir, walkSpeed * 0.5f, roamCollisionMask, QueryTriggerInteraction.Ignore) ||
                    !Physics.SphereCast(bodyCollider.transform.position + moveDir * walkSpeed * 0.5f, 2f, -_gravObj.characterOrientation.up, out _, 6f, roamCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    UpdateSubState(OrangeSubStates.SWAY);
                }
                else
                {
                    model.rotation = Quaternion.LookRotation(moveDir, _gravObj.characterOrientation.up);
                    if (_gravObj.GetMoveVelocity().magnitude < walkSpeed)
                    {
                        _rb.AddForce(moveDir * walkSpeed * 3f, ForceMode.Force);
                    }
                }
                break;
            case OrangeSubStates.TALK:
                if (partner != null)
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((partner.position - transform.position), _gravObj.characterOrientation.up).normalized;
                }
                break;
            case OrangeSubStates.SLEEP:
            case OrangeSubStates.SWAY:
                break;
        }
    }
    
    void OnEnterSight(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            target = c.transform;
        }
    }

    void OnExitSight(Collider c)
    {
        if (c.CompareTag("Player") && _state != OrangeState.PLAYER_SPOTTED && _state != OrangeState.REV_UP)
        {
            target = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_state == OrangeState.CHARGE || _state == OrangeState.SLOW_DOWN)
        {
            Obstacle o = collision.collider.GetComponent<Obstacle>();
            if (collision != null && collision.collider != null)
            {
                if (collision.collider.CompareTag("Player") && collision.collider.transform.parent != null)
                {
                    Health h = collision.collider.transform.parent.GetComponentInParent<Health>();
                    Vector3 impulseForce = (collision.collider.transform.position - transform.position).normalized;
                    impulseForce = Vector3.ProjectOnPlane(impulseForce, _gravObj.characterOrientation.up).normalized * knockbackForce;
                    h.Damage(1, impulseForce + _gravObj.characterOrientation.up * 3f);
                    _rb.velocity = _gravObj.GetFallingVelocity();
                    _rb.AddForce(-impulseForce * 0.7f + _gravObj.characterOrientation.up * 25f, ForceMode.Impulse);
                    UpdateState(OrangeState.BOUNCE_BACK);
                }
                else if (o != null && !ignoredObstacles.Contains(o))
                {
                    UpdateState(OrangeState.DIZZY);
                    _rb.velocity = Vector3.zero;
                    ignoredObstacles.Add(o);
                    _soundPlayer.PlaySFX("HitObstacle");
                    StartCoroutine(IgnoreObstacle(o));
                }
            }
        }
    }

    IEnumerator IgnoreObstacle(Obstacle o)
    {
        yield return new WaitForSeconds(2.5f);
        ignoredObstacles.Remove(o);
    }

    void UpdateState(OrangeState state)
    {
        print("Updating State to " + state);
        if (state != _state)
        {
            print("state updated");
            _state = state;
            timeStateChanged = Time.time;
            UpdateAnimState();
            if (state == OrangeState.DIZZY)
            {
                _lassoComp.isLassoable = true;
            }
            else
            {
                _lassoComp.isLassoable = false;
            }

            switch (state)
            {
                case OrangeState.IDLE:
                    // update based on subState
                    if (subState == OrangeSubStates.STANDUP)
                    {
                        timeToStateEnd = timeToStandup;
                    }
                    else
                    {
                        timeToStateEnd = timeToRoam;
                    }
                    _rb.velocity = _gravObj.GetFallingVelocity();
                    break;
                case OrangeState.PLAYER_SPOTTED:
                    _soundPlayer.PlaySFX("Alert");
                    timeToStateEnd = timeSpot;
                    _rb.velocity = _gravObj.GetFallingVelocity();
                    break;
                case OrangeState.REV_UP:
                    _soundPlayer.PlaySFX("RevUp");
                    timeToStateEnd = timeRevUp;
                    _rb.velocity = _gravObj.GetFallingVelocity();
                    break;
                case OrangeState.CHARGE:
                    _soundPlayer.StopSFX("RevUp");
                    _soundPlayer.PlaySFX("Charge");
                    moveDir = Vector3.ProjectOnPlane((target.position - transform.position).normalized, _gravObj.characterOrientation.up).normalized;
                    chargeTarget = target.position + 15f * moveDir;
                    timeToStateEnd = timeCharge;
                    break;
                case OrangeState.SLOW_DOWN:
                    timeToStateEnd = timeSlowDown;
                    break;
                case OrangeState.BOUNCE_BACK:
                    timeToStateEnd = timeSlowDown;
                    timeLastAttackEnd = Time.time;
                    break;
                case OrangeState.DIZZY:
                    _soundPlayer.StopSFX("Charge");
                    _soundPlayer.PlaySFX("Dizzy");
                    GetComponentInChildren<ParticleSystem>().Play();
                    timeToStateEnd = timeDizzy;
                    break;
                case OrangeState.HELD:
                    _soundPlayer.StopSFX("Dizzy");
                    break;
                case OrangeState.TOSSED:
                    _soundPlayer.PlaySFX("Thrown");
                    enemyAIDisabled = true;
                    break;
            }
        }
    }

    void UpdateSubState(OrangeSubStates state)
    {
        subState = state;
        UpdateAnimState();
        _rb.velocity = _gravObj.GetFallingVelocity();
        timeStateChanged = Time.time;
        switch (subState)
        {
            case OrangeSubStates.ROAM:
                // Choose a direction to walk
                float angle = UnityEngine.Random.Range(0f, 360f);
                moveDir = _gravObj.characterOrientation.TransformDirection(Quaternion.Euler(0f, angle, 0f) * Vector3.forward);
                timeToStateEnd = timeRoam;
                break;
            case OrangeSubStates.TALK:
                if (partner != null)
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((partner.position - transform.position), _gravObj.characterOrientation.up).normalized;
                    model.rotation = Quaternion.FromToRotation(model.forward, lookDir) * model.rotation;
                }
                break;
            case OrangeSubStates.SLEEP:
                break;
            case OrangeSubStates.SWAY:
                break;
        }
    }

    void UpdateAnimState()
    {
        if (orangeEnemyAnimator == null) { return; }

        if (_state == OrangeState.IDLE)
        {
            //orangeEnemyAnimator.Play("Base Layer.OE_Idle");
            switch (subState)
            {
                case OrangeSubStates.ROAM:
                    orangeEnemyAnimator.Play("Base Layer.OE_Move_Start");
                    break;
                case OrangeSubStates.TALK:
                    if (partner != null)
                    {
                        orangeEnemyAnimator.Play("Base Layer.OE_Talk_Wave");
                    }
                    else
                    {
                        orangeEnemyAnimator.Play("Base Layer.OE_Idle_Dance");
                    }
                    break;
                case OrangeSubStates.SLEEP:
                    orangeEnemyAnimator.Play("Base Layer.OE_Idle_Laying_Start");
                    break;
                case OrangeSubStates.SWAY:
                    orangeEnemyAnimator.Play("Base Layer.OE_Idle_Dance");
                    break;
                case OrangeSubStates.STANDUP:
                    orangeEnemyAnimator.Play("Base Layer.OE_Stun_Reset");
                    break;
            }
        }
        if (_state == OrangeState.PLAYER_SPOTTED)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Player_Spotted");
        }
        if (_state == OrangeState.CHARGE)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Roll_Anticipation");
        }
        if (_state == OrangeState.DIZZY)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Stun");
        }
        if (_state == OrangeState.HELD)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Lassoed");
        }
        if (_state == OrangeState.REV_UP)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Rev_Up"); // CHANGE
        }
        if (_state == OrangeState.TOSSED)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Thrown");
        }
    }

    public void ChangeSightRange(float num)
    {
        sightRange.transform.localScale = new Vector3(num, num, num);
        loseSightRange.transform.localScale = new Vector3(num, num, num);
    }

    // Makes enemy go through player CHANGED 
    //private void OnCollisionStay(Collision collision)
    //{
    //    if (collision.gameObject.tag == "Player" && _state == OrangeState.CHARGE)
    //    {
    //        StartCoroutine("ChangeCollisionInteraction", collision.collider);
    //    }
    //}
    //private IEnumerator ChangeCollisionInteraction(Collider collider)
    //{
    //    Physics.IgnoreCollision(bodyCollider, collider, true);
    //    yield return new WaitForSeconds(0.5f);
    //    Physics.IgnoreCollision(bodyCollider.GetComponent<Collider>(), collider, false);
    //}


    //    [SerializeField]
    //    Transform _model = null;
    //    SoundPlayer _soundPlayer = null;

    //    public GameObject body;

    //    public float knockbackForce;
    //    public float chargeSpeed;
    //    public float maxChargeDistance = 70.0f;
    //    public float walkSpeed = 10.0f;

    //    public float timeToBeginRev = 0.4f;
    //    public float timeToBeginCharge = 0.3f;
    //    public float timeSpentCharging = 0.8f;
    ///*    public float timeSpentWalking;
    //    public float walkTimer;*/
    //    public float dizzyTime = 1.0f;

    //    private float spottedParam = 0.0f;

    //    public float timeSpentWalking;
    //    public float walkTimer;
    //    private float _distanceFromPartner = 11f;
    //    public GameObject destinationPoint;
    //    Vector3 _directionOfPartner;
    //    public GameObject partner;

    //    private Transform _spottedPlayerTransform = null;
    //    private GravityObject _gravObject = null;
    //    private LassoableEnemy _lassoComp = null;
    //    private Vector3 _chargeStartPoint;
    //    private Vector3 _chargeDirection;
    //    private Vector3 _chargeTargetPoint;

    //    public GameObject sightRange;

    //    public bool playerInView = false;

    //    public Animator orangeEnemyAnimator;
    //    // Start is called before the first frame update
    //    void Start()
    //    {
    //        _lassoComp = GetComponent<LassoableEnemy>();
    //        _lassoComp.isLassoable = false;
    //        _gravObject = GetComponent<GravityObject>();
    //        if (destinationPoints.Length > 1)
    //        {
    //            destinationPoint = destinationPoints[0];
    //        }
    ///*        else
    //        {
    //            subState = OrangeSubStates.SLEEP;
    //        }*/
    //        UpdateAnimState();
    //        _distanceFromPartner = Mathf.Pow(_distanceFromPartner, 2);
    //        _soundPlayer = GetComponent<SoundPlayer>();
    //        Debug.Assert(_soundPlayer != null);
    //    }

    //    void FixedUpdate()
    //    {
    //        if (_lassoComp.currentlyLassoed) { UpdateState(OrangeState.HELD); }
    //        if (_state == OrangeState.HELD)
    //        {
    //            if (!_lassoComp.currentlyLassoed)
    //            {
    //                UpdateState(OrangeState.THROWN);
    //            }
    //            return;
    //        }

    //        if ((_state == OrangeState.PLAYER_SPOTTED || _state == OrangeState.REV_UP) && _spottedPlayerTransform == null)
    //        {
    //            UpdateState(OrangeState.IDLE);
    //        }
    //        switch (_state)
    //        {
    //            case OrangeState.PLAYER_SPOTTED:
    //                spottedParam += Time.deltaTime;
    //                _chargeDirection = (_spottedPlayerTransform.position - transform.position).normalized;
    //                if (_model != null)
    //                {
    //                    _model.rotation = Quaternion.Slerp(_model.rotation,
    //                        Quaternion.LookRotation(_chargeDirection, _gravObject.characterOrientation.up),
    //                        spottedParam);
    //                }
    //                break;
    //            case OrangeState.REV_UP:
    //                _chargeDirection = (_spottedPlayerTransform.position - transform.position).normalized;
    //                if (_model != null)
    //                {
    //                    _model.rotation = Quaternion.LookRotation(_chargeDirection, _gravObject.characterOrientation.up);
    //                }
    //                break;
    //            case OrangeState.CHARGE:
    //                if (_gravObject.GetMoveVelocity().magnitude < chargeSpeed)
    //                {
    //                    GetComponent<Rigidbody>().AddForce(_chargeDirection * chargeSpeed);
    //                }
    //                break;
    //            case OrangeState.IDLE:
    //                PlayDifferentIdle();
    //                break;
    //            default:

    //                break;
    //        }
    //    }

    //    void UpdateState(OrangeState newState)
    //    {
    //        if (_state == OrangeState.HELD && newState != OrangeState.THROWN) { return; }
    //        if (_state != newState)
    //        {
    //            _state = newState;
    //            UpdateAnimState();

    //            switch (newState)
    //            {
    //                case OrangeState.PLAYER_SPOTTED:
    //                    spottedParam = 0.0f;
    //                    GetComponent<Rigidbody>().velocity = Vector3.zero + _gravObject.GetFallingVelocity();
    //                    _soundPlayer.PlaySFX("Alert");

    //                    Invoke("EndPlayerSpotted", timeToBeginRev);
    //                    break;
    //                case OrangeState.REV_UP:
    //                    _chargeStartPoint = transform.position;
    //                    _soundPlayer.PlaySFX("RevUp");
    //                    //SoundManager.Instance().PlaySFX("OrangeRevUp");
    //                    Invoke("EndRevUp", timeToBeginCharge);
    //                    break;
    //                case OrangeState.CHARGE:
    //                    _soundPlayer.StopSFX("RevUp");
    //                    _soundPlayer.PlaySFX("Charge");
    //                    //SoundManager.Instance().StopSFX("OrangeRevUp");
    //                    //SoundManager.Instance().PlaySFX("OrangeCharge");
    //                    Invoke("EndCharge", timeSpentCharging);
    //                    break;
    //                case OrangeState.DIZZY:
    //                    GetComponent<Rigidbody>().velocity = Vector3.zero + _gravObject.GetFallingVelocity();
    //                    //SoundManager.Instance().PlaySFX("OrangeDizzy");
    //                    _soundPlayer.PlaySFX("Dizzy");
    //                    GetComponentInChildren<ParticleSystem>().Play();
    //                    Invoke("EndDizzy", dizzyTime);
    //                    break;
    //                case OrangeState.IDLE:
    //                    GetComponent<Rigidbody>().velocity = Vector3.zero;
    //                    break;
    //                case OrangeState.THROWN:
    //                    _soundPlayer.PlaySFX("Thrown");
    //                    break;
    //                case OrangeState.HELD:
    //                    //SoundManager.Instance().StopSFX("OrangeDizzy");
    //                    _soundPlayer.StopSFX("Dizzy");
    //                    GetComponentInChildren<ParticleSystem>().Stop();
    //                    break;
    //            }
    //            if (newState == OrangeState.DIZZY || newState == OrangeState.HELD)
    //            {
    //                //print("Lassoable");
    //                _lassoComp.isLassoable = true;
    //            }
    //            else
    //            {
    //                //print("Not lassoable");
    //                _lassoComp.isLassoable = false;
    //            }
    //        }
    //    }

    //    void PlayDifferentIdle()
    //    {
    //        switch (subState)
    //        {
    //            case OrangeSubStates.WALK:
    //                walkTimer += Time.deltaTime;
    //                if (walkTimer >= timeSpentWalking)
    //                {
    //                    EndWalk();
    //                    walkTimer = 0;
    //                }
    //                if (destinationPoint != null)
    //                {
    //                    _directionOfPartner = (destinationPoint.transform.position - transform.position).normalized;
    //                }
    //                _directionOfPartner.x = 0;
    //                _model.rotation = Quaternion.Slerp(_model.rotation,
    //                                Quaternion.LookRotation(_directionOfPartner, _gravObject.characterOrientation.up),
    //                                spottedParam);
    //                spottedParam += Time.deltaTime;
    //                transform.position += 4 * Time.deltaTime * _directionOfPartner;
    //                /*if (_gravObject.GetMoveVelocity().magnitude < chargeSpeed)
    //                {
    //                    GetComponent<Rigidbody>().AddForce(_directionOfPartner * chargeSpeed * 0.3f);
    //                }*/

    //                break;
    //            case OrangeSubStates.TALK:
    //                if (partner != null)
    //                {
    //                    _directionOfPartner = (partner.transform.position - transform.position).normalized;

    //                    _model.rotation = Quaternion.Slerp(_model.rotation,
    //                                    Quaternion.LookRotation(_directionOfPartner, _gravObject.characterOrientation.up),
    //                                    spottedParam);
    //                    spottedParam += Time.deltaTime;
    //                }
    //                break;
    //            case OrangeSubStates.SLEEP:
    //                break;
    //            case OrangeSubStates.SWAY:
    //                break;
    //        }
    //    }

    //    void UpdateAnimState()
    //    {
    //        if (orangeEnemyAnimator == null) { return; }

    //        if (_state == OrangeState.IDLE)
    //        {
    //            //orangeEnemyAnimator.Play("Base Layer.OE_Idle");
    //            switch (subState)
    //            {
    //                case OrangeSubStates.WALK:
    //                    orangeEnemyAnimator.Play("Base Layer.OE_Move_Start");
    //                    break;
    //                case OrangeSubStates.TALK:
    //                    if (partner != null) {
    //                        orangeEnemyAnimator.Play("Base Layer.OE_Talk_Wave");
    //                    }
    //                    else
    //                    {
    //                        orangeEnemyAnimator.Play("Base Layer.OE_Idle_Dance");
    //                    }
    //                    break;
    //                case OrangeSubStates.SLEEP:
    //                    orangeEnemyAnimator.Play("Base Layer.OE_Idle_Laying_Start");
    //                    break;
    //                case OrangeSubStates.SWAY:
    //                    orangeEnemyAnimator.Play("Base Layer.OE_Idle_Dance");
    //                    break;
    //            }
    //        }
    //        if (_state == OrangeState.PLAYER_SPOTTED)
    //        {
    //            orangeEnemyAnimator.Play("Base Layer.OE_Player_Spotted");
    //        }
    //        if (_state == OrangeState.CHARGE)
    //        {
    //            orangeEnemyAnimator.Play("Base Layer.OE_Roll_Anticipation");
    //        }
    //        if (_state == OrangeState.DIZZY)
    //        {
    //            orangeEnemyAnimator.Play("Base Layer.OE_Stun");
    //        }
    //        if (_state == OrangeState.HELD)
    //        {
    //            orangeEnemyAnimator.Play("Base Layer.OE_Lassoed");
    //        }
    //        if (_state == OrangeState.REV_UP)
    //        {
    //            orangeEnemyAnimator.Play("Base Layer.OE_Rev_Up"); // CHANGE
    //        }
    //        if (_state == OrangeState.THROWN)
    //        {
    //            orangeEnemyAnimator.Play("Base Layer.OE_Thrown");
    //        }
    //    }


    //    void EndPlayerSpotted()
    //    {
    //        if (_state != OrangeState.PLAYER_SPOTTED) { return; }
    //        UpdateState(OrangeState.REV_UP);
    //    }

    //    void EndRevUp()
    //    {
    //        if (_state != OrangeState.REV_UP) { return; }
    //        UpdateState(OrangeState.CHARGE);
    //    }

    //    void EndCharge()
    //    {
    //        if (_state != OrangeState.CHARGE) { return; }
    //        if (playerInView)
    //        {
    //            UpdateState(OrangeState.PLAYER_SPOTTED);
    //        } 
    //        else
    //        {
    //            UpdateState(OrangeState.IDLE);
    //        }
    //        //UpdateState(OrangeState.DIZZY);
    //    }

    //    int rand;
    //    void EndWalk()
    //    {
    //        if (destinationPoints.Length > 0)
    //        {
    //            rand = UnityEngine.Random.Range(0,destinationPoints.Length);
    //            destinationPoint = destinationPoints[rand];
    //            GetComponent<Rigidbody>().velocity = Vector3.zero;
    //        }
    //    }

    //    void EndDizzy()
    //    {
    //        if (_state != OrangeState.DIZZY) { return; }

    //        //SoundManager.Instance().StopSFX("OrangeDizzy");
    //        _soundPlayer.StopSFX("Dizzy");
    //        if (playerInView)
    //        {
    //            UpdateState(OrangeState.PLAYER_SPOTTED);
    //        }
    //        else
    //        {
    //            _spottedPlayerTransform = null;
    //            UpdateState(OrangeState.IDLE);
    //        }
    //    }

    //    private void OnCollisionEnter(Collision collision)
    //    {
    //        if (collision != null && collision.gameObject != null)
    //        {
    //            if (collision.gameObject.GetComponent<Obstacle>() != null && _state != OrangeState.THROWN && _state != OrangeState.REV_UP && _state == OrangeState.CHARGE)
    //            {
    //                collision.gameObject.GetComponent<Obstacle>().Hit();
    //                _soundPlayer.StopSFX("Charge");
    //                _soundPlayer.PlaySFX("HitObstacle");
    //                //SoundManager.Instance().StopSFX("OrangeCharge");
    //                //SoundManager.Instance().PlaySFX("OrangeHitWall");
    //                UpdateState(OrangeState.DIZZY);
    //            }
    ///*            else if (collision.gameObject.GetComponent<Obstacle>() != null && _state == OrangeState.DIZZY)
    //            {
    //                StartCoroutine("ChangeCollisionInteraction", collision.collider);
    //                //GetComponent<Rigidbody>().AddForce((transform.position - collision.transform.position).normalized * 2, ForceMode.Impulse);
    //            }*/
    //        }
    //    }

    //    // Needed to make character not stick to walls.
    //    private void OnCollisionStay(Collision collision)
    //    {
    //        if (collision.gameObject.GetComponent<Obstacle>() != null && _state == OrangeState.IDLE)
    //        {
    //            StartCoroutine("ChangeCollisionInteraction", collision.collider);
    //        }
    //        else if (collision.gameObject.tag == "Player" && _state == OrangeState.CHARGE)
    //        {
    //            collision.gameObject.GetComponentInParent<Health>().Damage(1, knockbackForce * ((transform.position - collision.gameObject.transform.position).normalized + Vector3.up));
    //            StartCoroutine("ChangeCollisionInteraction", collision.collider);
    //        }
    //    }
    //    private IEnumerator ChangeCollisionInteraction(Collider collider)
    //    {
    //        Physics.IgnoreCollision(body.GetComponent<Collider>(), collider, true);
    //        yield return new WaitForSeconds(0.5f);
    //        Physics.IgnoreCollision(body.GetComponent<Collider>(), collider, false);
    //    }

    //    private void OnTriggerEnter(Collider other)
    //    {
    //        if (other != null && other.gameObject != null)
    //        {
    //            if (other.gameObject.tag == "Player")
    //            {
    //                _spottedPlayerTransform = other.gameObject.transform;
    //                playerInView = true;
    //                // was a bug where when a player jumps while enemy is charging (escapes the triggerbox), then enters again
    //                // if the player gets hit, would not take damage.
    //                if (_state == OrangeState.IDLE)
    //                {
    //                    UpdateState(OrangeState.PLAYER_SPOTTED);
    //                }
    //            }
    //        }
    //    }

    //    Vector3 distance;
    //    float sqrDistance;
    //    private void OnTriggerStay(Collider other)
    //    {
    //        if (partner == null && other.gameObject.CompareTag("Enemy"))
    //        {
    //            distance = other.transform.position - transform.position;
    //            sqrDistance = distance.sqrMagnitude;

    //            if (sqrDistance <= _distanceFromPartner)
    //            {
    //                partner = other.gameObject;
    //                UpdateAnimState();
    //            }
    //        }
    //        else if (partner != null && other.gameObject == partner)
    //        {
    //            distance = other.transform.position - transform.position;
    //            sqrDistance = distance.sqrMagnitude;

    //            if (sqrDistance > _distanceFromPartner)
    //            {
    //                partner = null;
    //                UpdateAnimState();
    //            }
    //        }
    //    }

    //    public void ChangeSightRange(float num)
    //    {
    //        sightRange.transform.localScale = new Vector3(num, 1, num);
    //    }

    //    private void OnTriggerExit(Collider other)
    //    {
    //        if (other != null && other.gameObject != null)
    //        {
    //            if (other.gameObject.tag == "Player")
    //            {
    //                playerInView = false;
    //            }
    ///*            else if (other.gameObject.tag == "Enemy")
    //            {
    //                partner = null;
    //            }*/
    //        }
    //    }
}
