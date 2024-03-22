using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlenderBlade : MonoBehaviour
{
    public float speed;
    public int direction = 1;
    public int maxSize;
    public int time;
    public float growthRate;
    public float minGrowthRate;
    private BlenderBoss blenderBoss;
    public float flatKnockback;

    private void Start()
    {
        transform.localScale = Vector3.one;
        if (Random.Range(0, 2) == 1)
        {
            direction = -1;
        }
        blenderBoss = GameObject.FindWithTag("Boss").GetComponent<BlenderBoss>();
        StartCoroutine(GrowSize());
    }

    private void Update()
    {
        transform.Rotate(speed * direction * Time.deltaTime * Vector3.up);
    }

    IEnumerator GrowSize()
    {
        float currentSize = transform.localScale.x;
        float newSize = 0;
        if (blenderBoss.GetPhase() == 2)
        {
            speed *= 1.5f;
        }
        while (currentSize < maxSize) // TODO: Make it grow faster if x is at a certain size
        {
            newSize = currentSize < 10? currentSize + minGrowthRate * Time.deltaTime : currentSize + growthRate * Time.deltaTime;
            newSize = Mathf.Min(newSize, maxSize); // Clamp the size to maxSize
            transform.localScale = new Vector3(newSize, transform.localScale.y, transform.localScale.z);
            currentSize = newSize;
            yield return null;
        }
        print("GROWING DONE");
    }

    public IEnumerator ShrinkSize()
    {
        float currentSize = transform.localScale.x;
        float minSize = 0; // Define the minimum size as 0
        float newSize = 0;
        while (currentSize > minSize)
        {
            newSize = currentSize - growthRate * 10 * Time.deltaTime; // Subtract growthRate to shrink
            newSize = Mathf.Max(newSize, minSize); // Clamp the size to minSize
            transform.localScale = new Vector3(newSize, transform.localScale.y, transform.localScale.z);
            currentSize = newSize;
            yield return null;
        }
        Destroy(gameObject);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<Health>().Damage(1, flatKnockback * ((transform.position - other.transform.position).normalized + Vector3.up));
        }
    }
}
