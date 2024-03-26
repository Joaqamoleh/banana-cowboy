using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BananaPunch : MonoBehaviour
{
    [SerializeField]
    GameObject bananaPunchHitBox;

    [SerializeField]
    Transform leftPunchHitboxLocation, rightPunchHitboxLocation;
    Transform charOrientation;

    [SerializeField]
    KeyCode leftPunchButton = KeyCode.O, rightPunchButton = KeyCode.P;

    [SerializeField]
    float punchCooldown = 0.4f, hitboxLifetime = 0.1f;

    float lastPunchTime;

    public bool canPunch;

    private void OnEnable()
    {
        if (charOrientation == null)
        {
            charOrientation = GetComponent<GravityObject>().characterOrientation;
        }
    }

    private void Update()
    {
        if (!canPunch) { return; }
        if (Time.time - lastPunchTime > punchCooldown)
        {
            if (Input.GetKeyDown(leftPunchButton))
            {
                print("Left Punch");
                PerformPunch(leftPunchHitboxLocation.position);
            } 
            else if (Input.GetKeyDown(rightPunchButton))
            {
                print("RIght Punch");
                PerformPunch(rightPunchHitboxLocation.position);
            }
        }
    }

    void PerformPunch(Vector3 punchLocation)
    {
        Quaternion rot = charOrientation != null ? charOrientation.rotation : transform.rotation;
        GameObject hitbox = Instantiate(bananaPunchHitBox);
        hitbox.GetComponent<PunchHitbox>().lifetime = hitboxLifetime;
        int random = Random.Range(0, hitbox.transform.childCount);
        hitbox.transform.GetChild(random).gameObject.SetActive(true);
        hitbox.transform.SetPositionAndRotation(punchLocation, rot);
        lastPunchTime = Time.time;
    }
}
