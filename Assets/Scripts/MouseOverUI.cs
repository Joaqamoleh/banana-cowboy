using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MouseOverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool mouseOverPanel;
    public bool isAnimated;
    public float animDuration = 1.0f;
    public float animStrength = 5.0f;

    public void ResetTween()
    {
        transform.DORewind();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOverPanel = true;
        if (isAnimated)
        {
            ResetTween();
            // transform.DOShakePosition(animDuration, animStrength, randomness:0f).onComplete = ResetTween;
            // transform.DOShakeRotation(animDuration, animStrength).onComplete = ResetTween; ;
            transform.DOJump(transform.position, animStrength, 1, animDuration).SetEase(Ease.OutExpo).onComplete = ResetTween;
            // transform.DOJump(transform.position, animStrength, 1, animDuration).onComplete = ResetTween;
        }
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOverPanel = false;
    }
}
