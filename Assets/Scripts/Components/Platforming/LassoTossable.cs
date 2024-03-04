using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LassoTossable : LassoObject
{

    [SerializeField]
    float swingHeight = 3f, swingRadius = 4f, swingSpeed = 1f, tossForwardForce = 25f, tossUpForce = 10f, tossWeakMult = 0.3f, tossMedMult = 0.7f, tossStrongMult = 1.0f;
    // Swing Height is the height above the lassoHandPos which the swing happens
    // The others are pretty obvious
    [SerializeField]
    bool oneTimeThrow = true;


    private Rigidbody _rigidbody;
    bool tossed = false;
    private Vector3 _startingPullPos = Vector3.zero, _swingVel = Vector3.zero;

    public delegate void LassoTossEvent(LassoTossable tossedObject, Vector3 tossDirection, Vector3 tossRelativeUp, TossStrength strength);
    public event LassoTossEvent OnTossableTossed;
    public enum TossStrength
    {
        WEAK,
        MEDIUM,
        STRONG
    }

    private void Start()
    {
        base.Start();
        _rigidbody = GetComponent<Rigidbody>();
        Debug.Assert(_rigidbody != null);
    }

    /// <summary>
    /// Rigidly change the position of the lassoTossable towards the target position
    /// based on the given param t [0, 1]
    /// </summary>
    public void PullLassoTossableTowards(Vector3 targetPos, float t)
    {
        if (t == 0)
        {
            // Set initial position of this object for lerping.
            _startingPullPos = transform.position;
            _rigidbody.isKinematic = true;
        }

        transform.position = Vector3.SmoothDamp(transform.position, Vector3.Lerp(_startingPullPos, targetPos, t), ref _swingVel, 0.1f);
    }

    /// <summary>
    /// Rigidly move the lasso tossable above the lassoHandPos by some set height, radius, and speed
    /// </summary>
    public void SwingLassoTossableAround(Transform lassoHandPos, float t)
    {
        if (t == 0)
        {
            _rigidbody.isKinematic = true;
        }

        transform.position = Vector3.SmoothDamp(transform.position, 
            lassoHandPos.position 
            + lassoHandPos.up * swingHeight
            + lassoHandPos.right * swingRadius * Mathf.Sin(t * swingSpeed)
            + lassoHandPos.forward * swingRadius * Mathf.Cos(t * swingSpeed), ref _swingVel, 0.1f);
    }
    
    public void TossInDirection(Vector3 dir, Vector3 relativeUp, TossStrength strength)
    {
        if ((oneTimeThrow && !tossed) || !oneTimeThrow)
        {
            _rigidbody.isKinematic = false;
            float mult = 0.0f;
            switch (strength)
            {
                case TossStrength.WEAK:
                    mult = tossWeakMult;
                    break;
                case TossStrength.MEDIUM:
                    mult = tossMedMult;
                    break;
                case TossStrength.STRONG:
                    mult = tossStrongMult;
                    break;
            }
            _rigidbody.AddForce(dir * tossForwardForce + relativeUp * tossUpForce, ForceMode.Impulse);
            tossed = true;
            isLassoable = false;
            currentlyLassoed = false;
            OnTossableTossed?.Invoke(this, dir, relativeUp, strength);
        }
    }

    public float GetSwingHeight()
    {
        return swingHeight;
    }

    public float GetSwingRadius()
    {
        return swingRadius;
    }

    public bool IsTossed() { 
        return tossed; 
    }
}
