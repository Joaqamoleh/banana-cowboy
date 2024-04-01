using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryBomb : LassoTossable
{
    public GameObject explosion;

    public delegate void CherryBombExplodeEvent(Collision c);
    public event CherryBombExplodeEvent OnExplode;

    [Header("Death Juice Prefab")]
    public GameObject deathJuiceEffect;
    public GameObject vfxLocation;

    [SerializeField]
    Renderer[] renderers;
    [SerializeField]
    Collider[] colliders;

    [SerializeField]
    SoundPlayer sfxPlayer;
    float deathDelay = 1.0f;

    bool isDestroyed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (IsTossed() && !isDestroyed)
        {
            isDestroyed = true;
            if (SoundManager.Instance() != null)
            {
                sfxPlayer.PlaySFX("Explode");
                deathDelay = sfxPlayer.GetSFX("Explode").audioClip.length + 0.1f;
            }
            if (deathJuiceEffect != null)
            {
                if (vfxLocation != null)
                {
                    Instantiate(deathJuiceEffect, vfxLocation.transform.position, transform.rotation);
                }
                else
                {
                    Instantiate(deathJuiceEffect, transform.position, transform.rotation);
                }
            }
            SpawnExplosion();
            OnExplode?.Invoke(collision);
            foreach (var r in renderers)
            {
                Destroy(r);
            }
            foreach (var c in colliders)
            {
                Destroy(c);
            }
            StartCoroutine(DestroyRoot());
        }
    }

    IEnumerator DestroyRoot()
    {
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    void SpawnExplosion()
    {
        Instantiate(explosion, gameObject.transform.position, Quaternion.identity);
    }
}
