using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyColliderHandler : MonoBehaviour
{
    public delegate void BodyCollision(Collision c);
    public event BodyCollision OnBodyCollisionEnter;
    public event BodyCollision OnBodyCollisionExit;

    [SerializeField]
    EnemyController controller;

    public EnemyController GetEnemyController()
    {
        return controller;
    }

    public void OnCollisionEnter(Collision collision)
    {
        OnBodyCollisionEnter?.Invoke(collision);
    }

    public void OnCollisionExit(Collision collision)
    {
        OnBodyCollisionExit?.Invoke(collision);
    }
}
