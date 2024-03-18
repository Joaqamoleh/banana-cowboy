using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CollectableFollow : MonoBehaviour
{
    private GameObject _player = null;
    public bool follow = true;
    Vector3 _directionOfPlayer;
    public int moveSpeed;

    private void Start()
    {
        follow = true;
        if (SceneManager.GetActiveScene().name.Contains("Boss"))
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void Update()
    {
        if (follow && _player != null)
        {
            _directionOfPlayer = (_player.transform.position - transform.position).normalized;
            transform.position += moveSpeed * Time.deltaTime * _directionOfPlayer.normalized;
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
