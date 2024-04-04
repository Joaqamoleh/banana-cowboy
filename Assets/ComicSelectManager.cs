using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComicSelectManager : MonoBehaviour
{
    private static bool unlocksInitialized = false;
    private static Dictionary<string, bool> availableComics = new Dictionary<string, bool>();
    public static string[] ComicNames = { "Cutscene1", "Cutscene2", "Cutscene3" };

    private void Awake()
    {
        if (!unlocksInitialized)
        {
            Init();
            unlocksInitialized = true;
        }

    }

    public void Init()
    {
        foreach (string comicName in ComicNames)
        {
            availableComics[comicName] = false;
        }
        availableComics["Cutscene1"] = true;
    }

    public static void SetComicUnlock(string comic, bool unlocked)
    {
        availableComics[comic] = unlocked;
    }

    public static bool GetComicUnlocked(string comic)
    {
        return availableComics[comic];
    }
}

