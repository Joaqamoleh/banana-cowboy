using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockLevel : MonoBehaviour
{
    public string levelToUnlock;

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.tag == "Player")
        {
            LevelManager.SetLevelUnlock(levelToUnlock, true);
        }
    }
}
