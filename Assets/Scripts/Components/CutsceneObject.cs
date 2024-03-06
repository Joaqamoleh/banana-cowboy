using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneObject : MonoBehaviour
{
    [Tooltip("Plays scenes in the order they are listed in the array")]
    public Scene[] scenes;

    private int activeScene = 0;
    private bool _cutsceneComplete = false;
    private bool _skip = false;
    private bool _stopCutscene = false;

    public delegate void CutsceneCompleteCallback(CutsceneObject completedScene);
    public event CutsceneCompleteCallback OnCutsceneComplete;

    public void Play()
    {
        _cutsceneComplete = false;
        _stopCutscene = false;
        StopAllCoroutines();
        StartCoroutine(PlayScenes());
    }
    public bool IsCutsceneComplete()
    {
        return _cutsceneComplete;
    }

    IEnumerator PlayScenes()
    {
        while (activeScene < scenes.Length && !_stopCutscene)
        {
            _skip = false;
            PlayScene(scenes[activeScene]);
            yield return WaitForSceneCompletion(scenes[activeScene]);
            CleanupScene(scenes[activeScene]);
            activeScene++;
        }
        _cutsceneComplete = true;
        DialogueManager.Instance().HideActiveDialog();
        OnCutsceneComplete?.Invoke(this); // This is apparently a null check on OnCutsceneComplete, so things are good
    }

    void PlayScene(Scene scene)
    {
        if (scene.isDialogScene)
        {
            DialogueManager.Instance().DisplayDialog(scene.dialog);
        }

        if (scene.isTimelineScene)
        {
            if (scene.cinemachineCamController != null)
            {
                scene.cinemachineCamController.gameObject.SetActive(true);
            }
            scene.timelinePlayable.extrapolationMode = DirectorWrapMode.Hold;
            scene.timelinePlayable.Play();
        }

        if (scene.cameraShake)
        {
            // TODO:
        }
    }

    void CleanupScene(Scene scene)
    {
        if (scene.isDialogScene)
        {
            DialogueManager.Instance().HideDialog(scene.dialog);
        }
        if (scene.isTimelineScene)
        {
            if (scene.cinemachineCamController != null)
            {
                scene.cinemachineCamController.gameObject.SetActive(false);
            }
            StartCoroutine(EndTimelinePlayable(scene, _skip));
        }
    }

    IEnumerator EndTimelinePlayable(Scene s, bool skipped)
    {

        yield return new WaitForSeconds(0.01f);
        if (s.cinemachineCamController != null && skipped)
        {
            PlayerCameraController controller = FindAnyObjectByType<PlayerCameraController>();
            if (controller != null && scenes[scenes.Length - 1] == s)
            {
                controller.JumpToPlayer();
            }
        }
        s.timelinePlayable.Stop();
        yield return null;
    }

    public void SkipActiveScene()
    {
        _skip = true;
    }

    public void Stop()
    {
        _stopCutscene = true;
        SkipActiveScene();
    }

    IEnumerator WaitForSceneCompletion(Scene scene)
    {
        switch (scene.completionType)
        {
            case Scene.CompletionType.ON_INPUT:
                yield return WaitForInput(scene);
                break;
            case Scene.CompletionType.ON_DIALOG_COMPLETE:
                yield return WaitForDialog(scene.dialog);
                break;
            case Scene.CompletionType.ON_PLAYABLE_COMPLETE:
                yield return WaitForPlayable(scene.timelinePlayable);
                break;
        }
        if (!_skip)
        {
            yield return new WaitForSeconds(scene.completionDelay);
        }
    }

    IEnumerator WaitForInput(Scene s)
    {
        bool done = false;
        while (!done)
        {
            if (Input.GetKeyDown(CutsceneManager.Instance().cutsceneInput) && !PauseManager.pauseActive)
            {
                if (s.isDialogScene && !s.dialog.dialogFullyDisplayed)
                {
                    DialogueManager.Instance().DisplayDialog(s.dialog, false);
                }
                else
                {
                    done = true;
                }
            }
            yield return null;
        }
    }

    IEnumerator WaitForDialog(Dialog dialog)
    {
        if (dialog != null)
        {            
            bool done = false;
            while (!done)
            {
                if (dialog.dialogFullyDisplayed)
                {
                    done = true;
                }
                if (_skip)
                {
                    DialogueManager.Instance().DisplayDialog(dialog, false);
                    done = true;
                }
                yield return null;
            }
        }
    }

    IEnumerator WaitForPlayable(PlayableDirector playable)
    {
        if (playable != null)
        {
            bool done = false;
            while (!done) // && !_skip) for now, no skipping the camera / playables. Too hard to debug
            {
                if (playable.time >= playable.duration)
                {
                    done = true;
                }
                if (_skip)
                {
                    playable.time = playable.duration;
                    done = true;
                }
                yield return null;
            }
            playable.time = playable.duration;
        }
    }

    public void SkipToEndOfCutscene()
    {
        foreach (Scene s in scenes)
        {
            if (s.isTimelineScene)
            {
                if (s.cinemachineCamController != null)
                {
                    s.cinemachineCamController.gameObject.SetActive(false);
                    PlayerCameraController controller = FindAnyObjectByType<PlayerCameraController>();
                    if (controller != null)
                    {
                        controller.JumpToPlayer();
                    } 
                }
                s.timelinePlayable.time = s.timelinePlayable.duration;
                s.timelinePlayable.Play();
            }
        }
    }
}

[System.Serializable]
public class Scene
{
    public bool isDialogScene = false;
    [Tooltip("Only required if isDialogScene is true")]
    public Dialog dialog;

    public bool isTimelineScene = false;
    [Tooltip("Only required if isTimelineScene is true")]
    public PlayableDirector timelinePlayable = null;
    [Tooltip("Start this disabled. Will enable on cutscene play")]
    public CinemachineVirtualCamera cinemachineCamController = null;

    [Tooltip("Done only at the start of the scene")]
    public bool cameraShake = false;

    public enum CompletionType
    {
        ON_INPUT,
        ON_DIALOG_COMPLETE,
        ON_PLAYABLE_COMPLETE
    }
    public CompletionType completionType = CompletionType.ON_INPUT;
    [Tooltip("This is the time between the completion of the scene and the start of the next scene")]
    public float completionDelay = 1.5f; // Adds time between the completion of a scene and the start of the next scene
}
