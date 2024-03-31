using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public GameObject TextBox;
    public GameObject MovementText;
    public GameObject LassoGrabText;
    public GameObject LassoSwingText;

    private void Start()
    {
        ResetAllTutorialUI();

        CutsceneManager.Instance().GetCutsceneByName("Intro").OnCutsceneComplete += ShowMovementText;
        CutsceneManager.Instance().GetCutsceneByName("Throwables").OnCutsceneComplete += ShowLassoGrabText;
        CutsceneManager.Instance().GetCutsceneByName("Swinging").OnCutsceneComplete += ShowLassoSwingText;
    }

    public void ResetAllTutorialUI()
    {
        TextBox.SetActive(false);
        MovementText.SetActive(false);
        LassoGrabText.SetActive(false);
        LassoSwingText.SetActive(false);
    }

    private void ShowMovementText(CutsceneObject o)
    {
        TextBox.SetActive(true);
        MovementText.SetActive(true);
    }

    private void ShowLassoGrabText(CutsceneObject o)
    {
        TextBox.SetActive(true);
        LassoGrabText.SetActive(true);
    }

    private void ShowLassoSwingText(CutsceneObject o)
    {
        TextBox.SetActive(true);
        LassoSwingText.SetActive(true);
    }
}
