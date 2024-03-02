using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public int health { get; private set; }
    public int maxHealth { get; set; } = 3;

    private bool _canTakeDamage = true;
    public UIManager playerUI;

    private void Start()
    {
        health = maxHealth;   
    }

    public void Damage(int damage, Vector3 knockback)
    {
        if (_canTakeDamage && health > 0)
        {
            StartCoroutine(InvincibleFrames());
            health = Mathf.Clamp(health - damage, 0, 3);
            playerUI.SetHealthUI(health);
            if (health <= 0)
            {
                // Death
            }
            else
            {
                if (GetComponent<Rigidbody>() != null && !GetComponent<Rigidbody>().isKinematic)
                {
                    GetComponent<Rigidbody>().AddForce(knockback, ForceMode.Impulse);
                }
            }
        }
    }

    IEnumerator InvincibleFrames()
    {
        _canTakeDamage = false;
        yield return new WaitForSeconds(2f);
        _canTakeDamage = true;
    }

    public void SetHealth(int health)
    {
        this.health = health;
    }
}
