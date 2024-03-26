using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class LevelSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovered = false;
    public float animDuration = 1.0f;
    public float animStrength = 5.0f;
    public GameObject highlight;
    public GameObject planetInfo;

    // Start is called before the first frame update
    void Start()
    {
        highlight.SetActive(false);
        planetInfo.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        highlight.SetActive(true);
        ResetTween();
        transform.DOJump(transform.position, animStrength, 1, animDuration).SetEase(Ease.OutExpo).onComplete = ResetTween;
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        highlight.SetActive(false);
    }

    public void ResetTween()
    {
        transform.DORewind();
    }

    public void DisplayPlanetInfo()
    {
        planetInfo.SetActive(true);
    }
}
