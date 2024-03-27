using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class LevelSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovered = false;
    public bool isActive = true;
    public bool isUnlocked;
    public string levelName;

    public float planetAnimDuration = 1.0f;
    public float planetAnimStrength = 5.0f;
    public float infoAnimDuration = 0.5f;

    public GameObject highlight;
    public GameObject planetInfo;
    public GameObject lockedSticker;
    public List<GameObject> otherPlanets = new List<GameObject>();


    // Start is called before the first frame update
    void Start()
    {
        highlight.SetActive(false);
        planetInfo.SetActive(false);

        planetInfo.transform.position = transform.position;
        planetInfo.transform.localScale = Vector3.zero;

        // if unlocked
        if (LevelManager.GetLevelUnlocked(levelName))
        {
            lockedSticker.SetActive(false);
            isUnlocked = true;
            GetComponent<Button>().interactable = true;
        }
        // if locked
        else
        {
            lockedSticker.SetActive(true);
            GetComponent<Button>().interactable = false;
            isUnlocked = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            isHovered = true;
            if (isActive)
            {
                highlight.SetActive(true);
                ResetTween();
                transform.DOJump(transform.position, planetAnimStrength, 1, planetAnimDuration).SetEase(Ease.OutExpo).onComplete = ResetTween;
            }
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

    public void DisplayPlanetInfo()
    {
        // planet should not be highlightable
        isActive = false;

        // stop all dotweens 
        DOTween.KillAll();

        // selected planet should shrink
        transform.DOScale(Vector3.zero, 0.5f);

        // other planets should shrink and deactivate
        foreach (GameObject planet in otherPlanets)
        {
            planet.transform.DOScale(Vector3.zero, 0.5f).onComplete = ResetTween;
            planet.GetComponent<LevelSelectUI>().isActive = false;
        }

        // pop up for selected planet should grow FROM original location (looks like zooming in)
        planetInfo.SetActive(true);
        planetInfo.transform.DOLocalMove(Vector3.zero, infoAnimDuration).SetEase(Ease.OutExpo);
        planetInfo.transform.DOScale(Vector3.one, infoAnimDuration).SetEase(Ease.OutBounce).onComplete = ResetTween;
    }

    public void UndisplayPlanetInfo()
    {
        planetInfo.transform.DOMove(transform.position, infoAnimDuration).SetEase(Ease.OutExpo);
        planetInfo.transform.DOScale(Vector3.zero, infoAnimDuration).SetEase(Ease.OutExpo).onComplete = ResetPlanetInfo;
        
        transform.DOScale(Vector3.one, 0.5f).onComplete = ResetPlanet;

        foreach (GameObject planet in otherPlanets)
        {
            planet.transform.DOScale(Vector3.one, 0.5f).onComplete = ResetPlanets;
            
        }
    }

    private void ResetPlanetInfo()
    {
        planetInfo.SetActive(false);
    }

    private void ResetPlanet()
    {
        isActive = true;
    }

    private void ResetPlanets()
    {
        foreach (GameObject planet in otherPlanets)
        {
            planet.GetComponent<LevelSelectUI>().isActive = true;
        }
    }
}
