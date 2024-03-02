using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingScreen;
    public GameObject confirmationScreen;
    public static bool pauseActive = false;

    public static float mouseSliderPercent = 0.5f, scrollSliderPercent = 0.5f;

    [SerializeField]
    public Slider musicSlider = null, sfxSlider = null, mouseSensitivitySlider = null, scrollSensitivitySlider;
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
        if (camController != null)
        {
            camController.orbitSensitivity = mouseSliderPercent * 
                (PlayerCameraController.orbitSensitivityMax - PlayerCameraController.orbitSensitivityMin) 
                + PlayerCameraController.orbitSensitivityMin;
        }
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

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu.activeSelf)
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
        pauseMenu.SetActive(true);
        pauseActive = true;
        Time.timeScale = 0f;
        PlayerCameraController.ShowCursor();
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        settingScreen.SetActive(false);
        pauseActive = false;
        Time.timeScale = 1.0f;
        PlayerCameraController.HideCursor();
    }

    public void ChangeScreen(Button button)
    {
        switch (button.name)
        {
            case "Continue":
                pauseMenu.SetActive(false);
                pauseActive = false;
                Time.timeScale = 1.0f;
                PlayerCameraController.HideCursor();
                break;
            case "Checkpoint Restart":
                pauseActive = false;
                Time.timeScale = 1.0f;
                SoundManager.Instance().StopAllSFX();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
            case "Settings":
                settingScreen.SetActive(true);
                break;
            case "Back":
            case "Cancel":
                settingScreen.SetActive(false);
                confirmationScreen.SetActive(false);
                break;
            case "Restart":
                pauseActive = false;
                LevelData.resetLevelData();
                SoundManager.Instance().StopAllSFX();
                Time.timeScale = 1.0f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
            case "Quit":
                confirmationScreen.SetActive(true);
                break;
            case "Confirm":
                pauseActive = false;
                LevelData.resetLevelData();
                SoundManager.Instance().StopAllSFX();
                SoundManager.Instance().StopAllMusic();
                SoundManager.Instance().PlayMusic("Menu Music");
                Time.timeScale = 1.0f;
                SceneManager.LoadScene(0);
                break;
        }
    }

}
