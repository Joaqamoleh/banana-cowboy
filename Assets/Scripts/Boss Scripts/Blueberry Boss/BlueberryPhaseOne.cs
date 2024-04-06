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
    int numBreakablePoles = 3;
    List<BreakablePole> poles = new List<BreakablePole>();
    

    [SerializeField]
    CutsceneObject phaseOneCutscene;

    [SerializeField]
    Transform player, bossOrientation, bossRoot;

    [SerializeField, Min(0f)]
    float damageStunDuration = 0.5f, windupDuration = 1.0f, attackCooldown = 1.0f, stuckDuration = 2f;
    float timeStateChange = 0f, timeStateEnd = 0f;

    [SerializeField, Range(-90f, 90f)]
    float attackStartAngle = -45f, attackFinishAngle = 45f;
    [SerializeField, Range(1f, 360f), Tooltip("In degrees per second")]
    float attackSpeed = 15f;
    float attackAngle = 0f;


    [SerializeField]
    GameObject[] disableOnEnd, enableOnEnd;

    [SerializeField]
    GameObject healthUIHolder;
    [SerializeField]
    Image healthUI;

    bool active = false;

    [Header("Debug")]
    [SerializeField]
    GameObject debugAttack;

    enum State
    {
        DAMAGED,
        LOOK_AT_PLAYER,
        WINDUP,
        ATTACK,
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
        if (debugAttack != null)
        {
            debugAttack.SetActive(false);
        }
    }

    void StartPhaseOne(CutsceneObject o)
    {
        active = true;
        UpdateState(State.LOOK_AT_PLAYER);
    }

    void OnDamaged(int damage)
    {
        if (_state == State.STUCK)
        {
            health -= damage;
            if (health <= 0)
            {
                UpdateState(State.END_PHASE);
            }
            else
            {
                UpdateState(State.DAMAGED);
            }
        }
    }

    void DetectAttack(Collider other)
    {
        if (other != null && other.gameObject != null && _state == State.ATTACK)
        {
            if (other.transform.parent != null && other.transform.parent.GetComponentInParent<BreakablePole>() != null)
            {
                BreakablePole p = other.transform.parent.GetComponentInParent<BreakablePole>();
                p.BreakPole();
                poles.Add(p);
                UpdateState(State.STUCK);
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
                if (numBreakablePoles == poles.Count)
                {
                    foreach (var pole in poles)
                    {
                        pole.RespawnPole();
                    }
                    poles.Clear();
                }
                print("Looking at player!");
                break;
            case State.WINDUP:
                timeStateEnd = windupDuration;
                attackAngle = attackStartAngle;
                print("Boss windup!");
                break;
            case State.ATTACK:
                attackAngle = attackStartAngle;
                print("Boss attack!");
                break;
            case State.STUCK:
                timeStateEnd = stuckDuration;
                print("Boss attack got stuck!");
                break;
            case State.END_PHASE:
                print("Boss end phase 1!");
                bossOrientation.rotation = bossRoot.rotation;
                foreach (var o in disableOnEnd)
                {
                    o.SetActive(false);
                }
                foreach (var o in enableOnEnd)
                {
                    o.SetActive(true);
                }
                CutsceneManager.Instance().PlayCutsceneByName("Phase 1 End");
                break;
        }
        if (debugAttack != null)
        {
            bossAttackTrigger.transform.rotation = bossOrientation.rotation * Quaternion.Euler(0f, attackAngle, 0f);
            debugAttack.SetActive(state == State.ATTACK || state == State.WINDUP || state == State.STUCK);
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
                        Vector3 lookDir = Vector3.ProjectOnPlane((player.position - bossOrientation.position), bossRoot.up).normalized;
                        bossOrientation.rotation = Quaternion.LookRotation(lookDir, bossRoot.up);
                    }
                    break;
                case State.WINDUP:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.ATTACK);
                    }
                    break;
                case State.ATTACK:
                    if (attackAngle >= attackFinishAngle)
                    {
                        UpdateState(State.LOOK_AT_PLAYER);
                    }
                    else
                    {
                        bossAttackTrigger.transform.rotation = bossOrientation.rotation * Quaternion.Euler(0f, attackAngle, 0f);
                        attackAngle += attackSpeed * Time.deltaTime;
                    }
                    break;
                case State.STUCK:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.LOOK_AT_PLAYER);
                    }
                    break;
            }
        }
    }
}
