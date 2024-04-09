using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingObject : MonoBehaviour
{
    public Transform target;
    public int direction = 1;
    public float angle = 0f;
    public float speed = 2f;
    public float radius = 15f;

    Vector3 offset;

    private float x = 0f;
    private float z = 0f;
    float rotationAngle;

    SoundPlayer _soundPlayer;

    private void Start()
    {
        _soundPlayer = GetComponent<SoundPlayer>();
        _soundPlayer.PlaySFX("Scream");
    }

    void Update()
    {
        x = Mathf.Cos(angle);
        z = Mathf.Sin(angle);

        offset = new Vector3(0, z, x) * radius;

        transform.position = target.position + offset;
        angle += direction * speed * Time.deltaTime;
    }
}
