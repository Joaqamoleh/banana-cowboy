using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BlueShooterController : EnemyController
{
    // STATES:
    // IDLE     ---- Player Enters Detection --->    SHOOTING
    // SHOOTING ---- Player Exits Outer Detection ---> IDLE


    // SHOOTING STATES: go in order
    // PREPARING / AIMING SHOT, SHOOTING, COOLDOWN
    // Can only exit shooting in preparing or cooldown state. Aiming and shooting will go to completion

    private enum ShooterState
    {
        IDLE,
        PLAYER_SPOTTED,
        PREPARING_SHOT,
        AIMING,
        SHOOTING,
        COOLDOWN,
    }
    private ShooterState state;

    [SerializeField]
    private DetectionTriggerHandler sightAgroRange, loseAgroRange;

    [SerializeField]
    GravityObject _gravObj;

    [SerializeField, Min(0f)]
    float aimingTime = 1.2f, cooldownTime = 0.0f;

    [SerializeField]
    EnemyProjectile projectileToShoot;

    [SerializeField]
    Transform projectileAttachLocation, model;
    [SerializeField]
    Animator animator;

    private Transform target;
    private EnemyProjectile projectileHeld;
    private Vector3 aimedDir, aimedPoint;
    private float timeStateChange = 0f, timeStateEnd = 1.0f;

    private void Awake()
    {
        Debug.Assert(sightAgroRange != null);
        Debug.Assert(loseAgroRange != null);
        sightAgroRange.OnTriggerEntered += EnterSightRange;
        loseAgroRange.OnTriggerExited += LeftSightRange;
    }


    private void Start()
    {
        UpdateState(ShooterState.IDLE);
    }

    private void Update()
    {
        if (enemyAIDisabled) { return; }

        // Detect State change based on current state
        switch (state)
        {
            case ShooterState.IDLE:
                if (target != null)
                {
                    UpdateState(ShooterState.PLAYER_SPOTTED);
                }
                break;
            case ShooterState.PLAYER_SPOTTED:
                // Wait for spotted animation to complete
                if (target == null)
                {
                    UpdateState(ShooterState.IDLE);
                }
                else if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(ShooterState.PREPARING_SHOT);
                }
                else
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position).normalized, _gravObj.characterOrientation.up);
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up), ((Time.time - timeStateChange) / timeStateEnd) * 3f);
                }
                break;
            case ShooterState.PREPARING_SHOT:
                // Wait for shot to be prepared (again animation to complete)
                if (target == null)
                {
                    UpdateState(ShooterState.IDLE);
                }
                else if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(ShooterState.AIMING);
                }
                else
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position).normalized, _gravObj.characterOrientation.up);
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up), ((Time.time - timeStateChange) / timeStateEnd) * 3f);
                }
                break;
            case ShooterState.AIMING:
                // Rotate towards the player and choose target throw direction
                if (target == null)
                {
                    UpdateState(ShooterState.IDLE);
                }
                else if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(ShooterState.SHOOTING);
                }
                else
                {
                    projectileHeld.transform.position = projectileAttachLocation.position;
                    // Perform state action
                    aimedDir = (target.position - projectileHeld.transform.position).normalized;
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position).normalized, _gravObj.characterOrientation.up);
                    aimedPoint = target.position;
                    // Rotate model towards aimingDirection
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up), ((Time.time - timeStateChange) / timeStateEnd) * 3f);
                }
                break;
            case ShooterState.SHOOTING:
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(ShooterState.COOLDOWN);
                }
                else if (Time.time - timeStateChange > timeStateEnd - 0.3f)
                {
                    projectileHeld.FireProjectile(aimedDir, aimedPoint);
                }
                else
                {
                    projectileHeld.transform.position = projectileAttachLocation.position;
                    if (target != null) // Not having null checks produces errors
                    {
                        aimedDir = (target.position - projectileHeld.transform.position).normalized;
                        Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position).normalized, _gravObj.characterOrientation.up);
                        aimedPoint = target.position;
                        model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up), ((Time.time - timeStateChange) / timeStateEnd) * 3f);
                    }
                }
                break;
            case ShooterState.COOLDOWN:
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    if (target == null)
                    {
                        UpdateState(ShooterState.IDLE);
                    }
                    else
                    {
                        UpdateState(ShooterState.PREPARING_SHOT);
                    }
                }
                else if (target != null)
                {
                    Vector3 lookDir = Vector3.ProjectOnPlane((target.position - transform.position).normalized, _gravObj.characterOrientation.up);
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(lookDir, _gravObj.characterOrientation.up), ((Time.time - timeStateChange) / timeStateEnd) * 3f);
                }
                break;
        }
    }

    private void EnterSightRange(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            target = c.transform;
        }
    }

    private void LeftSightRange(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            target = null;
        }
    }

    private void UpdateState(ShooterState state)
    {
        this.state = state;
        timeStateChange = Time.time;
        UpdateAnimState(state);
        switch (state)
        {
            case ShooterState.IDLE:
            case ShooterState.PLAYER_SPOTTED:
                if (state == ShooterState.PLAYER_SPOTTED)
                {
                    sfxPlayer.PlaySFX("Alert");
                }
                if (projectileHeld != null)
                {
                    Destroy(projectileHeld.gameObject);
                    projectileHeld = null;
                }
                break;
            case ShooterState.PREPARING_SHOT:
                sfxPlayer.PlaySFX("Windup");

                if (projectileHeld != null)
                {
                    Destroy(projectileHeld.gameObject);
                    projectileHeld = null;
                }
                projectileHeld = Instantiate(projectileToShoot, null, false);
                break;
            case ShooterState.AIMING:
                timeStateEnd = aimingTime;
                break;
            case ShooterState.SHOOTING:
                sfxPlayer.StopSFX("Windup");
                sfxPlayer.PlaySFX("Toss");
                break;
            case ShooterState.COOLDOWN:
                projectileHeld = null;
                timeStateEnd = cooldownTime;
                break;
        }
    }

    private void UpdateAnimState(ShooterState state)
    {
        switch(state)
        {
            case ShooterState.IDLE:
            case ShooterState.COOLDOWN:
                animator.Play("BE_Idle_1");
                break;
            case ShooterState.PLAYER_SPOTTED:
                animator.Play("BE_Notice_Player");
                timeStateEnd = animator.GetCurrentAnimatorStateInfo(0).length - 0.2f;
                break;
            case ShooterState.PREPARING_SHOT:
                //timeStateEnd = animator.GetCurrentAnimatorStateInfo(0).length;
                timeStateEnd = 0f;
                break;
            case ShooterState.AIMING:
                animator.Play("BE_Throw_Windup");
                //animator.Play("BE_Throw_Loop");
                break;
            case ShooterState.SHOOTING:
                animator.Play("BE_Throw_Attack");
                timeStateEnd = animator.GetCurrentAnimatorStateInfo(0).length;
                break;
        }
    }
}