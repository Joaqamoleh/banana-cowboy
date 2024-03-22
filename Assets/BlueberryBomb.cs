using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueberryBomb : MonoBehaviour
{
    public float speed;
    public int pos; // What position in indicatorSpawnObject in BlenderBoss.cs
    private BlenderBoss blenderBoss;
    public GameObject deathJuiceEffect;

    private void Start()
    {
        blenderBoss = GameObject.FindWithTag("Boss").GetComponent<BlenderBoss>();
    }

    private void Update()
    {
        transform.Translate(speed * Time.deltaTime * Vector3.down);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<Health>().Damage(1, ((transform.position - other.transform.position).normalized + Vector3.back + Vector3.up));
            HitSomething();

        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Create a splat effect
            blenderBoss.CreateSplat(pos);
            HitSomething();        
        }
    }

    void HitSomething()
    {
        SoundManager.Instance().PlaySFX("CherryBombExplode");
        Instantiate(deathJuiceEffect, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
