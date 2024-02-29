using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int health { get; private set; } = 0;
    public int maxHealth { get; set; } = 3;

    public void Damage(int damage, Vector3 knockback)
    {
        health = Mathf.Clamp(health - damage, 0, 1);
        if (GetComponent<Rigidbody>() != null && !GetComponent<Rigidbody>().isKinematic)
        {
            GetComponent<Rigidbody>().AddForce(knockback, ForceMode.Impulse);
        }
    }

    public void SetHealth(int health)
    {
        this.health = health;
    }
}
