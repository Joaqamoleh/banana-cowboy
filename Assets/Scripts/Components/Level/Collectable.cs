using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Collectable : MonoBehaviour
{
    public enum TYPE
    {
        STAR,
        HEALTH
    }

    public TYPE typeOfCollectable;

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
            Destroy(gameObject);
        }
    }
}
