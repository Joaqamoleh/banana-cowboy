using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    LassoableEnemy enemyToSpawn;

    [SerializeField]
    GameObject respawnVFX;

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
        // do respawn vfx if it exists
        if (respawnVFX != null) {
            Instantiate(respawnVFX, transform.position, transform.rotation);
        }
        Invoke("SpawnEnemy", delayOfRespawnAfterDeath);
    }

    void SpawnEnemy()
    {
        LassoableEnemy enemy = Instantiate(enemyToSpawn);
        enemy.transform.position = transform.position;
        enemy.OnDeath += OnSpawnedEnemyDeath;
    }
}
