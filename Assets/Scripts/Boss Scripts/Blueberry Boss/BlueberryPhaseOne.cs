using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UI;


public class BlueberryPhaseOne : BossController
{
    [SerializeField]
    Renderer[] bossRenderers;
    [SerializeField]
    Material[] bossDamageMats;

    [SerializeField]
    DetectionTriggerHandler bossAttackTrigger;

    [SerializeField]
    ArenaSpawners[] obstacleSpawners, barrelSpawners;

    [SerializeField]
    CutsceneObject phaseOneCutscene;

    [SerializeField]
    Transform player, bossOrientation;

    [SerializeField, Min(0f)]
    float damageStunDuration = 0.5f, windupDuration = 1.0f, attackCooldown = 1.0f, stuckDuration = 2f;
    float timeStateChange = 0f, timeStateEnd = 0f;

    [SerializeField, Range(-90f, 90f)]
    float attackStartAngle = -45f, attackFinishAngle = 45f;
    [SerializeField, Range(1f, 360f), Tooltip("In degrees per second")]
    float attackSpeed = 15f;


    [SerializeField]
    GameObject healthUIHolder;
    [SerializeField]
    Image healthUI;

    bool active = false;

    enum State
    {
        DAMAGED,
        LOOK_AT_PLAYER,
        WINDUP,
        SWING,
        STUCK,
        END_PHASE
    }

    State _state;

    // Start is called before the first frame update
    void Start()
    {
        phaseOneCutscene.OnCutsceneComplete += StartPhaseOne;
        onDamage += OnDamaged;
        bossAttackTrigger.OnTriggerEntered += DetectAttack;
        active = false;
    }

    void StartPhaseOne(CutsceneObject o)
    {
        active = true;
        UpdateState(State.LOOK_AT_PLAYER);
    }

    void OnDamaged(int damage)
    {
        if (health <= 0)
        {
            UpdateState(State.END_PHASE);
        }
        else
        {
            UpdateState(State.DAMAGED);
        }
    }

    void DetectAttack(Collider other)
    {
        if (other != null && other.gameObject != null)
        {
            if (other.gameObject.CompareTag("CherryBomb"))
            {
                Destroy(other);
            }
            else if (other.gameObject.CompareTag("Player"))
            {
                print("Damaged Player!");
                player.GetComponent<Health>().Damage(0, bossOrientation.up * 5f);
            }
        }

    }

    void UpdateState(State state)
    {
        _state = state;
        timeStateChange = Time.time;
        switch (state)
        {
            case State.DAMAGED:
                timeStateEnd = damageStunDuration;
                print("Boss damaged!");
                break;
            case State.LOOK_AT_PLAYER:
                timeStateEnd = attackCooldown;
                print("Looking at player!");
                break;
            case State.WINDUP:
                timeStateEnd = windupDuration;
                print("Boss windup!");
                break;
            case State.SWING:
                timeStateEnd = (attackFinishAngle - attackStartAngle) / attackSpeed;
                print("Boss attack!");
                break;
            case State.STUCK:
                timeStateEnd = stuckDuration;
                print("Boss attack got stuck!");
                break;
            case State.END_PHASE:
                print("Boss end phase 1!");
                break;
        }
    }

    void Update()
    {
        if (active)
        {
            switch (_state)
            {
                case State.DAMAGED:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.LOOK_AT_PLAYER);
                    }
                    break;
                case State.LOOK_AT_PLAYER:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.WINDUP);
                    }
                    else
                    {
                        Vector3 lookDir = Vector3.ProjectOnPlane((player.position - bossOrientation.position), bossOrientation.up).normalized;
                        bossOrientation.rotation = Quaternion.LookRotation(lookDir, bossOrientation.up);
                    }
                    break;
                case State.WINDUP:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.SWING);
                    }
                    break;
                case State.SWING:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.LOOK_AT_PLAYER);
                    }
                    break;
                case State.STUCK:
                    break;
                case State.END_PHASE:
                    break;
            }
        }
    }
}
