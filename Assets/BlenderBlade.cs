using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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
    public GameObject windVFX;

    private float elapsedTime = 0f;
    private float spinDuration = 2;
    bool done = false;

    // Slow down variables
    float slowDownDuration = 0.75f; // Duration for slowing down
    float passedTime = 0f;
    float startSpeed;

    private void Start()
    {
        done = false;
        transform.localScale = Vector3.one;
        if (Random.Range(0, 2) == 1)
        {
            direction = -1;
        }
        blenderBoss = GameObject.FindWithTag("Boss").GetComponent<BlenderBoss>();
        blenderBoss.modelAnimator.ResetTrigger("BladeDone");
        startSpeed = speed;
        StartCoroutine(GrowSize());
    }

    private bool isReversing = false;

    private void Update()
    {
        if (blenderBoss.GetPhase() == 2 && !isReversing && !done)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= spinDuration)
            {
                StartCoroutine(ReverseSpin());
            }
        }

        transform.Rotate(speed * direction * Time.deltaTime * Vector3.up);
    }

    int directionStored;
    IEnumerator ReverseSpin()
    {
        isReversing = true;
        directionStored = direction;
        blenderBoss.modelAnimator.SetTrigger("BladeButton"); //Play("BL_Press_Button_Switch_Rotation");

        passedTime = 0f;

        while (passedTime < slowDownDuration)
        {
            speed = Mathf.Lerp(startSpeed, 0f, passedTime / slowDownDuration);
            passedTime += Time.deltaTime;
            yield return null;
        }

        direction = 0;
        yield return new WaitForSeconds(0.1f); // CHANGE IF NEED LONGER PAUSE TIME    
        direction = (-1 * directionStored);
        elapsedTime = 0f;

        while (passedTime < slowDownDuration)
        {
            speed = Mathf.Lerp(0f, startSpeed, passedTime / slowDownDuration);
            passedTime += Time.deltaTime;
            yield return null;
        }

        speed = startSpeed;
        elapsedTime = 0f;
        isReversing = false;
    }



    IEnumerator GrowSize()
    {
        float currentSize = transform.localScale.x;
        float newSize = 0;
        if (blenderBoss.GetPhase() == 2)
        {
            speed *= 1.2f;
        }
        while (currentSize < maxSize)
        {
            newSize = currentSize < 10? currentSize + minGrowthRate * Time.deltaTime : currentSize + growthRate * Time.deltaTime;
            newSize = Mathf.Min(newSize, maxSize); // Clamp the size to maxSize
            transform.localScale = new Vector3(newSize, transform.localScale.y, newSize);
            windVFX.transform.localScale = new Vector3(newSize, newSize, newSize);
            currentSize = newSize;
            yield return null;
        }
        print("GROWING DONE");
    }

    public IEnumerator ShrinkSize()
    {
        done = true;
        StopAllCoroutines();
        blenderBoss.modelAnimator.SetTrigger("BladeDone");
        float currentSize = transform.localScale.x;
        float minSize = 0; // Define the minimum size as 0
        float newSize = 0;
        while (currentSize > minSize)
        {
            newSize = currentSize - growthRate * 10 * Time.deltaTime; // Subtract growthRate to shrink
            newSize = Mathf.Max(newSize, minSize); // Clamp the size to minSize
            transform.localScale = new Vector3(newSize, transform.localScale.y, newSize);
            windVFX.transform.localScale = new Vector3(newSize, newSize, newSize);
            currentSize = newSize;
            yield return null;

        }
        Destroy(gameObject);
    }


    /*    private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponentInParent<Health>().Damage(1, flatKnockback * ((transform.position - other.transform.position).normalized + Vector3.up));
            }
        }*/
}
