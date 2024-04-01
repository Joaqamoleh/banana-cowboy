using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSoundController : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField]
    Button button;

    void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (SoundManager.Instance() == null) { return; }
        SoundManager.Instance().PlaySFX("ClickUI");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SoundManager.Instance() == null) { return; }
        SoundManager.Instance().PlaySFX("HoverUI");
    }
}
