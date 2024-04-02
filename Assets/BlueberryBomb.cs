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

    private void Start()
    {
        blenderBoss = GameObject.FindWithTag("Boss")?.GetComponent<BlenderBoss>();
    }

    private void Update()
    {
        transform.Translate(speed * Time.deltaTime * Vector3.down);
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
            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
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
        SoundManager.Instance().PlaySFX("CherryBombExplode");
        hitObject = true;
        Instantiate(deathJuiceEffect, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
