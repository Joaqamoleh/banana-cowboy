using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ComicSelectUI : MonoBehaviour
{
    public string comicName;
    public bool isUnlocked;
    public GameObject lockedSticker;


    // Start is called before the first frame update
    void Start()
    {
        // set highlight to inactive
        // if it is locked, set button to not be interactable, give it ? sticker
        // otherwise, it is interactable, remove ? sticker 
        if (ComicSelectManager.GetComicUnlocked(comicName)) // true = unlocked
        {
            isUnlocked = true;
            lockedSticker.SetActive(false);
            GetComponent<Button>().interactable = true;
            print(comicName);
            print("UNLOCKED");
        }
        else // false = locked
        {
            isUnlocked = false;
            lockedSticker.SetActive(true);
            GetComponent<Button>().interactable = false;
            print(comicName);
            print("LOCKED");
        }
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.UI);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetComicSelect(bool fromSelectScreen)
    {
        ComicCutsceneManager.comicSelect = fromSelectScreen;
    }

    // pointer enter/exit for highlight when hovered
}
