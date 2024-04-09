using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class ComicSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    bool isHovered = false;
    public string comicName;
    public bool isUnlocked;

    public float animStrength;
    public float animDuration;

    public GameObject lockedSticker;
    public GameObject highlight;


    // Start is called before the first frame update
    void Start()
    {
        // set highlight to inactive
        highlight.SetActive(false);

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            isHovered = true;
                highlight.SetActive(true);
                ResetTween();
                transform.DOJump(transform.position, animStrength, 1, animDuration).SetEase(Ease.OutExpo).onComplete = ResetTween;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            isHovered = false;
            highlight.SetActive(false);
        }

    }

    public void ResetTween()
    {
        transform.DORewind();
    }

    public void SetComicSelect(bool fromSelectScreen)
    {
        ComicCutsceneManager.comicSelect = fromSelectScreen;
    }

    // pointer enter/exit for highlight when hovered
}
