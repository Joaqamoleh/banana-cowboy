using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableFollow : MonoBehaviour
{
    private GameObject _player = null;
    public bool follow = true;
    Vector3 _directionOfPlayer;
    public int moveSpeed;

    private void Start()
    {
        follow = true;
    }

    private void Update()
    {
        if (follow && _player != null)
        {
            _directionOfPlayer = (_player.transform.position - transform.position).normalized;
            transform.position += _directionOfPlayer.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player = other.gameObject;
        }   
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player = null;
        }
    }
}
