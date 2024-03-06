using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    LassoableEnemy enemyToSpawn;

    [SerializeField]
    float delayOfRespawnAfterDeath = 4f;

    [SerializeField]
    bool spawnOnStart = true;

    public void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemy();
        }
    }

    void OnSpawnedEnemyDeath(Collision c)
    {
        Invoke("SpawnEnemy", delayOfRespawnAfterDeath);
    }

    void SpawnEnemy()
    {
        LassoableEnemy enemy = Instantiate(enemyToSpawn);
        enemy.transform.position = transform.position;
        enemy.OnDeath += OnSpawnedEnemyDeath;
    }
}
