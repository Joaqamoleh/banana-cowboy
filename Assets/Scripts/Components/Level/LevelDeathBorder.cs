using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDeathBorder : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject != null)
        {
            if (other.gameObject.GetComponentInParent<Health>() != null)
            {
                other.gameObject.GetComponentInParent<Health>().Damage(999, Vector3.zero);
            }
            else if (other.GetComponent<BodyColliderHandler>() != null && other.GetComponent<BodyColliderHandler>().GetEnemyController() != null)
            {
                other.GetComponent<BodyColliderHandler>().GetEnemyController().KillEnemy();
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }
}
