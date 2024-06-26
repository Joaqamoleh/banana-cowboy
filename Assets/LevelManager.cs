using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private static bool unlocksInitialized = false;
    private static Dictionary<string, bool> availableLevels = new Dictionary<string, bool>();
    public static string[] levelNames = { "Tutorial Level", "Orange Level", "Blueberry Level", "Strawberry Level", "Blender Boss Room" };
    private void Awake()
    {
#if DEBUG || DEVELOPMENT_BUILD
        foreach (string levelName in levelNames)
        {
            availableLevels[levelName] = true;
        }
#else
        if (!unlocksInitialized)
        {
            Init();
            unlocksInitialized = true;
        }
#endif
    }

    public void BackToMainMenu()
    {
        LevelSwitch.ChangeScene("Menu");
    }

    public void SelectLevel(string planetName)
    {
        LevelSwitch.ChangeScene(planetName);
        ComicCutsceneManager.comicSelect = false;
    }

    public void SelectCutscene(string cutscene)
    {
        LevelSwitch.ChangeScene(cutscene);
        ComicCutsceneManager.comicSelect = true;
    }

    public static void Init()
    {
        foreach (string s in levelNames)
        {
            availableLevels[s] = false;
        }

        availableLevels["Tutorial Level"] = true;
    }

    public static void SetLevelUnlock(string level, bool unlocked)
    {
        availableLevels[level] = unlocked;
    }

    public static bool GetLevelUnlocked(string level)
    {
        return availableLevels[level];
    }
}
