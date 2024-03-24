using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JuiceProjectile : MonoBehaviour
{
    private BlenderBoss blenderBoss;
    public Vector3 defaultScale;
    //    public float flatKnockback;

    private void OnEnable()
    {
        defaultScale = new Vector3(0.6f, 1, 1);
        blenderBoss = GameObject.FindWithTag("Boss").GetComponent<BlenderBoss>(); 
        StartCoroutine(JuiceBlast());
    }

    IEnumerator JuiceBlast()
    {
        transform.localScale = defaultScale;
        transform.localPosition = new Vector3(0, 14.1800003f, -6);

        Vector3 targetScale = Vector3.zero; // Target scale is Vector3.zero for complete disappearance
        Vector3 initialScale = transform.localScale;
        Vector3 initialPosition = transform.position;
        float blastDuration = 5.0f; // Adjust duration of blast as needed
        float elapsedTime = 0f;

        while (elapsedTime < blastDuration && !blenderBoss.doneMoving)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / blastDuration);

            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            transform.localScale = new Vector3(defaultScale.x, transform.localScale.y, defaultScale.z);
            yield return null;
        }

        // Ensure final state
        transform.localScale = targetScale;
        transform.position = initialPosition - new Vector3(0, initialScale.y, 0);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<Health>().Damage(1, ((transform.position - other.transform.position).normalized + Vector3.back + Vector3.up));
        }
        else if (other.name.Contains("Body") && other.CompareTag("Enemy") && other.gameObject.GetComponentInParent<LassoableEnemy>() != null)
        {
            other.gameObject.GetComponentInParent<LassoableEnemy>().TossInDirection(Vector3.zero, Vector3.up, LassoTossable.TossStrength.WEAK);
        }
    }
}
