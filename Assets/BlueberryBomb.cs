using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueberryBomb : MonoBehaviour
{
    public float speed;
    public int pos; // What position in indicatorSpawnObject in BlenderBoss.cs
    private BlenderBoss blenderBoss;
    public GameObject deathJuiceEffect;
    public bool hitObject = false;

    public delegate void OnExplode();
    public OnExplode onExplode;
    SoundPlayer soundPlayer;

    private void Start()
    {
        blenderBoss = GameObject.FindWithTag("Boss")?.GetComponent<BlenderBoss>();
        soundPlayer = GetComponent<SoundPlayer>();
    }

    private void Update()
    {
        transform.Translate(speed * Time.deltaTime * -transform.up, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitObject)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponentInParent<Health>().Damage(1, ((transform.position - other.transform.position).normalized + Vector3.back + Vector3.up));
                HitSomething();

            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Default"))
            {
                // Create a splat effect
                if (blenderBoss != null)
                {
                    blenderBoss.CreateSplat(pos);
                }
                HitSomething();
            }
        }
    }

    void HitSomething()
    {
        ScreenShakeManager.Instance.ShakeCamera(3, 2, 1);
        hitObject = true;
        Instantiate(deathJuiceEffect, transform.position, transform.rotation);
        soundPlayer.PlaySFX("Explode");
        Destroy(GetComponent<Collider>());
        Destroy(GetComponent<Renderer>());
        StartCoroutine(KillMe());
    }

    IEnumerator KillMe()
    {
        onExplode?.Invoke();
        yield return new WaitForSeconds(soundPlayer.GetSFX("Explode").audioClip.length + 0.1f);
        Destroy(gameObject);
    }
}
