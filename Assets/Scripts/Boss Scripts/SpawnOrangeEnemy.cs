using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnOrangeEnemy : MonoBehaviour
{

    public GameObject character;

    public void Spawn()
    {
        GameObject temp = Instantiate(character, transform.position, transform.rotation);
        if (SceneManager.GetActiveScene().name.Contains("Blender")) // Messy but works
        {
            temp.GetComponent<OrangeEnemyController>().ChangeSightRange(0.6f);
        }
        Destroy(gameObject);
    }
}
