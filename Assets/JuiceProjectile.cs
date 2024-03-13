using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JuiceProjectile : MonoBehaviour
{
    public BlenderBoss blenderBoss;
    public Vector3 defaultScale;
    public float flatKnockback;

    private void OnEnable()
    {
        defaultScale = new Vector3(0.5f, 1, 7.65f);
        blenderBoss = GameObject.Find("BlenderBoss").GetComponent<BlenderBoss>();
        StartCoroutine(JuiceBlast());
    }

    IEnumerator JuiceBlast()
    {
        transform.localScale = defaultScale;
        while (!blenderBoss.doneMoving && transform.localScale.y > 0)
        {
            print("SHRINKING");
            yield return new WaitForSeconds(0.5f);
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y - 0.2f, transform.localScale.z);
            transform.position -= new Vector3(0, transform.localScale.y - 0.1f, 0);
            if (transform.localScale.y <= 0)
            {
                transform.localScale = defaultScale;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<Health>().Damage(1, flatKnockback * ((transform.position - other.transform.position).normalized + Vector3.back + Vector3.up));
        }
    }
}
