using UnityEngine;


public class CharacterDialogue : MonoBehaviour
{
    public Animator NPC_Animator;
    public Dialog dialog;
    

    private void OnTriggerEnter(Collider collision)
    {
        if (dialog.speakerDialog != string.Empty) { 
            if (collision.CompareTag("Player"))
            {
                print(DialogueManager.Instance());
                DialogueManager.Instance().DisplayDialog(dialog);
                if (NPC_Animator != null)
                {
                    NPC_Animator.Play("Base Layer.B_OD_Talk_Start");
                }
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (dialog.speakerDialog != string.Empty)
        {
            if (collision.CompareTag("Player"))
            {
                DialogueManager.Instance().HideDialog(dialog);
                if (NPC_Animator != null)
                {
                    NPC_Animator.Play("Base Layer.B_OD_Idle");
                }
            }
        }
    }
}