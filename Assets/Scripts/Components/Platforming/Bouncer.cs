using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [SerializeField]
    float bounceForce = 20f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject != null && collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            collision.gameObject.GetComponent<Rigidbody>().AddForce(transform.up * bounceForce, ForceMode.Impulse);
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().PlaySFX("BouncePlatform");
            }
        }
    }
}
