using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    SoundPlayer sfxPlayer;

    [SerializeField]
    Renderer[] renderers;
    [SerializeField]
    Collider[] colliders;

    [Header("Death Juice Effect")]
    [SerializeField]
    GameObject deathEffect;
    [SerializeField]
    GameObject vfxLocation;

    [SerializeField]
    ItemDrop[] itemDrops;
    [SerializeField, Range(0f, 100f)]
    float chanceToDropItem;
    public bool enemyAIDisabled { get; set; } = false;
    bool killed = false;
    public enum DeathSource
    {
        OTHER,
        TOSSED,
        HIT_BY_TOSS,
        EXPLOSION,
    }
    [SerializeField]
    List<DeathSource> ignoreDeathBySource = new List<DeathSource>();


    public delegate void EnemyDeath(DeathSource source);
    public event EnemyDeath OnDeath;

    public void OnValidate()
    {
        if (itemDrops != null) {        
            float totalPercent = 0f;
            foreach (var item in itemDrops)
            {
                if (totalPercent + item.percentageChance <= 100f)
                {
                    totalPercent += item.percentageChance;
                }
                else
                {
                    item.percentageChance = (100f - totalPercent);
                    totalPercent = 100f;
                }
            }
        }
    }

    public void KillEnemy(bool shakeCamOnDeath)
    {
        KillEnemy(DeathSource.OTHER, shakeCamOnDeath);
    }

    public void KillEnemy(DeathSource source = DeathSource.OTHER, bool shakeCamOnDeath = false)
    {
        if (killed) { return; }
        if (ignoreDeathBySource.Contains(source)) { return; }

        killed = true;
        enemyAIDisabled = true;

        // instantiate death effect
        if (deathEffect != null)
        {
            // instantiate VFX at a certain location if it exists

            if (vfxLocation != null)
            {
                Instantiate(deathEffect, vfxLocation.transform.position, transform.rotation);
            }
            else {
                Instantiate(deathEffect, transform.position, transform.rotation);
            }
            
        }

        if (shakeCamOnDeath && ScreenShakeManager.Instance)
        {
            ScreenShakeManager.Instance.ShakeCamera(1.5f, 1, 0.1f);
        }

        sfxPlayer.PlaySFX("Splat");

        float shouldDropItem = Random.Range(0f, 100f);
        if (shouldDropItem < chanceToDropItem)
        {
            shouldDropItem = Random.Range(0f, 100f);
            float cumulative = 0f;
            foreach (var item in itemDrops)
            {
                cumulative += item.percentageChance;
                if (cumulative < shouldDropItem)
                {
                    if (item.drop != null)
                    {
                        Collectable drop = Instantiate(item.drop, transform.position, Quaternion.identity);
                        drop.src = Collectable.Source.ENEMY;
                    }
                }
            }
        }

        foreach (var r in renderers)
        {
            Destroy(r);
        }
        foreach (var c in colliders)
        {
            Destroy(c);
        }

        OnDeath?.Invoke(source);

        StartCoroutine(DestroyGameobject());
    }

    IEnumerator DestroyGameobject()
    {
        yield return new WaitForSeconds(sfxPlayer.GetSFX("Splat").src.clip.length + 0.2f);
        Destroy(transform.gameObject);
    }
}

[System.Serializable]
public class ItemDrop
{
    public Collectable drop;
    [Range(0f, 100f)]
    public float percentageChance;
}