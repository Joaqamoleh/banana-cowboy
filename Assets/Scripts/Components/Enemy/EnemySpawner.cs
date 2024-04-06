using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    EnemyController enemyToSpawn;

    [SerializeField]
    GameObject respawnVFX;

    [SerializeField]
    float delayOfRespawnAfterDeath = 4f;

    [SerializeField]
    bool spawnOnStart = true;
    [SerializeField]
    EnemyController existingEnemy = null;

    public void OnEnable()
    {
        if (spawnOnStart)
        {
            SpawnEnemy();
        }
        if (!spawnOnStart)
        {
            existingEnemy.OnDeath += OnSpawnedEnemyDeath;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (existingEnemy != null)
        {
            Destroy(existingEnemy.gameObject);
            existingEnemy = null;
        }
    }

    void OnSpawnedEnemyDeath(EnemyController.DeathSource s)
    {
        StartCoroutine(SpawnEnemyDelayed());
    }

    IEnumerator SpawnEnemyDelayed()
    {
        if (respawnVFX != null) {
            Instantiate(respawnVFX, transform.position, transform.rotation);
        }
        yield return new WaitForSeconds(delayOfRespawnAfterDeath);
        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        EnemyController enemy = Instantiate(enemyToSpawn);
        enemy.transform.position = transform.position;
        enemy.OnDeath += OnSpawnedEnemyDeath;
    }
}
