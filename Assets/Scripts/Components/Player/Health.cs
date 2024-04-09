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

    public PlayerCameraController playerCameraController;

    public Material normalColor;
    public Material damageColor;

    public Renderer charRender;
    public Renderer lassoRender;

    public GameObject deathVFX;

    public delegate void DamageEvent(int damage, bool hasDied);
    public DamageEvent OnDamaged;

    private void Start()
    {
        health = maxHealth;
        LevelData.OnStarSparkleChanged += OnStarSparkleChangedHandler;
        LevelData.CheckpointReached += OnCheckpointReached;
    }

    private void OnDestroy()
    {
        LevelData.OnStarSparkleChanged -= OnStarSparkleChangedHandler;
        LevelData.CheckpointReached -= OnCheckpointReached;
    }

    // Callback method for the OnStarSparkleChanged event
    private void OnStarSparkleChangedHandler(int totalStarSparkleCount)
    {
        if (totalStarSparkleCount % 10 == 0)
        {
            Damage(-1, Vector3.zero);
        }
    }

    private void OnCheckpointReached()
    {
        Damage(-3, Vector3.zero);
    }

    public void Damage(int damage, Vector3 knockback)
    {
        if (damage < 0) // Got healed
        {
            if (health == 3)
            {
                print("ALL READY FULL");
                return;
            }
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
            if (damage != 0) {
                playerUI.SetHealthUI(health, false);
            }
            ScreenShakeManager.Instance.ShakeCamera(3, 3, 0.1f);
            if (health <= 0)
            {
                OnDamaged?.Invoke(damage, true); // true if player has died from hit
                StartCoroutine(DyingExplosion());
            }
            else
            {
                OnDamaged?.Invoke(damage, false);
                if (GetComponent<Rigidbody>() != null && !GetComponent<Rigidbody>().isKinematic)
                {
                    GetComponent<Rigidbody>().AddForce(knockback, ForceMode.Impulse);
                }
            }
        }
    }

    IEnumerator DyingExplosion() {
        // turn off renderer for Banana Cowboy
        StartCoroutine(DisableMesh());
        playerCameraController.FreezeCamera();
        if (deathVFX != null)
        {
            Instantiate(deathVFX, GetComponent<Rigidbody>().transform.position, GetComponent<Rigidbody>().transform.rotation);
        }
        yield return new WaitForSeconds(1.5f);
        // reset checkpoint data
        LevelData.ResetCheckpointData();
        SoundManager.Instance().StopAllSFX();
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

    IEnumerator DisableMesh() {
        charRender.forceRenderingOff = true;
        lassoRender.forceRenderingOff = true;
        for (int i = 0; i < charRender.transform.childCount; i++) {
            Transform t = charRender.transform.GetChild(i);
            Renderer r = t.GetComponent<Renderer>();
            if (r != null) {
                r.forceRenderingOff = true;
            } else {
                // handle eyes
                for (int j = 0; j < t.childCount; j++) { 
                    Transform tj = t.GetChild(j);
                    Renderer rj = tj.GetComponent<Renderer>();
                    if (rj != null) {
                        rj.forceRenderingOff = true;
                    }
                }
            }
        }
        yield return null;
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

    public bool GetCanTakeDamage()
    {
        return _canTakeDamage;
    }
}
