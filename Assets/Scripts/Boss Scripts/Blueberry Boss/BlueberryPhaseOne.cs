using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BlueberryPhaseOne : BossController
{
    [SerializeField]
    Renderer[] bossRenderers;
    [SerializeField]
    Material[] bossDamageMats;

    [SerializeField]
    Animator bossAnimator;

    [SerializeField]
    AnimationEventHandler animationHandler;

    [SerializeField]
    DetectionTriggerHandler bossAttackTrigger;

    [SerializeField]
    int numBreakablePoles = 3;
    List<BreakablePole> poles = new List<BreakablePole>();
    

    [SerializeField]
    CutsceneObject phaseOneCutscene, phaseTwoCutscene, finalCutscene;

    [SerializeField]
    Transform player, bossOrientation, bossRoot, swordTip, swordRoot;

    [SerializeField, Min(0f)]
    float damageStunDuration = 0.5f, windupDuration = 1.0f, attackCooldown = 1.0f, stuckDuration = 2f;
    float timeStateChange = 0f, timeStateEnd = 0f;

    [Header("Phase 1")]
    [SerializeField]
    GameObject[] disableOnEnd;
    [SerializeField]
    GameObject[] enableOnEnd;
    [SerializeField]
    BreakableCannon cannonPhaseOne;

    [Header("Phase 2")]
    [SerializeField]
    GameObject[] enableOnBegin;
    [SerializeField]
    BreakableCannon cannonPhaseTwo;



    [SerializeField]
    GameObject healthUIHolder;
    [SerializeField]
    Image healthUI;

    bool active = false, inPhaseTwo = false;

    [Header("Debug")]
    [SerializeField]
    GameObject debugAttack;

    SoundPlayer soundPlayer;

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
        phaseTwoCutscene.OnCutsceneComplete += StartPhaseTwo;
        CutsceneManager.Instance().GetCutsceneByName("Phase 1 End").OnCutsceneComplete += MakeEndPhaseState;
        finalCutscene.OnCutsceneComplete += EndLevel;

        animationHandler.OnSwordAnimChange += UpdateSwordAttackTriggerActive;

        onDamage += OnDamaged;
        bossAttackTrigger.OnTriggerEntered += DetectAttack;
        active = false;
        if (debugAttack != null)
        {
            debugAttack.SetActive(false);
        }

        soundPlayer = GetComponent<SoundPlayer>();
    }

    void UpdateSwordAttackTriggerActive(bool active)
    {
        bossAttackTrigger.gameObject.SetActive(active);
    }

    void MakeEndPhaseState(CutsceneObject o)
    {
        UpdateState(State.END_PHASE);
    }

    void StartPhaseOne(CutsceneObject o)
    {
        GetComponent<CannonController>().ShouldFireBombs(false);
        inPhaseTwo = false;
        health = 3;
        active = true;
        UpdateState(State.LOOK_AT_PLAYER);
    }

    void StartPhaseTwo(CutsceneObject o)
    {
        GetComponent<CannonController>().ShouldFireBombs(false);
        inPhaseTwo = true;
        health = 3;
        active = true;
        UpdateState(State.LOOK_AT_PLAYER);
        Invoke("EnablePhaseTwo", 0.1f);
    }

    void EndLevel(CutsceneObject o)
    {
        LevelManager.SetLevelUnlock("Blender Boss Room", true);
        LevelSwitch.ChangeScene("Level Select");
    }

    void EnablePhaseTwo()
    {
        foreach (var o in enableOnBegin)
        {
            o.SetActive(true);
        }
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
                print("Colliding attack with " + other.name);
                if (!p.IsBroken())
                {
                    p.BreakPole();
                    poles.Add(p);
                    UpdateState(State.STUCK);
                }
            } 
            else if (other.GetComponent<BreakableCannon>() != null)
            {
                BreakableCannon c = other.GetComponent<BreakableCannon>();
                if (!c.IsBroken())
                {
                    c.BreakCannon();
                    UpdateState(State.STUCK);
                }

            }
            else if (other.gameObject.CompareTag("Player"))
            {
                print("Damaged Player!");
                player.GetComponent<Health>().Damage(1, bossOrientation.up * 30f);
            }
        }

    }

    void UpdateState(State state)
    {
        State prev = _state;
        _state = state;
        timeStateChange = Time.time;
        switch (state)
        {
            case State.DAMAGED:
                timeStateEnd = damageStunDuration;
                print("Boss damaged!");
                break;
            case State.LOOK_AT_PLAYER:
                if (prev == State.DAMAGED || prev == State.STUCK)
                {
                    bossAnimator.Play("BB_Dizzy_Reset");
                } 
                else
                {
                    bossAnimator.Play("BB_Idle");

                }
                bossAnimator.speed = 1.0f;
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
                bossAnimator.Play("BB_Sword_Windup");
                bossAnimator.speed = 1.0f;
                timeStateEnd = windupDuration;
                soundPlayer.PlaySFX("Swing");
                print("Boss windup!");
                break;
            case State.ATTACK:
                bossAnimator.Play("BB_Sword_Attack_Success");
                bossAnimator.speed = 0.7f;
                timeStateEnd = bossAnimator.GetCurrentAnimatorStateInfo(0).length + 0.1f;
                print("Boss attack!");
                break;
            case State.STUCK:
                bossAnimator.Play("BB_Dizzy_Fall");
                bossAnimator.speed = 1.0f;
                timeStateEnd = stuckDuration;
                print("Boss attack got stuck!");
                break;
            case State.END_PHASE:
                bossOrientation.rotation = bossRoot.rotation;
                bossAnimator.Play("BB_Idle");
                bossAnimator.speed = 1.0f;
                if (inPhaseTwo)
                {
                    print("Boss end phase 2!");
                    CutsceneManager.Instance().PlayCutsceneByName("Phase 2 End");
                }
                else
                {
                    print("Boss end phase 1!");
                    CutsceneManager.Instance().PlayCutsceneByName("Phase 1 End");
                    CutsceneManager.Instance().GetCutsceneByName("Phase 1 End").OnCutsceneComplete += OnEndFightCutscene;
                }
                break;
        }
        if (debugAttack != null)
        {
            RepositionSwordAttackTrigger();
            debugAttack.SetActive(state == State.ATTACK || state == State.WINDUP);
        }
    }

    void OnEndFightCutscene(CutsceneObject obj)
    {
        GetComponent<CannonController>().ShouldFireBombs(true);
        if (cannonPhaseOne != null && !cannonPhaseOne.IsBroken())
        {
            cannonPhaseOne.BreakCannon();
        }
        if (cannonPhaseTwo != null)
        {
            cannonPhaseTwo.gameObject.SetActive(true);
        }
    }

    void RepositionSwordAttackTrigger()
    {
        Vector3 swordDir = Vector3.ProjectOnPlane((swordTip.position - swordRoot.position).normalized, bossOrientation.up);
        bossAttackTrigger.transform.position = swordRoot.position;
        bossAttackTrigger.transform.rotation = Quaternion.LookRotation(swordDir, bossOrientation.up);
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
                    else
                    {
                        RepositionSwordAttackTrigger();
                    }
                    break;
                case State.ATTACK:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.LOOK_AT_PLAYER);
                    }
                    else
                    {
                        RepositionSwordAttackTrigger();
                    }
                    break;
                case State.STUCK:
                    if (Time.time - timeStateChange > timeStateEnd)
                    {
                        UpdateState(State.LOOK_AT_PLAYER);
                    }
                    break;
                case State.END_PHASE:
                    foreach (var o in disableOnEnd)
                    {
                        o.SetActive(false);
                    }
                    foreach (var o in enableOnEnd)
                    {
                        o.SetActive(true);
                    }
                    active = false;
                    break;
            }
        }
    }
}
