using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryBombSpawner : MonoBehaviour
{
    [SerializeField]
    CherryBomb cherryBombPrefab;

    [SerializeField]
    GameObject respawnVFX;

    [SerializeField]
    public float spawnDelay = 3f;
    void Start()
    {
        SpawnCherryBomb();
    }

    void OnCherryExplode(Collision c)
    {
        // do respawn vfx if it exists
        if (respawnVFX != null)
        {
            Instantiate(respawnVFX, transform.position, transform.rotation);
        }
        Invoke("SpawnCherryBomb", spawnDelay);
    }

    void SpawnCherryBomb()
    {
        CherryBomb cb = Instantiate(cherryBombPrefab);
        cb.transform.position = transform.position;
        cb.OnExplode += OnCherryExplode;
    }

}
