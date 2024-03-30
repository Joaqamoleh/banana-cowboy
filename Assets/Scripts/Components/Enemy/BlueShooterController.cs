using System.Collections;
using System.Collections.Generic;
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
    float spottedTime = 0.2f, prepareShotTime = 0.4f, aimingTime = 1.2f, shootingTime = 0.1f, cooldownTime = 0.0f;

    [SerializeField]
    EnemyProjectile projectileToShoot;

    [SerializeField]
    Transform projectileAttachLocation, model;

    private Transform target;
    private EnemyProjectile projectileHeld;
    private Vector3 aimedDir, aimedPoint;
    private float timeStateChange = 0f, timeStateEnd = 1.0f;

    private void Awake()
    {
        UpdateState(ShooterState.IDLE);
        Debug.Assert(sightAgroRange != null);
        Debug.Assert(loseAgroRange != null);
        sightAgroRange.OnTriggerEntered += EnterSightRange;
        loseAgroRange.OnTriggerExited += LeftSightRange;
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
                // Play toss animation
                if (Time.time - timeStateChange > timeStateEnd)
                {
                    UpdateState(ShooterState.COOLDOWN);
                }
                else
                {

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
        switch (state)
        {
            case ShooterState.IDLE:
                timeStateEnd = 0.0f;
                if (projectileHeld != null)
                {
                    projectileHeld.transform.parent = null;
                    projectileHeld.FireProjectile(-_gravObj.characterOrientation.up, projectileHeld.transform.position - _gravObj.characterOrientation.up * 5f);
                    projectileHeld = null;
                }
                break;
            case ShooterState.PLAYER_SPOTTED:
                timeStateEnd = spottedTime;
                break;
            case ShooterState.PREPARING_SHOT:
                timeStateEnd = prepareShotTime;
                break;
            case ShooterState.AIMING:
                projectileHeld = Instantiate(projectileToShoot, null, false);
                timeStateEnd = aimingTime;
                break;
            case ShooterState.SHOOTING:
                timeStateEnd = shootingTime;
                break;
            case ShooterState.COOLDOWN:
                projectileHeld.transform.parent = null;
                projectileHeld.FireProjectile(aimedDir, aimedPoint);
                projectileHeld = null;
                timeStateEnd = cooldownTime;
                break;
        }
    }
}