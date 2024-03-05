using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        if (damage < 0) // Got healed
        {
            health = Mathf.Clamp(health - damage, 0, 3);
            playerUI.SetHealthUI(health, true);
            return;
        }
        if (_canTakeDamage && health > 0)
        {
            StartCoroutine(InvincibleFrames());
            health = Mathf.Clamp(health - damage, 0, 3);
            playerUI.SetHealthUI(health, false);
            ScreenShakeManager.Instance.ShakeCamera(2, 3, 0.1f);
            if (health <= 0)
            {
                // Death, make player disappear and splat effects here

                // reset checkpoint data
                LevelData.ResetCheckpointData();
                SceneManager.LoadScene("Orange Boss Scene");
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
