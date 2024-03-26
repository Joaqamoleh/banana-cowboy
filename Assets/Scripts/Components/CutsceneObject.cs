using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

public class CutsceneObject : MonoBehaviour
{
    public bool canSkipCutscene = true;
    [Tooltip("Plays scenes in the order they are listed in the array")]
    public Scene[] scenes;

    private int activeScene = 0;
    private bool _cutsceneComplete = false;
    private bool _skip = false;
    private bool _stopCutscene = false;

    public delegate void CutsceneCallback(CutsceneObject completedScene);
    public event CutsceneCallback OnCutsceneStart;
    public event CutsceneCallback OnCutsceneComplete;

    private Coroutine dialogueScreenShake = null;


    public void Play()
    {
        _cutsceneComplete = false;
        _stopCutscene = false;
        StopAllCoroutines();
        StartCoroutine(PlayScenes());
        OnCutsceneStart?.Invoke(this);
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
            _shakeTimer = 0; // Stops screen from shaking if not finished shaking 
        }

        if (scene.cinemachineCamController != null)
        {
            scene.cinemachineCamController.gameObject.SetActive(true);
        }
        if (scene.isTimelineScene)
        {
            scene.timelinePlayable.extrapolationMode = DirectorWrapMode.Hold;
            scene.timelinePlayable.Play();
        }

        if (scene.cameraShake && scene.cinemachineCamController != null && scene.cinemachineCamController.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>() != null)
        {
            StartCoroutine(ShakeScreen(scene));
        }
    }

    // TODO: Change to fit the dialogue
    float _shakeTimer = 0;

    IEnumerator ShakeScreen(Scene scene)
    {
        _shakeTimer = scene.shakeDuration;
        CinemachineBasicMultiChannelPerlin shakingCamera = scene.cinemachineCamController.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        shakingCamera.m_AmplitudeGain = scene.shakeAmplitude;
        shakingCamera.m_FrequencyGain = scene.shakeIntensity;
        while (_shakeTimer > 0)
        {
            _shakeTimer -= Time.deltaTime;
            shakingCamera.m_AmplitudeGain = Mathf.Lerp(scene.shakeAmplitude, 0f, 1f - (_shakeTimer / scene.shakeDuration));
            yield return null;
        }
        shakingCamera.m_AmplitudeGain = 0;
    }

    void CleanupScene(Scene scene)
    {
        if (scene.isDialogScene)
        {
            DialogueManager.Instance().HideDialog(scene.dialog);
        }
        if (scene.isTimelineScene)
        {
            StartCoroutine(EndTimelinePlayable(scene, _skip));
        }

        if (scene.cinemachineCamController != null && (scenes[scenes.Length - 1] == scene || scenes[activeScene].cinemachineCamController != scene.cinemachineCamController))
        {
            StartCoroutine(EndCinemachine(scene));
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

    IEnumerator EndCinemachine(Scene s)
    {
        float duration = s.cameraShake ? Mathf.Clamp(s.shakeDuration - (s.timelinePlayable != null ? (float)s.timelinePlayable.duration : 0.0f), 0.1f, 10.0f) : 0.1f;
        yield return new WaitForSeconds(duration);
        s.cinemachineCamController.gameObject.SetActive(false);
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
            if (GetSceneInput(s) && !PauseManager.pauseActive)
            {
                // TODO: Needs testing
                /*#if UNITY_IOS || UNITY_ANDROID
                                if (EventSystem.current.IsPointerOverGameObject())
                                {
                                    yield return null;
                                }
                #endif*/
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

    bool GetSceneInput(Scene s)
    {
        if (Input.GetKeyDown(s.cutsceneInput))
        {
            return true;
        }
        foreach (KeyCode k in s.altInputs)
        {
            if (Input.GetKeyDown(k))
            {
                return true;
            }
        }
        return false;
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
                OnCutsceneComplete?.Invoke(this);
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
    public float shakeIntensity = 4f, shakeAmplitude = 6f, shakeDuration = 2f;

    [Tooltip("The input for going to the next scene")]
    public KeyCode cutsceneInput = KeyCode.Mouse0;
    public KeyCode[] altInputs;

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
