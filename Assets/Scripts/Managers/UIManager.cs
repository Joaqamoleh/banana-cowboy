using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    public Animator healthAnimator = null;

    [SerializeField]
    Canvas hudElementsCanvas;
    [SerializeField]
    Sprite[] healthSprites, healthBlinkSprites, reticleSprites;
    [SerializeField]
    Image healthSprite, reticleSprite, panHandSprite;
    [SerializeField]
    GameObject throwBarSpriteRoot;

    [SerializeField]
    RectTransform throwBarContainer, throwLowPower, throwMedPower, throwHighPow, throwBarIndicator;
    private int _health = 3;

    Vector3 indicatorStartingPos;

    public TMP_Text starSparkles;

    private void Awake()
    {
        instance = this;
        if (throwBarIndicator != null)
        {
            indicatorStartingPos = throwBarIndicator.localPosition;
        }
        UpdateStars();
    }

    private void Start()
    {
        if (CutsceneManager.Instance() != null)
        {
            CutsceneManager.Instance().OnCutsceneStart += HideUIForCutscene;
            CutsceneManager.Instance().OnCutsceneEnd += ShowUIPostCutscene;
        }
    }
    public static void UpdateStars()
    {
        if (instance == null) { return; }
        instance.starSparkles.text = "X " + (LevelData.starSparkleTotal + LevelData.starSparkleCheckpoint + LevelData.starSparkleTemp);    
    }

    /*    public void ChangeHealth(int change)
        {
            int newHealth = Mathf.Clamp(_health + change, 0, 4);
            if (newHealth != _health)
            {
                healthAnimator.SetTrigger("Damaged");
                _health = newHealth;
                SetAbsHealth(_health);
            }
        }*/

    public void SetHealthUI(int health, bool healed)
    {
        if (health >= 0 && health < 4)
        {
            if (healed)
            {

            }
            else
            {
                healthAnimator.SetTrigger("Damaged");
            }
            StartCoroutine(HealthIconTransition(health, healed));
        }
    }

    IEnumerator HealthIconTransition(int health, bool healed)
    {
        if (!healed)
        {
            healthSprite.GetComponent<Image>().sprite = healthBlinkSprites[health];
            yield return new WaitForSeconds(0.5f);
        }
        healthSprite.GetComponent<Image>().sprite = healthSprites[health];

    }

    public void SetReticlePosition(Vector3 screenSpacePosition)
    {
        reticleSprite.transform.position = screenSpacePosition;
    }

    public void ReticleOverLassoable()
    {
        reticleSprite.GetComponent<Image>().sprite = reticleSprites[1];
    }

    public void ReticleOverNone()
    {
        reticleSprite.GetComponent<Image>().sprite = reticleSprites[0];
    }
    public void ShowReticle()
    {
        reticleSprite.gameObject.SetActive(true);
    }

    public void HideReticle()
    {
        reticleSprite.gameObject.SetActive(false);
    }

    public void SetPanHandPosition(Vector3 screenSpacePosition)
    {
        panHandSprite.transform.position = screenSpacePosition;
    }

    public void ShowPanHand()
    {
        panHandSprite.transform.position = reticleSprite.transform.position;
        panHandSprite.gameObject.SetActive(true);
    }

    public void HidePanHand()
    {
        panHandSprite.gameObject.SetActive(false);
    }

    public void ShowThrowBar()
    {
        throwBarSpriteRoot.SetActive(true);
    }

    public void HideThrowBar()
    {
        throwBarSpriteRoot.SetActive(false);
    }

    /**
     * relativePos is on the range [-1, 1] with 0 being the center of the bar.
     */
    public void SetThrowIndicatorPos(float relativePos)
    {
        if (throwBarIndicator == null) { return; }
        float width = (throwLowPower.rect.width) * throwLowPower.localScale.x;
        float halfWidth = width / 2;
        throwBarIndicator.localPosition = new Vector3(
            indicatorStartingPos.x + relativePos * halfWidth,
            indicatorStartingPos.y, 
            indicatorStartingPos.z
        );
    }

    public LassoTossable.TossStrength GetTossIndicatorStrength()
    {
        float lowPowerWidth = (throwLowPower.rect.width / 2f) * throwLowPower.localScale.x;
        float medPowerWidth = (throwMedPower.rect.width / 2f) * throwMedPower.localScale.x;
        float highPowerWidth = (throwHighPow.rect.width / 2f) * throwHighPow.localScale.x;

        if ((throwBarIndicator.localPosition.x > 0 && throwBarIndicator.localPosition.x < (highPowerWidth)) || 
            (throwBarIndicator.localPosition.x < 0 && throwBarIndicator.localPosition.x > -(highPowerWidth)))
        {
            
            return LassoTossable.TossStrength.STRONG;
        }
        if (throwBarIndicator.localPosition.x > 0 && throwBarIndicator.localPosition.x < medPowerWidth ||
            (throwBarIndicator.localPosition.x < 0 && throwBarIndicator.localPosition.x > -medPowerWidth))
        {
            return LassoTossable.TossStrength.MEDIUM;
        }
        return LassoTossable.TossStrength.WEAK;
    }
    
    void HideUIForCutscene(CutsceneObject s)
    {
        hudElementsCanvas.gameObject.SetActive(false);
    }

    void ShowUIPostCutscene(CutsceneObject s)
    {
        hudElementsCanvas.gameObject.SetActive(true);
        PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
    }

    public static UIManager Instance()
    {
        return instance;
    }
}
