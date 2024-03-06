using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Collectable : MonoBehaviour
{
    public enum TYPE
    {
        STAR,
        HEALTH
    }

    public enum SOURCE 
    { 
        ENEMY,
        LEVEL
    }

    public TYPE typeOfCollectable;
    public SOURCE locationCameFrom;
    private bool pickedUp = false;
    private Vector3 positionKey;
    private void Awake()
    {
        if (SOURCE.ENEMY != locationCameFrom)
        {
            print("SHOULD NOT BE HERE");
            positionKey = transform.position;
            if (!LevelData.starSparkleObjectCheckpoint.ContainsKey(positionKey))
            {
                LevelData.starSparkleObjectCheckpoint.Add(positionKey, false);
            }
            else
            {
                pickedUp = LevelData.starSparkleObjectCheckpoint[positionKey];
            }
            if (pickedUp)
            {
                GetComponent<Renderer>().enabled = false;
                GetComponent<Collider>().enabled = false;
                transform.root.GetComponent<CollectableFollow>().follow = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (TYPE.STAR == typeOfCollectable)
            {
                SoundManager.Instance().PlaySFX("CollectStarSparkle");
                LevelData.starSparkleTemp++;
                UIManager.UpdateStars();
            }
            else if (TYPE.HEALTH == typeOfCollectable)
            {
                other.GetComponentInParent<Health>().Damage(-1, Vector3.zero);
            }

            if (SOURCE.ENEMY == locationCameFrom)
            {
                transform.root.GetComponentInChildren<ParticleSystem>().Stop();
                Destroy(transform.root.gameObject);
            }
            else if (SOURCE.LEVEL == locationCameFrom)
            {
                pickedUp = true;
                GetComponent<Renderer>().enabled = false;
                GetComponent<Collider>().enabled = false;
                LevelData.starSparkleObjectCheckpoint[positionKey] = true;
                transform.root.GetComponent<CollectableFollow>().follow = false;
                transform.root.GetComponentInChildren<ParticleSystem>().Stop();
            }
        }
    }
}
