using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BlueberrySmackerController : EnemyController
{
    [SerializeField]
    private DetectionTriggerHandler sightAgroRange, loseAgroRange, attackRangeTrigger;

    GravityObject _gravObj;
    Rigidbody _rb;

    [SerializeField]
    Collider bodyCollider;

    [SerializeField]
    Transform model;

    public enum SmackerState
    {
        IDLE,
        PLAYER_SPOTTED,
        CHASING,
        WAIT_AT_EDGE,
        SMACK_WINDUP,
        SMACK_ATTACK,
        DIZZY,
        HELD,
        TOSSED,
    }
    private SmackerState _state = SmackerState.TOSSED;

    [SerializeField, Min(0f)]
    float spottedTime = 0.2f, smackWindupTime = 0.2f, smackAttackTime = 0.2f, dizzyTime = 2.5f, attackCooldown = 1.5f, chaseSpeed = 40f;
    private float timeStateChange = 0f, timeStateEnd = 1.0f, timeLastAttack = 0f;

    Transform target = null;
    Vector3 moveDir;


    [SerializeField]
    LayerMask chaseCollisionMask = -1;

    // Start is called before the first frame update
    void Start()
    {
        UpdateState(SmackerState.IDLE);
    }

    private void UpdateState(SmackerState state)
    {
        if (_state != state)
        {
            timeStateChange = Time.time;
            UpdateAnimState(state);

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
                    break;
                case SmackerState.SMACK_WINDUP:
                    timeStateEnd = smackWindupTime;
                    break;
                case SmackerState.SMACK_ATTACK:
                    timeStateEnd = smackAttackTime;
                    timeLastAttack = Time.time;
                    break;
                case SmackerState.DIZZY:
                    timeStateEnd = dizzyTime;
                    break;
                case SmackerState.HELD:
                    enemyAIDisabled = true;
                    break;
                case SmackerState.TOSSED:
                    enemyAIDisabled = true;
                    break;
            }
        }
    }

    private void UpdateAnimState(SmackerState state)
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch(_state)
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
            case SmackerState.CHASING:
                if (target == null)
                {
                    UpdateState(SmackerState.IDLE);
                }
                else
                {
                    if (Physics.Raycast(bodyCollider.transform.position, moveDir, out RaycastHit h, chaseSpeed * 0.5f, chaseCollisionMask, QueryTriggerInteraction.Ignore) ||
                        !Physics.SphereCast(bodyCollider.transform.position + moveDir * chaseSpeed * 0.5f, 2f, -_gravObj.characterOrientation.up, out _, 6f, chaseCollisionMask, QueryTriggerInteraction.Ignore))
                    {
                        if (h.collider != null)
                        {
                            // Evade obstacle 
                            // Ring of raycasts around the character, pick the one direction closest to the target direction
                            // that isn't colliding with anything.

                        }
                        else
                        {
                            UpdateState(SmackerState.WAIT_AT_EDGE);
                        }
                    }
                    else
                    {
                        // Clear to move directly towards the 
                        model.rotation = Quaternion.LookRotation(moveDir, _gravObj.characterOrientation.up);
                        if (_gravObj.GetMoveVelocity().magnitude < chaseSpeed)
                        {
                            _rb.AddForce(moveDir * chaseSpeed, ForceMode.Force);
                        }
                    }
                }
                break;
            case SmackerState.WAIT_AT_EDGE:
                // Rotate to watch target as long as it exists.
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(SmackerState.IDLE);
                    // Update substate to patrol?
                }
                break;
            case SmackerState.SMACK_WINDUP:
                break;
            case SmackerState.SMACK_ATTACK:
                break;
            case SmackerState.DIZZY:
                break;
            case SmackerState.HELD:
                break;
            case SmackerState.TOSSED:
                break;
        }
    }
}
