using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryBombSpawner : MonoBehaviour
{
    [SerializeField]
    CherryBomb cherryBombPrefab;

    public float spawnDelay = 3f;
    void Start()
    {
        SpawnCherryBomb();
    }

    void OnCherryExplode(Collision c)
    {
        Invoke("SpawnCherryBomb", spawnDelay);
    }

    void SpawnCherryBomb()
    {
        CherryBomb cb = Instantiate(cherryBombPrefab);
        cb.transform.position = transform.position;
        cb.OnExplode += OnCherryExplode;
    }

}
