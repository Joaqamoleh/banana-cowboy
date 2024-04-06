using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryBombSpawner : MonoBehaviour
{
    [SerializeField]
    CherryBomb cherryBombPrefab;
    CherryBomb cb;


    [SerializeField]
    GameObject respawnVFX;

    [SerializeField]
    public float spawnDelay = 3f;
    bool spawnActive = false;
    void OnEnable()
    {
        SpawnCherry();
        spawnActive = true;
    }

    private void OnDisable()
    {
        spawnActive = false;
        StopAllCoroutines();
        if (cb != null)
        {
            cb.TossInDirection(Vector3.zero, Vector3.up, LassoTossable.TossStrength.STRONG);
        }
    }

    void OnCherryExplode(Collision c)
    {
        if (spawnActive)
        {
            StartCoroutine(SpawnOnDelay());
        }
    }

    IEnumerator SpawnOnDelay()
    {
        // do respawn vfx if it exists
        if (respawnVFX != null)
        {
            Instantiate(respawnVFX, transform);
        }
        yield return new WaitForSeconds(spawnDelay);
        SpawnCherry();
    }

    void SpawnCherry()
    {
        cb = Instantiate(cherryBombPrefab);
        cb.transform.position = transform.position;
        cb.OnExplode += OnCherryExplode;
    }

}
