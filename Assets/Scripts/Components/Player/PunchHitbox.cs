using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    [HideInInspector]
    public float lifetime;
    float spawnTime;

    // Start is called before the first frame update
    void Start()
    {
        spawnTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Time.time - spawnTime > lifetime) 
        { 
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null) { return; }
        LassoTossable tossable = other.gameObject.GetComponent<LassoTossable>();
        if (tossable == null)
        {
            tossable = other.gameObject.GetComponentInParent<LassoTossable>();
        }

        if (tossable != null)
        {
            Vector3 dir = (other.gameObject.transform.position - transform.position).normalized;
            tossable.TossInDirection(dir, transform.up, LassoTossable.TossStrength.STRONG);
        }
        else
        {
            // Destroy breakable
            if (other.CompareTag("Breakable"))
            {
                SoundManager.Instance().PlaySFX("BarrierBreak");
                Destroy(other.gameObject);
            }
        }

    }
}
