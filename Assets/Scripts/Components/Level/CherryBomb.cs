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

    bool isDestroyed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (IsTossed() && !isDestroyed)
        {
            isDestroyed = true;
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().PlaySFX("CherryBombExplode");
            }
            if (deathJuiceEffect != null)
            {
                Instantiate(deathJuiceEffect, transform.position, transform.rotation);
            }
            SpawnExplosion();
            OnExplode?.Invoke(collision);
            Destroy(gameObject);
        }
    }
    void SpawnExplosion()
    {
        Instantiate(explosion, gameObject.transform.position, Quaternion.identity);
    }
}
