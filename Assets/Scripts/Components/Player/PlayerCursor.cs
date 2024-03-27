using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCursor : MonoBehaviour
{
    public enum CursorType
    {
        UI,
        LASSO_AIM,
        CAMERA_PAN,
        HIDDEN
    }
    CursorType activeType = CursorType.HIDDEN;

    private static PlayerCursor instance;

    public static float cursorSensitivity = 20f, maxCursorSensitivity = 40f, minCursorSensitivity = 1f;

    [SerializeField]
    UIManager playerUI;
    [SerializeField]
    Texture2D UICursorTexture;
    [SerializeField]
    LayerMask lassoLayerMask;

    Vector2 currentCursorPos;

    //public delegate void HoverLassoableEvent(LassoObject hoveredObject);
    //public event HoverLassoableEvent OnHoverLassoableEnter;
    //public event HoverLassoableEvent OnHoverLassoableExit;
    //LassoObject currentlyHovered = null;
    private int _validTouchID = -1;

    float dMouseX;
    float dMouseY;

    private void Start()
    {
        instance = this;
        Cursor.SetCursor(UICursorTexture, Vector2.zero, CursorMode.Auto);
        currentCursorPos = new Vector2(Screen.width / 2, Screen.height / 2);
        SetCursorType(activeType);
    }

    private void OnEnable()
    {
        SetCursorType(activeType);
    }

    private void Update()
    {
        if (playerUI == null) { return; }
        if (activeType == CursorType.LASSO_AIM) // Might be more here later, but the only one that moves is this one
        {
            // Get input to move indicator on screen
            dMouseX = Input.GetAxisRaw("Mouse X");
            dMouseY = Input.GetAxisRaw("Mouse Y");
            currentCursorPos = new Vector2(
                Mathf.Clamp(currentCursorPos.x + dMouseX * cursorSensitivity, 0f, Screen.width),
                Mathf.Clamp(currentCursorPos.y + dMouseY * cursorSensitivity, 0f, Screen.height));
        }

#if !(UNITY_IOS || UNITY_ANDROID)
        if (activeType == CursorType.LASSO_AIM)
        {
            playerUI.SetReticlePosition(currentCursorPos);
            LassoObject hover = GetHoveredLassoObject();

            if (hover != null && hover.isLassoable && !hover.currentlyLassoed && hover.isInRange)
            {
                playerUI.ReticleOverLassoable();
            }
            else
            {
                playerUI.ReticleOverNone();
            }
        }
#else
        if (activeType == CursorType.LASSO_AIM)
        {
            foreach (Touch touch in Input.touches)
            {
                // Check if it's the beginning phase of touch
                if (touch.phase == TouchPhase.Began)
                {
                    // Check if the touch is not touching a UI element
                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        // Assign the valid touch ID if it's not already set
                        if (_validTouchID == -1)
                        {
                            _validTouchID = touch.fingerId;
                        }
                    }
                }

                // Check if it's the end phase of touch
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    // Check if the ended/canceled touch matches the valid touch ID
                    if (touch.fingerId == _validTouchID)
                    {
                        // Reset the valid touch ID
                        _validTouchID = -1;
                    }
                }

                // Check if the current touch is the valid one
                if (touch.fingerId == _validTouchID)
                {
                    // Get the touch position
                    Vector2 touchPos = Input.GetTouch(0).position;

                    // Perform raycast from the touch position
                    LassoObject hover = GetTouchedLassoObject(touchPos);

                    if (hover != null && hover.isLassoable && !hover.currentlyLassoed)
                    {
                        // Handle click on lassoable object
                        //OnHoverLassoableEnter?.Invoke(hover);
                        playerUI.ReticleOverLassoable();
                    }
                    else
                    {
                        // Handle click outside lassoable object
                        playerUI.ReticleOverNone();
                    }

                    // Update reticle position based on touch position
                    if (Input.touchCount > 0)
                    {
                        currentCursorPos = Input.GetTouch(Input.touchCount - 1).position;
                        playerUI.SetReticlePosition(currentCursorPos);
                    }
                    break;
                }
            }
        }
#endif
    }

    public LassoObject GetTouchedLassoObject(Vector2 touchPosition)
    {
        if (activeType != CursorType.LASSO_AIM) { return null; }

        Ray touchRay = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        if (Physics.Raycast(touchRay, out hit, 100f, lassoLayerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject.GetComponentInParent<LassoObject>();
        }
        else
        {
            return null;
        }
    }


    /**
     * Returns null when no lasso object is hovered over
     */
    public LassoObject GetHoveredLassoObject()
    {
        if (activeType != CursorType.LASSO_AIM) { return null; }

        Ray mouseRay = Camera.main.ScreenPointToRay(currentCursorPos);
        RaycastHit hit;
        if (Physics.Raycast(mouseRay, out hit, 100f, lassoLayerMask, QueryTriggerInteraction.Collide))
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
            activeType = type;
            print("Setting cursor type of " + type);
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
        if (type == CursorType.UI)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
#if !(UNITY_IOS || UNITY_ANDROID)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
#endif
        }
        if (instance == null || instance.playerUI == null) { return; }
        instance.SetCursorType(type);
    }
}
