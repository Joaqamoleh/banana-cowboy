using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public string cutsceneToPlay;
    public bool cutscenePlayOnce;

    bool cutscenePlayed = false;
    bool previousUIDeleted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.tag == "Player")
        {
            if (cutscenePlayOnce && !cutscenePlayed)
            {
                CutsceneManager.Instance().PlayCutsceneByName(cutsceneToPlay);
                cutscenePlayed = true;
            }
            else if (!cutscenePlayOnce)
            {
                CutsceneManager.Instance().PlayCutsceneByName(cutsceneToPlay);
            }

            if (FindObjectOfType<TutorialUI>() != null && !previousUIDeleted)
            {
                FindObjectOfType<TutorialUI>().ResetAllTutorialUI();
                previousUIDeleted = true;
            }
        }
    }
}
