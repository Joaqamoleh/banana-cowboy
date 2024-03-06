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

    public Material normalColor;
    public Material damageColor;
    public Renderer charRender;

    public GameObject deathVFX;

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
            if (damageColor != null && normalColor != null && charRender != null)
            {
                StartCoroutine(FlashInvincibility());
            }
            health = Mathf.Clamp(health - damage, 0, 3);
            playerUI.SetHealthUI(health, false);
            ScreenShakeManager.Instance.ShakeCamera(2, 3, 0.1f);
            if (health <= 0)
            {
                // Death, make player disappear and splat effects here
                StartCoroutine(dyingExplosion());
            }
            else
            {
                if (GetComponent<Rigidbody>() != null && !GetComponent<Rigidbody>().isKinematic)
                {
                    SoundManager.Instance().PlaySFX("PlayerHurt");
                    GetComponent<Rigidbody>().AddForce(knockback, ForceMode.Impulse);
                }
            }
        }
    }

    IEnumerator dyingExplosion() {
        if (deathVFX != null)
        {
            Instantiate(deathVFX, GetComponent<Rigidbody>().transform.position, GetComponent<Rigidbody>().transform.rotation);
        }
        yield return new WaitForSeconds(1.0f);
        // reset checkpoint data
        LevelData.ResetCheckpointData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator FlashInvincibility()
    {
        while (!_canTakeDamage)
        {
            charRender.material = damageColor; // Assign damageColor material
            yield return new WaitForSeconds(0.2f);
            charRender.material = normalColor; // Assign normalColor material
            yield return new WaitForSeconds(0.2f); // Add a slight delay before looping again
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
