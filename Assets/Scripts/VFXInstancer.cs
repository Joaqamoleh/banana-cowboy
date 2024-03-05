using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXInstancer : MonoBehaviour
{
    ParticleSystem pSystem;
    // Start is called before the first frame update
    void Start()
    {
        pSystem = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!pSystem.isPlaying) {
            Destroy(gameObject);
        }
    }   
}
