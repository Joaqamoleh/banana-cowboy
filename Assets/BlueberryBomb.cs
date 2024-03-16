using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueberryBomb : MonoBehaviour
{
    public float speed;
    public int pos; // What position in indicatorSpawnObject in BlenderBoss.cs
    public BlenderBoss blenderBoss;
    private void Start()
    {
        blenderBoss = GameObject.Find("BlenderBoss").GetComponent<BlenderBoss>();
    }

    private void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<Health>().Damage(1, 1 * ((transform.position - other.transform.position).normalized + Vector3.back + Vector3.up));
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            // Create a splat effect
            blenderBoss.CreateSplat(pos);
        }
    }
}
