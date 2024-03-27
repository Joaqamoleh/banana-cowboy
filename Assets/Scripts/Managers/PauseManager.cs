using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField]
    GameObject pauseScreen, defaultMenu, settingsMenu, confirmationMenu;

    public enum PauseMenus
    {
        NONE,
        DEFAULT,
        SETTINGS,
        CONFIRMATION,
    }
    private PauseMenus activeMenu, previouslyActive;


    public static bool pauseActive;

    public static float mouseSliderPercent = 0.5f, scrollSliderPercent = 0.5f, panSliderPercent = 0.5f;

    [SerializeField]
    public Slider musicSlider = null, sfxSlider = null, mouseSensitivitySlider = null, scrollSensitivitySlider = null, panSensitivitySlider = null;
    PlayerCameraController camController;

    private void Start()
    {
        if (musicSlider != null)
        {
            musicSlider.value = SoundManager.Instance().MusicVolume;
            musicSlider.onValueChanged.AddListener(delegate { OnMusicValueChanged(); });
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = SoundManager.Instance().SFXVolume;
            sfxSlider.onValueChanged.AddListener(delegate { OnSFXValueChanged(); });
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = mouseSliderPercent;
            mouseSensitivitySlider.onValueChanged.AddListener(delegate { OnMouseSensitivityChanged(); });
        }

        if (scrollSensitivitySlider != null)
        {
            scrollSensitivitySlider.value = scrollSliderPercent;
            scrollSensitivitySlider.onValueChanged.AddListener(delegate { OnScrollSensitivityChanged(); });
        }

        if (panSensitivitySlider != null)
        {
            panSensitivitySlider.value = panSliderPercent;
            panSensitivitySlider.onValueChanged.AddListener (delegate { OnPanSensitivityChanged(); });
        }

        camController = FindAnyObjectByType<PlayerCameraController>();
        if (camController != null)
        {
            camController.orbitSensitivity = mouseSliderPercent *
                (PlayerCameraController.orbitSensitivityMax - PlayerCameraController.orbitSensitivityMin)
                + PlayerCameraController.orbitSensitivityMin;

            camController.orbitZoomSensitivity = scrollSliderPercent *
                (PlayerCameraController.orbitZoomSensitivityMax - PlayerCameraController.orbitZoomSensitivityMin)
                + PlayerCameraController.orbitZoomSensitivityMin;
        }

        PlayerCursor.cursorSensitivity = mouseSliderPercent *
            (PlayerCursor.maxCursorSensitivity - PlayerCursor.minCursorSensitivity)
            + PlayerCursor.minCursorSensitivity;
    }

    public void OnMusicValueChanged()
    {
        SoundManager.Instance().MusicVolume = musicSlider.value;
    }

    public void OnSFXValueChanged()
    {
        SoundManager.Instance().SFXVolume = sfxSlider.value;
    }

    public void OnMouseSensitivityChanged()
    {
        mouseSliderPercent = mouseSensitivitySlider.value;
        PlayerCursor.cursorSensitivity = mouseSliderPercent *
            (PlayerCursor.maxCursorSensitivity - PlayerCursor.minCursorSensitivity)
            + PlayerCursor.minCursorSensitivity;
    }

    public void OnScrollSensitivityChanged()
    {
        scrollSliderPercent = scrollSensitivitySlider.value;
        if (camController != null)
        {
            camController.orbitZoomSensitivity = scrollSliderPercent * 
                (PlayerCameraController.orbitZoomSensitivityMax - PlayerCameraController.orbitZoomSensitivityMin) 
                + PlayerCameraController.orbitZoomSensitivityMin;
        }
    }

    public void OnPanSensitivityChanged()
    {
        panSliderPercent = panSensitivitySlider.value;
        if (camController != null)
        {
            camController.orbitSensitivity = panSliderPercent *
                (PlayerCameraController.orbitSensitivityMax - PlayerCameraController.orbitSensitivityMin)
                + PlayerCameraController.orbitSensitivityMin;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseScreen.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseScreen.SetActive(true);
        SetActiveMenu(PauseMenus.DEFAULT);

        pauseActive = true;
        Time.timeScale = 0f;
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.UI);
    }

    public void ResumeGame()
    {
        pauseScreen.SetActive(false);
        SetActiveMenu(PauseMenus.NONE);

        pauseActive = false;
        Time.timeScale = 1.0f;
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
    }

    public void SetActiveMenu(PauseMenus menu)
    {
        previouslyActive = activeMenu;
        activeMenu = menu;
        defaultMenu.SetActive(false);
        settingsMenu.SetActive(false);
        confirmationMenu.SetActive(false);
        switch(menu)
        {
            case PauseMenus.DEFAULT:
                defaultMenu.SetActive(true);
                break;
            case PauseMenus.SETTINGS:
                settingsMenu.SetActive(true);
                break;
            case PauseMenus.CONFIRMATION:
                confirmationMenu.SetActive(true);
                break;
            default:
            case PauseMenus.NONE:
                break;
        }
    }

    public void SetToLastActiveMenu()
    {
        SetActiveMenu(previouslyActive);
    }

    public void OnSettingsPressed()
    {
        SetActiveMenu(PauseMenus.SETTINGS);
    }

    public void OnBackPressed()
    {
        SetActiveMenu(PauseMenus.DEFAULT);
    }

    public void OnQuitPressed()
    {
        SetActiveMenu(PauseMenus.CONFIRMATION);
    }

    public void OnCheckpointRestartPressed()
    {
        ResumeGame();
        LevelData.ResetCheckpointData();
        SoundManager.Instance().StopAllSFX();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnRestartLevelPressed()
    {
        LevelData.ResetLevelData();
        OnCheckpointRestartPressed();
    }

    public void OnConfirmQuitPressed()
    {
        pauseActive = false;
        Time.timeScale = 1.0f;
        LevelData.ResetLevelData();
        LevelSwitch.ChangeScene("Level Select");
        SoundManager.Instance().StopAllSFX();
    }

}
