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

    
    [Tooltip("The name of the cutscene to play when the scene starts")]
    public string cutsceneOnStart = string.Empty;
    [Tooltip("The list of cutscenes that can be played by this manager")]
    public Cutscene[] cutscenes;
    [Tooltip("Key to skip cutscenes (If the cutscene is skipable")]
    KeyCode skipkey = KeyCode.Mouse0;

    static List<string> playedCutscenes = new List<string>();

    private Cutscene activeCutscene = null;

    public delegate void CutsceneEventCallback(CutsceneObject activeCutscene);
    public event CutsceneEventCallback OnCutsceneStart;
    public event CutsceneEventCallback OnCutsceneEnd;

    private void Awake()
    {
        s_instance = this;
        for (int i = 0; i < cutscenes.Length; i++)
        {
            cutscenes[i].cutscene.index = i;
            cutscenes[i].cutscene.hasPlayed = playedCutscenes.Contains(cutscenes[i].name);
        }
    }

    void Start()
    {
        Invoke("PlayStartOnDelay", 0.2f);
    }

    void PlayStartOnDelay()
    {
        PlayStartCutscene();
        foreach (var played in playedCutscenes)
        {
            print("Skipping cutscene: " + played);
            CutsceneObject skipped = GetCutsceneByName(played);
            skipped.SkipToEndOfCutscene();
            OnCutsceneEnd?.Invoke(skipped);
        }
    }

    private void Update()
    {
        if (activeCutscene != null && !activeCutscene.cutscene.IsCutsceneComplete())
        {
            if (Input.GetKeyDown(skipkey) && !PauseManager.pauseActive && activeCutscene.cutscene.canSkipCutscene)
            {
                activeCutscene.cutscene.SkipActiveScene();
            }
        }
    }

    public void PlayStartCutscene()
    {
        if (cutsceneOnStart != string.Empty && !playedCutscenes.Contains(cutsceneOnStart))
        {
            PlayCutsceneByName(cutsceneOnStart);
            //if (hasPlayedStartingCutscene && !repeatCutsceneOnRestart)
            //{
            //    Cutscene cs = Array.Find(cutscenes, cs => cs.name == cutsceneOnStart);
            //    OnCutsceneEnd?.Invoke(cs.cutscene);
            //    cs.cutscene.SkipToEndOfCutscene();
            //}
            //if (repeatCutsceneOnRestart || (!repeatCutsceneOnRestart && !hasPlayedStartingCutscene))
            //{
            //    hasPlayedStartingCutscene = true;
            //}
        }
    }

    public void PlayCutsceneByName(string name)
    {
        Cutscene cs = Array.Find(cutscenes, cs => cs.name == name);
        if (cs == null) { return; }
        if (cs.cutscene.hasPlayed && cs.cutscene.playCutsceneOncePerLevel) { return; }
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

    //public static void ResetPlayStartingCutscene()
    //{
    //    hasPlayedStartingCutscene = false;
    //}

    public static void ResetPlayedCutscenes()
    {
        playedCutscenes = new List<string>();
    }

    public void HasPlayedCutscene(CutsceneObject completedScene)
    {
        if (name != cutsceneOnStart)
        {
            Cutscene cs = Array.Find(cutscenes, cs => cs.cutscene == completedScene);
            if (cs != null)
            {
                playedCutscenes.Add(cs.name);
            }
        }
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
