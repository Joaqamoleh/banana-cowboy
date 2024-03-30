using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField]
    int damage = 0;

    [SerializeField]
    float knockbackForce = 10f;

    [SerializeField]
    GameObject hitParticleEffect, missParticleEffect;

    GravityObject gravObj;
    Rigidbody rb;

    bool fired = false;

    // Start is called before the first frame update
    void Start()
    {
        gravObj = GetComponent<GravityObject>();
        if (gravObj != null)
        {
            gravObj.disabled = true;
        }
        rb = GetComponent<Rigidbody>();
        Debug.Assert(rb != null);
        rb.isKinematic = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    public void FireProjectile(Vector3 dir, Vector3 targetPoint)
    {
        fired = true;
        rb.isKinematic = false;
        if (gravObj != null)
        {
            gravObj.disabled = false;
        }
        rb.AddForce(dir * 50f, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!fired) { return; }

        if (collision != null && collision.collider != null && collision.collider.CompareTag("Player")) 
        {
            Vector3 knockbackDir = (collision.collider.transform.position - transform.position).normalized;
            if (gravObj != null)
            {
                knockbackDir = Vector3.ProjectOnPlane(knockbackDir, gravObj.characterOrientation.up).normalized;
            }
            collision.collider.transform.parent.GetComponentInParent<Health>().Damage(0, knockbackDir * knockbackForce);
            if (hitParticleEffect != null)
            {
                Instantiate(hitParticleEffect, transform.position, transform.rotation);
            }
        }
        if (missParticleEffect != null)
        {
            Instantiate(missParticleEffect, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }
}
