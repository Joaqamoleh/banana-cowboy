using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryBomb : LassoableEnemy
{
    public GameObject explosion;

    public delegate void CherryBombExplodeEvent(Collision c);
    public event CherryBombExplodeEvent OnExplode;

    private void OnCollisionEnter(Collision collision)
    {
        if (IsTossed())
        {
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
