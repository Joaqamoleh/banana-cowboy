using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [field: SerializeField] public int checkpointNum { get; private set; }

    public Animator NPC_Animator;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Set checkpoint " + checkpointNum + ".");
            // set game managers respawn coords to set coordinates
            LevelData.SetCheckpoint(checkpointNum);
            NPC_Animator.Play("Base Layer.NPC_B_OD_Checkpoint");
        }
    }
}
