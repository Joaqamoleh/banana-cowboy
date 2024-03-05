using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PlayerCursor : MonoBehaviour
{
    public enum CursorType
    {
        UI,
        LASSO_AIM,
        CAMERA_PAN,
        HIDDEN
    }
    public static CursorType activeType;
    static CursorType prevType;

    private static PlayerCursor instance;

    public static float cursorSensitivity = 20f, maxCursorSensitivity = 40f, minCursorSensitivity = 1f;

    [SerializeField]
    UIManager playerUI;
    [SerializeField]
    Texture2D UICursorTexture;
    [SerializeField]
    LayerMask lassoLayerMask;

    Vector2 currentCursorPos;

    public delegate void HoverLassoableEvent(LassoObject hoveredObject);
    public event HoverLassoableEvent OnHoverLassoableEnter;
    public event HoverLassoableEvent OnHoverLassoableExit;
    LassoObject currentlyHovered = null;


    private void Start()
    {
        instance = this;
        Cursor.SetCursor(UICursorTexture, Vector2.zero, CursorMode.Auto);
        SetActiveCursorType(CursorType.LASSO_AIM);

        currentCursorPos = new Vector2(Screen.width / 2, Screen.height / 2);
    }

    private void Update()
    {
        if (playerUI == null) { return; }
        if (activeType == CursorType.LASSO_AIM) // Might be more here later, but the only one that moves is this one
        {
            // Get input to move indicator on screen
            float dMouseX = Input.GetAxisRaw("Mouse X");
            float dMouseY = Input.GetAxisRaw("Mouse Y");
            currentCursorPos = new Vector2(
                Mathf.Clamp(currentCursorPos.x + dMouseX * cursorSensitivity, 0f, Screen.width), 
                Mathf.Clamp(currentCursorPos.y + dMouseY * cursorSensitivity, 0f, Screen.height));
        }

        if (activeType == CursorType.LASSO_AIM)
        {
            playerUI.SetReticlePosition(currentCursorPos);
            LassoObject hover = GetHoveredLassoObject();

            if (currentlyHovered == null && hover != null && hover.isLassoable) {
                // New object hovered
                OnHoverLassoableEnter?.Invoke(hover);
                currentlyHovered = hover;

            }
            else if (currentlyHovered != null && hover == null && currentlyHovered.isLassoable)
            {
                // Hovered object no longer hovered
                OnHoverLassoableExit?.Invoke(currentlyHovered);
                currentlyHovered = hover;

            }
            else if (currentlyHovered != null && hover != null && hover != currentlyHovered)
            {
                // Check to see if not equal, then if they aren't, then two updates to send signals about
                if (currentlyHovered.isLassoable)
                {
                    OnHoverLassoableExit?.Invoke(currentlyHovered);
                }
                if (hover.isLassoable)
                {
                    OnHoverLassoableEnter?.Invoke(hover);
                }
                currentlyHovered = hover;
            }


            if (hover != null && hover.isLassoable && !hover.currentlyLassoed)
            {
                playerUI.ReticleOverLassoable();
            } 
            else
            {
                playerUI.ReticleOverNone();
            }
        }
    }

    /**
     * Returns null when no lasso object is hovered over
     */
    LassoObject GetHoveredLassoObject()
    {
        if (activeType != CursorType.LASSO_AIM) { return null; }

        Ray mouseRay = Camera.main.ScreenPointToRay(currentCursorPos);
        RaycastHit hit;
        if (Physics.Raycast(mouseRay, out hit, 100f, lassoLayerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject.GetComponentInParent<LassoObject>(); 
        } 
        else
        {
            return null;
        }

    }

    public Ray GetCursorRay()
    {
        return Camera.main.ScreenPointToRay(currentCursorPos);
    }

    void SetCursorType(CursorType type)
    { 
        if (activeType != type)
        {
            prevType = activeType;
            activeType = type;

            if (activeType == CursorType.UI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } 
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            playerUI.HideReticle();
            playerUI.HidePanHand();

            switch (activeType)
            {
                case CursorType.UI:
                    break;
                case CursorType.LASSO_AIM:
                    playerUI.ShowReticle();
                    break;
                case CursorType.CAMERA_PAN:
                    playerUI.ShowPanHand();
                    break;
                case CursorType.HIDDEN:
                    break;
            }
        }

    }
    public static void SetActiveCursorType(CursorType type)
    {
        if (instance == null || instance.playerUI == null) { return; }
        instance.SetCursorType(type);
    }

    public static void GoBackToPreviousCursorType()
    {
        if (instance == null || instance.playerUI == null) { return; }
        instance.SetCursorType(prevType);
    }

    public static CursorType GetActiveCursorType()
    {
        return activeType;
    }
}
