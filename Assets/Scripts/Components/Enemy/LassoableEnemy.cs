using UnityEngine;

public class LassoableEnemy : LassoTossable
{
    bool isDestroyed = false;

    [Header("Item Drops")]
    public GameObject item1;
    public GameObject item2;

    [Header("Death Juice Prefab")]
    public GameObject deathJuiceEffect;

    public delegate void LassoEnemyDeath(Collision c);
    public event LassoEnemyDeath OnDeath;

    private void OnCollisionEnter(Collision collision)
    {
        if (IsTossed() && !isDestroyed)
        {
            HandleCollision(collision);
            OnDeath?.Invoke(collision);
        }
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.collider.CompareTag("Boss"))
        {
            HandleBossCollision(collision);
        }
        else
        {
            ScreenShakeManager.Instance.ShakeCamera(1.5f, 1, 0.1f);
            DestroySelf();
        }

        if (collision.collider.CompareTag("Breakable"))
        {
            SoundManager.Instance().PlaySFX("BarrierBreak");
            Destroy(collision.collider.gameObject);
        }
    }

    private void HandleBossCollision(Collision collision)
    {
        if (collision.transform.name.Contains("Blender"))
        {
            BlenderBoss boss = GameObject.Find("Blender Boss").GetComponent<BlenderBoss>();
            boss.Damage(1);
        }
        else if(collision.transform.name == "Orange Boss" || collision.transform.parent.parent.name == "Orange Boss" || collision.transform.name.Contains("Peel"))
        {
            OrangeBoss boss = GameObject.Find("Orange Boss").GetComponent<OrangeBoss>();
            if (collision.transform.name.Contains("Weak Spot"))
            {
                print("Weak Spot Damage");
                boss.Damage(2);
            }
            else
            {
                print("Normal Damage");
                boss.Damage(1);
            }
        }
         
        DestroySelf();
    }

    private void DestroySelf()
    {
        isDestroyed = true;
        DropItem();
        SoundManager.Instance().PlaySFX("EnemySplat");
        InstantiateDeathJuiceEffect();
        Destroy(gameObject);
    }

    private void DropItem()
    {
        int shouldDropItem = Random.Range(1, 100);
        if (shouldDropItem <= 60)
        {
            int whichItem = Random.Range(1, 100);
            if (whichItem <= 100)
            {
                DropItem(item1);
            }
            else
            {
                DropItem(item2);
            }
        }
        else
        {
            print("Didn't drop anything");
        }
    }

    private void DropItem(GameObject item)
    {
        if (item != null)
        {
            print(item.name + " dropped");
            GameObject spawnedItem = Instantiate(item, transform.position, Quaternion.identity);
            spawnedItem.transform.GetChild(0).GetChild(0).GetComponent<Collectable>().locationCameFrom = Collectable.SOURCE.ENEMY;
        }
    }

    private void InstantiateDeathJuiceEffect()
    {
        if (deathJuiceEffect != null)
        {
            Instantiate(deathJuiceEffect, transform.position, transform.rotation);
        }
    }
}

/*using UnityEngine;

public class LassoableEnemy : LassoTossable
{
    bool isDestroyed = false;

    [Header("Item Drops")]
    public GameObject item1;
    public GameObject item2;

    [Header("Death Juice Prefab")]
    public GameObject deathJuiceEffect;

    public delegate void LassoEnemyDeath(Collision c);
    public event LassoEnemyDeath OnDeath;

    int shouldDropItem;
    int whichItem;
    private void OnCollisionEnter(Collision collision)
    {
        // TODO: Handle orange, blueberry, strawberry enemies and bosses, and blender
        if (IsTossed() && !isDestroyed)
        {
            print(collision.collider.name);
            if (collision.collider.CompareTag("Boss"))
            {
                // Will fix to handle more bosses (for orange, handle weak spots too)
                if (collision.transform.name == "Orange Boss" || collision.transform.parent.parent.name == "Orange Boss" || collision.transform.name.Contains("Peel"))
                {
                    OrangeBoss boss = GameObject.Find("Orange Boss").GetComponent<OrangeBoss>();
                    if (collision.transform.name.Contains("Weak Spot"))
                    {
                        print("Weak Spot Damage");
                        boss.Damage(2);
                        DestroySelf(); // TODO: There's a bug where sometimes you hit multiple collision. Need a better(?) way to fix. Test isDestroyed
                    }
                    else
                    {
                        print("Normal Damage");
                        boss.Damage(1);
                        DestroySelf();
                    }
                }
            }
            else
            {
                ScreenShakeManager.Instance.ShakeCamera(1.5f, 1, 0.1f);
                DestroySelf();
            }
            // TODO: Clean this up later
            if (collision.collider.CompareTag("Breakable"))
            {
                Destroy(collision.collider.gameObject);
                SoundManager.Instance().PlaySFX("BarrierBreak");
            }
            OnDeath?.Invoke(collision);
        }
    }

    void DestroySelf()
    {
        isDestroyed = true;
        DropItem();
        SoundManager.Instance().PlaySFX("EnemySplat");
        // initiate juice explosion
        if (deathJuiceEffect != null) {
            Instantiate(deathJuiceEffect, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }

    void DropItem()
    {
        shouldDropItem = Random.Range(1, 100);
        if (shouldDropItem <= 60) // 60% to drop something
        {
            whichItem = Random.Range(1, 100);
            if (whichItem <= 100) // 80% to drop item1. Make 100 for now until health is done
            {
                if (item1 != null)
                {
                    print("Item 1 dropped");
                    GameObject item = Instantiate(item1, transform.position, Quaternion.identity);
                    item.transform.GetChild(0).GetChild(0).GetComponent<Collectable>().locationCameFrom = Collectable.SOURCE.ENEMY;
                }
            }
            else
            {
                print("Item 2 dropped");
                if (item2 != null)
                {
                    GameObject item = Instantiate(item2, transform.position, Quaternion.identity);
                    item.transform.GetChild(0).GetChild(0).GetComponent<Collectable>().locationCameFrom = Collectable.SOURCE.ENEMY;
                }
            }
        }
        else
        {
            print("Didn't drop anything");
        }
    }
}*/