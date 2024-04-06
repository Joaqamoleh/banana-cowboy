using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField]
    public int health = 3;

    public delegate void OnDamage(int damage);
    public event OnDamage onDamage;

    public void DamageBoss(int damage)
    {
        onDamage?.Invoke(damage);
    }
}
