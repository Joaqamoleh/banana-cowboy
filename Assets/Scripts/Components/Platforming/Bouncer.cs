using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bouncer : MonoBehaviour
{
    [SerializeField]
    float bounceForce = 20f;
    public Animator jelloAnimator;

    [SerializeField]
    Sound sfx;
    [SerializeField]
    float soundDistance = 40f;

    public void Start()
    {
        sfx.src = gameObject.AddComponent<AudioSource>();
        sfx.src.clip = sfx.audioClip;
        sfx.src.volume = sfx.volume * SoundManager.Instance().SFXVolume;
        sfx.src.pitch = sfx.pitch;
        sfx.src.maxDistance = soundDistance;
        sfx.src.spatialBlend = 1.0f;
        sfx.type = Sound.Type.SFX;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject != null && collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            collision.gameObject.GetComponent<Rigidbody>().AddForce(transform.up * bounceForce, ForceMode.Impulse);
            if (sfx != null && sfx.src != null)
            {
                 sfx.src.Play();
            }

            if (jelloAnimator != null)
            {
                jelloAnimator.Play("J_Jiggle");
            }
        }
    }
}
