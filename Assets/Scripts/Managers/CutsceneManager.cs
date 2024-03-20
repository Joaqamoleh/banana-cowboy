using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/**
 * Cutscene managers are per-scene. If any cutscene is
 * called when it does not exist, it will not be played
 */
public class CutsceneManager : MonoBehaviour
{
    private static CutsceneManager s_instance = null;
    private static bool hasPlayedStartingCutscene = false;

    [Tooltip("The input for skipping / going to the next scene")]
    public KeyCode cutsceneInput = KeyCode.Mouse0;
    [Tooltip("The name of the cutscene to play when the scene starts")]
    public string cutsceneOnStart = string.Empty;
    public bool repeatCutsceneOnRestart = false;
    [Tooltip("The list of cutscenes that can be played by this manager")]
    public Cutscene[] cutscenes;

    private Cutscene activeCutscene = null;

    public delegate void CutsceneEventCallback(CutsceneObject activeCutscene);
    public event CutsceneEventCallback OnCutsceneStart;
    public event CutsceneEventCallback OnCutsceneEnd;

    

    private void Awake()
    {
        s_instance = this;
    }

    void Start()
    {
        Invoke("PlayStartOnDelay", 0.2f);
    }

    void PlayStartOnDelay()
    {
        s_instance.PlayStartCutscene();
    }

    private void Update()
    {
        if (activeCutscene != null && !activeCutscene.cutscene.IsCutsceneComplete())
        {
            if (Input.GetKeyDown(cutsceneInput) && !PauseManager.pauseActive)
            {
                activeCutscene.cutscene.SkipActiveScene();
            }
        }
    }

    public void PlayStartCutscene()
    {
        if (cutsceneOnStart != string.Empty)
        {
            if (hasPlayedStartingCutscene && !repeatCutsceneOnRestart)
            {
                Cutscene cs = Array.Find(cutscenes, cs => cs.name == cutsceneOnStart);
                OnCutsceneEnd?.Invoke(cs.cutscene);
                cs.cutscene.SkipToEndOfCutscene();
            }
            if (repeatCutsceneOnRestart || (!repeatCutsceneOnRestart && !hasPlayedStartingCutscene))
            {
                PlayCutsceneByName(cutsceneOnStart);
                hasPlayedStartingCutscene = true;
            }
        }
    }

    public void PlayCutsceneByName(string name)
    {
        Cutscene cs = Array.Find(cutscenes, cs => cs.name == name);
        if (cs == null) { return; }
        if (activeCutscene != null) { 
            activeCutscene.cutscene.Stop();
            activeCutscene.cutscene.OnCutsceneComplete -= CutsceneFinished;
        }
        activeCutscene = cs;
        activeCutscene.cutscene.OnCutsceneComplete += CutsceneFinished;
        activeCutscene.cutscene.Play();
        OnCutsceneStart?.Invoke(activeCutscene.cutscene);
    }

    void CutsceneFinished(CutsceneObject completedScene)
    {
        OnCutsceneEnd?.Invoke(completedScene);
    }

    public static CutsceneManager Instance()
    {
        return s_instance;
    }

    public static void ResetPlayStartingCutscene()
    {
        hasPlayedStartingCutscene = false;
    }

    public CutsceneObject GetCutsceneByName(string name)
    {
        Cutscene cs = Array.Find(cutscenes, cs => cs.name == name);
        return cs.cutscene;
    }
}

[System.Serializable]
public class Cutscene
{
    public string name;
    public CutsceneObject cutscene;
}
