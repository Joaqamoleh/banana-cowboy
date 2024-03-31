using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class BlueberrySmackerController : EnemyController
{
    [SerializeField]
    private DetectionTriggerHandler sightAgroRange, loseAgroRange, attackRangeTrigger;

    GravityObject _gravObj;
    Rigidbody _rb;
    LassoableEnemy _lassoComp;

    [SerializeField]
    Collider bodyCollider;

    [SerializeField]
    Transform model, detectFallLocation;

    [SerializeField]
    MeleeHitbox hitboxPrefab;
    MeleeHitbox _meleeHitbox;

    [SerializeField]
    GameObject attackDebugIndicator, dizzyDebugIndicator, attackSuccessIndicator;

    public enum SmackerState
    {
        IDLE,
        COOLDOWN,
        PLAYER_SPOTTED,
        CHASING,
        WAIT_AT_EDGE,
        SMACK_WINDUP,
        SMACK_ATTACK,
        DIZZY,
        HIT_SUCCESS,
        HELD,
        TOSSED,
    }
    private SmackerState _state = SmackerState.TOSSED;

    [SerializeField, Min(0f)]
    float spottedTime = 0.2f, smackWindupTime = 0.2f, smackAttackTime = 0.2f, dizzyTime = 2.5f, attackCooldown = 1.5f, 
        chaseSpeed = 15f, accelSpeed = (50f * 1f) / 19f, deccelSpeed = (50f * 1f) / 19f, knockbackForce = 60f;
    private float timeStateChange = 0f, timeStateEnd = 1.0f, timeLastAttack = 0f;

    Transform target = null;
    Vector3 attackPos;
    bool playerInAttackRange = false;


    [SerializeField]
    LayerMask chaseCollisionMask = -1;

    void Start()
    {
        _gravObj = GetComponent<GravityObject>();
        _lassoComp = GetComponent<LassoableEnemy>();
        _lassoComp.isLassoable = false;
        _rb = GetComponent<Rigidbody>();
        sightAgroRange.OnTriggerEntered += OnSightEntered;
        loseAgroRange.OnTriggerExited += OnSightExited;
        attackRangeTrigger.OnTriggerEntered += OnAttackRangeEntered;
        attackRangeTrigger.OnTriggerExited += OnAttackRangeExited;
        UpdateState(SmackerState.IDLE);
    }

    void Update()
    {
        if (enemyAIDisabled) { return; }

        if (_lassoComp.currentlyLassoed) { UpdateState(SmackerState.HELD); }

        switch (_state)
        {
            case SmackerState.IDLE:
                if (target != null)
                {
                    UpdateState(SmackerState.PLAYER_SPOTTED);
                }
                break;
            case SmackerState.PLAYER_SPOTTED:
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(SmackerState.CHASING);
                }
                break;
            case SmackerState.COOLDOWN:
                if (target == null)
                {
                    UpdateState(SmackerState.IDLE);
                }
                else if (Time.time - timeLastAttack > attackCooldown)
                {
                    if (playerInAttackRange)
                    {
                        UpdateState(SmackerState.SMACK_WINDUP);
                    }
                    else
                    {
                        UpdateState(SmackerState.CHASING);
                    }
                }
                else
                {
                    // Look at player
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position), _gravObj.characterOrientation.up).normalized;
                    model.rotation = Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up);
                }
                break;
            case SmackerState.CHASING:
                if (target == null)
                {
                    UpdateState(SmackerState.IDLE);
                }
                else
                {
                    if (!Physics.SphereCast(detectFallLocation.position, 0.5f, -_gravObj.characterOrientation.up, out _, 6f, chaseCollisionMask, QueryTriggerInteraction.Ignore))
                    {
                        print("BRUH");
                        UpdateState(SmackerState.WAIT_AT_EDGE);
                    }
                    else if (!playerInAttackRange)
                    {
                        Debug.DrawRay(detectFallLocation.position, -_gravObj.characterOrientation.up * 8f, Color.green);
                        // Clear to move directly towards the player
                        Vector3 currentVel = _gravObj.GetMoveVelocity();
                        Vector3 targetVel = Vector3.ProjectOnPlane(target.position - transform.position, _gravObj.characterOrientation.up).normalized * chaseSpeed;
                        float accelRate = (Vector3.Dot(targetVel, currentVel) > 0) ? accelSpeed : deccelSpeed;

                        if (currentVel.magnitude > targetVel.magnitude &&
                            Vector3.Dot(currentVel, targetVel) > 0 &&
                            targetVel.magnitude > 0.01f && _gravObj.IsOnGround())
                        {
                            //Prevent any deceleration from happening, or in other words conserve are current momentum
                            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
                            accelRate = 0;
                        }
                        Vector3 speedDiff = targetVel - currentVel;
                        Vector3 movement = speedDiff * accelRate;
                        _rb.AddForce(movement);

                        Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position), _gravObj.characterOrientation.up).normalized;
                        model.rotation = Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up);
                    }
                    else
                    {
                        // Player in attack range, hit them!
                        UpdateState(SmackerState.SMACK_WINDUP);
                    }
                }
                break;
            case SmackerState.WAIT_AT_EDGE:
                // Rotate to watch target as long as it exists.
                if (target == null || Time.time - timeStateChange > timeStateEnd)
                {
                    // Update substate to patrol?
                    UpdateState(SmackerState.IDLE);
                }
                else
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position), _gravObj.characterOrientation.up).normalized;
                    model.rotation = Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up);
                    if (Physics.SphereCast(detectFallLocation.position, 0.5f, -_gravObj.characterOrientation.up, out _, 6f, chaseCollisionMask, QueryTriggerInteraction.Ignore))
                    {
                        UpdateState(SmackerState.CHASING);
                    }
                    else
                    {
                        Debug.DrawRay(detectFallLocation.position, -_gravObj.characterOrientation.up * 8f, Color.red);
                    }
                }
                break;
            case SmackerState.SMACK_WINDUP:
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(SmackerState.SMACK_ATTACK);
                }
                break;
            case SmackerState.SMACK_ATTACK:
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    Vector3 knockback = Vector3.ProjectOnPlane((attackPos - transform.position), _gravObj.characterOrientation.up).normalized * knockbackForce;
                    if (_meleeHitbox.PerformAttack(0, knockback))
                    {
                        // Attack success!
                        UpdateState(SmackerState.HIT_SUCCESS);
                    }
                    else
                    {
                        // Attack failed :(
                        UpdateState(SmackerState.DIZZY);
                    }
                    timeLastAttack = Time.time;
                }
                break;
            case SmackerState.HIT_SUCCESS:
                if (target == null)
                {
                    UpdateState(SmackerState.IDLE);
                }
                else if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(SmackerState.COOLDOWN);
                }
                else
                {
                    // Might not be a needed state, might be fun to have the blueberry laugh at the player while in this state
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position), _gravObj.characterOrientation.up).normalized;
                    model.rotation = Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up);
                }
                break;
            case SmackerState.DIZZY:
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    if (target != null)
                    {
                        UpdateState(SmackerState.COOLDOWN);
                    }
                    else
                    {
                        UpdateState(SmackerState.IDLE);
                    }
                }
                break;
            case SmackerState.HELD:
                if (!_lassoComp.currentlyLassoed)
                {
                    UpdateState(SmackerState.TOSSED);
                }
                break;
            case SmackerState.TOSSED:
                break;
        }
    }
    void OnSightEntered(Collider c)
    {
        print("Sight entered " + c.name);
        if (c.CompareTag("Player"))
        {
            print("target updated");
            target = c.transform;
        }
    }

    void OnSightExited(Collider c)
    {
        print("Sight left " + c.name);
        if (c.CompareTag("Player"))
        {
            print("target updated null");
            target = null;
        }
    }

    void OnAttackRangeEntered(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            playerInAttackRange = true;
        }
    }

    void OnAttackRangeExited(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            playerInAttackRange = false;
        }
    }

    private void UpdateState(SmackerState state)
    {
        if (_state != state)
        {
            print("In state " + state);
            _state = state;
            timeStateChange = Time.time;
            UpdateAnimState(state);

            _lassoComp.isLassoable = state == SmackerState.DIZZY;

            if (state != SmackerState.CHASING || state != SmackerState.HELD || state != SmackerState.TOSSED)
            {
                _rb.velocity = _gravObj.GetFallingVelocity(); // Stop moving
            }
            switch (state)
            {
                case SmackerState.IDLE:
                    // Substates?
                    break;
                case SmackerState.PLAYER_SPOTTED:
                    timeStateEnd = spottedTime;
                    break;
                case SmackerState.CHASING:
                    // Max chasing time, 20 seconds for now
                    timeStateEnd = 20f;
                    break;
                case SmackerState.WAIT_AT_EDGE:
                    // Wait for some time, then go back to idle (patrol?)
                    timeStateEnd = 5f; // Timeout time
                    break;
                case SmackerState.SMACK_WINDUP:
                    timeStateEnd = smackWindupTime;
                    _meleeHitbox = Instantiate(hitboxPrefab);
                    _meleeHitbox.transform.position = attackRangeTrigger.transform.position;
                    attackPos = _meleeHitbox.transform.position;
                    break;
                case SmackerState.SMACK_ATTACK:
                    timeStateEnd = smackAttackTime;
                    break;
                case SmackerState.DIZZY:
                    Destroy(_meleeHitbox.gameObject);
                    _meleeHitbox = null;
                    timeStateEnd = dizzyTime;
                    break;
                case SmackerState.HIT_SUCCESS:
                    Destroy(_meleeHitbox.gameObject);
                    _meleeHitbox = null;
                    timeStateEnd = 0.2f;
                    break;
                case SmackerState.HELD:
                    break;
                case SmackerState.TOSSED:
                    enemyAIDisabled = true;
                    break;
            }
        }
    }

    private void UpdateAnimState(SmackerState state)
    {
        dizzyDebugIndicator.SetActive(state == SmackerState.DIZZY);
        attackDebugIndicator.SetActive(state == SmackerState.SMACK_WINDUP);
        attackSuccessIndicator.SetActive(state == SmackerState.HIT_SUCCESS);

        switch (state)
        {
            case SmackerState.IDLE:
                break;
            case SmackerState.COOLDOWN:
                break;
            case SmackerState.PLAYER_SPOTTED:
                break;
            case SmackerState.CHASING:
                break;
            case SmackerState.WAIT_AT_EDGE:
                break;
            case SmackerState.SMACK_WINDUP:
                break;
            case SmackerState.SMACK_ATTACK:
                break;
            case SmackerState.DIZZY:
                break;
            case SmackerState.HIT_SUCCESS:
                break;
            case SmackerState.HELD:
                break;
            case SmackerState.TOSSED:
                break;
        }
    }

}
