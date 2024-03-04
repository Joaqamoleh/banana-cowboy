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
            transform.GetComponent<Renderer>().enabled = false;
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
                Destroy(gameObject);
            }
            else if (SOURCE.LEVEL == locationCameFrom)
            {
                pickedUp = true;
                transform.GetComponent<Renderer>().enabled = false;
                LevelData.starSparkleObjectCheckpoint[positionKey] = true;
            }
        }
    }
}
