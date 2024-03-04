using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(GravityObject))]
public class OrangeEnemyController : EnemyController
{
    enum OrangeState
    {
        IDLE,
        ROAM,
        PLAYER_SPOTTED,
        REV_UP,
        CHARGE,
        SLOW_DOWN,
        DIZZY,
        HELD,
        THROWN
    }

    public enum OrangeSubStates
    {
        WALK,
        TALK, 
        SLEEP,
        SWAY
    }

    private OrangeState _state;
    public OrangeSubStates subState;
    public GameObject[] destinationPoints = null;

    [SerializeField]
    Transform _model = null;

    public GameObject body;

    public float knockbackForce;
    public float chargeSpeed;
    public float maxChargeDistance = 70.0f;
    public float walkSpeed = 10.0f;

    public float timeToBeginRev = 0.4f;
    public float timeToBeginCharge = 0.3f;
    public float timeSpentCharging = 0.8f;
/*    public float timeSpentWalking;
    public float walkTimer;*/
    public float dizzyTime = 1.0f;

    private float spottedParam = 0.0f;

    public float timeSpentWalking;
    public float walkTimer;
    private float _distanceFromPartner = 11f;

    private Transform _spottedPlayerTransform = null;
    private GravityObject _gravObject = null;
    private LassoableEnemy _lassoComp = null;
    private Vector3 _chargeStartPoint;
    private Vector3 _chargeDirection;
    private Vector3 _chargeTargetPoint;

    public bool playerInView = false;

    public Animator orangeEnemyAnimator;
    // Start is called before the first frame update
    void Start()
    {
        _lassoComp = GetComponent<LassoableEnemy>();
        _lassoComp.isLassoable = false;
        _gravObject = GetComponent<GravityObject>();
        if (destinationPoints.Length > 1)
        {
            destinationPoint = destinationPoints[0];
        }
        else
        {
            subState = OrangeSubStates.SLEEP;
        }
        UpdateAnimState();
        _distanceFromPartner = Mathf.Pow(_distanceFromPartner, 2);
    }

    // TODO: Changed from Update. Make sure things still work
    void FixedUpdate()
    {
        if (_lassoComp.currentlyLassoed) { UpdateState(OrangeState.HELD); }
        if (_state == OrangeState.HELD)
        {
            if (!_lassoComp.currentlyLassoed)
            {
                UpdateState(OrangeState.THROWN);
            }
            return;
        }

        if ((_state == OrangeState.PLAYER_SPOTTED || _state == OrangeState.REV_UP) && _spottedPlayerTransform == null)
        {
            UpdateState(OrangeState.IDLE);
        }
        switch (_state)
        {
            case OrangeState.PLAYER_SPOTTED:
                spottedParam += Time.deltaTime;
                _chargeDirection = (_spottedPlayerTransform.position - transform.position).normalized;
                if (_model != null)
                {
                    _model.rotation = Quaternion.Slerp(_model.rotation,
                        Quaternion.LookRotation(_chargeDirection, _gravObject.characterOrientation.up),
                        spottedParam);
                }
                break;
            case OrangeState.REV_UP:
                _chargeDirection = (_spottedPlayerTransform.position - transform.position).normalized;
                if (_model != null)
                {
                    _model.rotation = Quaternion.LookRotation(_chargeDirection, _gravObject.characterOrientation.up);
                }
                break;
            case OrangeState.CHARGE:
                if (_gravObject.GetMoveVelocity().magnitude < chargeSpeed)
                {
                    GetComponent<Rigidbody>().AddForce(_chargeDirection * chargeSpeed);
                }
                break;
            case OrangeState.IDLE:
                PlayDifferentIdle();
                break;
            default:

                break;
        }
    }

    void UpdateState(OrangeState newState)
    {
        if (_state == OrangeState.HELD && newState != OrangeState.THROWN) { return; }
        if (_state != newState)
        {
            _state = newState;
            UpdateAnimState();

            switch (newState)
            {
                case OrangeState.PLAYER_SPOTTED:
                    spottedParam = 0.0f;
                    GetComponent<Rigidbody>().velocity = Vector3.zero + _gravObject.GetFallingVelocity();
                    Invoke("EndPlayerSpotted", timeToBeginRev);
                    break;
                case OrangeState.REV_UP:
                    _chargeStartPoint = transform.position;
                    SoundManager.Instance().PlaySFX("OrangeRevUp");
                    Invoke("EndRevUp", timeToBeginCharge);
                    break;
                case OrangeState.CHARGE:
                    SoundManager.Instance().StopSFX("OrangeRevUp");
                    SoundManager.Instance().PlaySFX("OrangeCharge");
                    Invoke("EndCharge", timeSpentCharging);
                    break;
                case OrangeState.DIZZY:
                    GetComponent<Rigidbody>().velocity = Vector3.zero + _gravObject.GetFallingVelocity();
                    SoundManager.Instance().PlaySFX("OrangeDizzy");
                    GetComponentInChildren<ParticleSystem>().Play();
                    Invoke("EndDizzy", dizzyTime);
                    break;
                case OrangeState.IDLE:
                    GetComponent<Rigidbody>().velocity = Vector3.zero;
                    break;
                case OrangeState.THROWN:
                    break;
                case OrangeState.HELD:
                    SoundManager.Instance().StopSFX("OrangeDizzy");
                    break;
            }
            if (newState == OrangeState.DIZZY || newState == OrangeState.HELD)
            {
                //print("Lassoable");
                _lassoComp.isLassoable = true;
            }
            else
            {
                //print("Not lassoable");
                _lassoComp.isLassoable = false;
            }
        }
    }
    public GameObject destinationPoint;
    Vector3 _directionOfPartner;
    public GameObject partner;
    void PlayDifferentIdle()
    {
        switch (subState)
        {
            case OrangeSubStates.WALK:
                walkTimer += Time.deltaTime;
                if (walkTimer >= timeSpentWalking)
                {
                    EndWalk();
                    walkTimer = 0;
                }
                _directionOfPartner = (destinationPoint.transform.position - transform.position).normalized;
                _directionOfPartner.y = 0;
                _model.rotation = Quaternion.Slerp(_model.rotation,
                                Quaternion.LookRotation(_directionOfPartner, _gravObject.characterOrientation.up),
                                spottedParam);
                spottedParam += Time.deltaTime;
                if (_gravObject.GetMoveVelocity().magnitude < chargeSpeed)
                {
                    GetComponent<Rigidbody>().AddForce(_directionOfPartner * 4);
                }
                
                break;
            case OrangeSubStates.TALK:
                if (partner != null)
                {
                    _directionOfPartner = (partner.transform.position - transform.position).normalized;

                    _model.rotation = Quaternion.Slerp(_model.rotation,
                                    Quaternion.LookRotation(_directionOfPartner, _gravObject.characterOrientation.up),
                                    spottedParam);
                    spottedParam += Time.deltaTime;
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
                case OrangeSubStates.WALK:
                    orangeEnemyAnimator.Play("Base Layer.OE_Move_Start");
                    break;
                case OrangeSubStates.TALK:
                    if (partner != null) {
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
        if (_state == OrangeState.THROWN)
        {
            orangeEnemyAnimator.Play("Base Layer.OE_Thrown");
        }
    }


    void EndPlayerSpotted()
    {
        if (_state != OrangeState.PLAYER_SPOTTED) { return; }
        UpdateState(OrangeState.REV_UP);
    }

    void EndRevUp()
    {
        if (_state != OrangeState.REV_UP) { return; }
        UpdateState(OrangeState.CHARGE);
    }

    void EndCharge()
    {
        if (_state != OrangeState.CHARGE) { return; }
        //TODO: TEST MORE
        if (playerInView)
        {
            UpdateState(OrangeState.PLAYER_SPOTTED);
        } 
        else
        {
            UpdateState(OrangeState.IDLE);
        }
        //UpdateState(OrangeState.DIZZY);
    }

    int rand;
    void EndWalk()
    {
        rand = UnityEngine.Random.Range(0,destinationPoints.Length);
        destinationPoint = destinationPoints[rand];
        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    void EndDizzy()
    {
        if (_state != OrangeState.DIZZY) { return; }

        SoundManager.Instance().StopSFX("OrangeDizzy");
        if (playerInView)
        {
            UpdateState(OrangeState.PLAYER_SPOTTED);
        }
        else
        {
            _spottedPlayerTransform = null;
            UpdateState(OrangeState.IDLE);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject != null)
        {
            if (collision.gameObject.GetComponent<Obstacle>() != null && _state != OrangeState.THROWN && _state != OrangeState.REV_UP && _state == OrangeState.CHARGE)
            {
                collision.gameObject.GetComponent<Obstacle>().Hit();
                SoundManager.Instance().StopSFX("OrangeCharge");
                SoundManager.Instance().PlaySFX("OrangeHitWall");
                UpdateState(OrangeState.DIZZY);
            }
/*            else if (collision.gameObject.GetComponent<Obstacle>() != null && _state == OrangeState.DIZZY)
            {
                StartCoroutine("ChangeCollisionInteraction", collision.collider);
                //GetComponent<Rigidbody>().AddForce((transform.position - collision.transform.position).normalized * 2, ForceMode.Impulse);
            }*/
        }
    }

    // Needed to make character not stick to walls. TODO: Test to see if it breaks things.

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.GetComponent<Obstacle>() != null && _state == OrangeState.IDLE)
        {
            StartCoroutine("ChangeCollisionInteraction", collision.collider);
        }
        else if (collision.gameObject.tag == "Player" && _state == OrangeState.CHARGE)
        {
            collision.gameObject.GetComponentInParent<Health>().Damage(1, knockbackForce * ((transform.position - collision.gameObject.transform.position).normalized + Vector3.up));
            StartCoroutine("ChangeCollisionInteraction", collision.collider);
        }
    }
    private IEnumerator ChangeCollisionInteraction(Collider collider)
    {
        Physics.IgnoreCollision(body.GetComponent<Collider>(), collider, true);
        yield return new WaitForSeconds(0.5f);
        Physics.IgnoreCollision(body.GetComponent<Collider>(), collider, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null)
        {
            if (other.gameObject.tag == "Player")
            {
                _spottedPlayerTransform = other.gameObject.transform;
                playerInView = true;
                // was a bug where when a player jumps while enemy is charging (escapes the triggerbox), then enters again
                // if the player gets hit, would not take damage.
                if (_state == OrangeState.IDLE)
                {
                    UpdateState(OrangeState.PLAYER_SPOTTED);
                }
            }
        }
    }

    Vector3 distance;
    float sqrDistance;
    private void OnTriggerStay(Collider other)
    {
        if (partner == null && other.gameObject.CompareTag("Enemy"))
        {
            distance = other.transform.position - transform.position;
            sqrDistance = distance.sqrMagnitude;

            if (sqrDistance <= _distanceFromPartner)
            {
                partner = other.gameObject;
                UpdateAnimState();
            }
        }
        else if (partner != null && other.gameObject == partner)
        {
            distance = other.transform.position - transform.position;
            sqrDistance = distance.sqrMagnitude;

            if (sqrDistance > _distanceFromPartner)
            {
                partner = null;
                UpdateAnimState();
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject != null)
        {
            if (other.gameObject.tag == "Player")
            {
                playerInView = false;
            }
/*            else if (other.gameObject.tag == "Enemy")
            {
                partner = null;
            }*/
        }
    }
}
