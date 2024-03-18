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

    public float flatKnockback;

    private void Start()
    {
        transform.localScale = Vector3.one;
        if (Random.Range(0,2) == 1)
        {
            direction = -1;
        }
        StartCoroutine(GrowSize());   
    }

    private void Update()
    {
        transform.Rotate(speed *  direction * Time.deltaTime * Vector3.up);
    }

    IEnumerator GrowSize()
    {
        float currentSize = transform.localScale.x;

        while (currentSize < maxSize)
        {
            float newSize = currentSize + growthRate * Time.deltaTime;
            newSize = Mathf.Min(newSize, maxSize); // Clamp the size to maxSize
            transform.localScale = new Vector3(newSize, transform.localScale.y, transform.localScale.z);
            currentSize = newSize;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<Health>().Damage(1, flatKnockback * ((transform.position - other.transform.position).normalized + Vector3.up));
        }
    }
}
