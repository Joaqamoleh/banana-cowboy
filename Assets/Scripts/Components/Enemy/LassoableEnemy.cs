using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LassoableEnemy : LassoTossable
{
    bool isDestroyed = false;
    public GameObject item1;
    public GameObject item2;

    int shouldDropItem;
    int whichItem;
    private void OnCollisionEnter(Collision collision)
    {
        // TODO: Handle orange, blueberry, strawberry enemies and bosses, and blender
        if (IsTossed() && !isDestroyed)
        {
            if (collision.collider.CompareTag("Boss"))
            {
                // Will fix to handle more bosses (for orange, handle weak spots too)
                if (collision.transform.name == "Orange Boss" || collision.transform.parent.parent.name == "Orange Boss" || collision.transform.name.Contains("Peel")){
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
                DestroySelf();
            }
            // TODO: Clean this up later
            if (collision.collider.CompareTag("Breakable"))
            {
                Destroy(collision.collider.gameObject);
            }
        }
    }

    void DestroySelf()
    {
        isDestroyed = true;
        DropItem();
        SoundManager.Instance().PlaySFX("EnemySplat");
        ScreenShakeManager.Instance.ShakeCamera(1.5f, 1, 0.1f);
        Destroy(gameObject);
    }

    void DropItem()
    {
        shouldDropItem = Random.Range(1, 100);
        if (shouldDropItem <= 60) // 60% to drop something
        {
            whichItem = Random.Range(1, 100);
            if (whichItem <= 80) // 80% to drop item1
            {
                if (item1 != null)
                {
                    print("Item 1 dropped");
                    Instantiate(item1, transform.position, Quaternion.identity);
                }
            }
            else
            {
                print("Item 2 dropped");
                if (item2 != null)
                {
                    Instantiate(item2, transform.position, Quaternion.identity);
                }
            }
        }
        else
        {
            print("Didn't drop anything");
        }
    }
}
