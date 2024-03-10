using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool mouseOverPanel;

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOverPanel = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOverPanel = false;
    }
}
