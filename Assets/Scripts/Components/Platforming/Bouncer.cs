using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [SerializeField]
    float bounceForce = 20f, maxVerticalVel = 60f;
    public Animator jelloAnimator;

    [SerializeField]
    SoundPlayer sfx;



    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject != null && collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            
            if (collision.gameObject.GetComponent<GravityObject>() != null)
            {

                GravityObject grav = collision.gameObject.GetComponent<GravityObject>();
                //print("Falling vel is : " + grav.GetFallingVelocity() + " With mag " + grav.GetFallingVelocity().magnitude);
                if (grav.GetFallingVelocity().magnitude < maxVerticalVel 
                    || Vector3.Dot(grav.GetFallingVelocity(), transform.up) < 0f)
                {
                    ApplyBounce(collision.gameObject.GetComponent<Rigidbody>());
                }
            }
            else
            {
                // Just gonna shoot into space I guess, lol
                ApplyBounce(collision.gameObject.GetComponent<Rigidbody>());
            }
        }
    }

    void ApplyBounce(Rigidbody rb)
    {
        rb.AddForce(transform.up * bounceForce, ForceMode.Impulse);
        if (sfx != null)
        {
            sfx.PlaySFX("Bounce");
        }

        if (jelloAnimator != null)
        {
            jelloAnimator.Play("J_Jiggle");
        }
    }
}
