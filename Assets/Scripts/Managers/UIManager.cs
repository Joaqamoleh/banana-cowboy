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
    Sprite[] healthSprites, healthBlinkSprites, reticleSelectedSprites, reticleSprites;
    [SerializeField]
    Image healthImage, panHandImage;
    [SerializeField]
    Image[] reticleImages;
    [SerializeField]
    GameObject throwBarSpriteRoot, reticleRoot;

    [SerializeField]
    RectTransform throwBarContainer, throwLowPower, throwMedPower, throwHighPow, throwBarIndicator;
    private int _health = 3;

    Vector3 indicatorStartingPos;

    public TMP_Text starSparkles;

    [SerializeField]
    float timeForReticleLockOn = 0.1f, timeForReticleReturn = 0.4f, reticleLockOnRadius = 80f, reticleDefaultRadius = 100f;
    bool reticleHighlight = false;
    float reticleTime;

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

    private void Update()
    {
        if (reticleRoot.activeSelf)
        {
            if (reticleHighlight)
            {
                float t = EasingsLibrary.EaseOutQuint(reticleTime / timeForReticleLockOn);
                float lerpedRadius = Mathf.Lerp(reticleImages[1].rectTransform.localPosition.y, reticleLockOnRadius, t);
                reticleImages[1].rectTransform.localPosition = new Vector2(0, lerpedRadius);
                reticleImages[2].rectTransform.localPosition = new Vector2(-lerpedRadius, 0);
                reticleImages[3].rectTransform.localPosition = new Vector2(0, -lerpedRadius);
                reticleImages[4].rectTransform.localPosition = new Vector2(lerpedRadius, 0);
            }
            else
            {
                float t = EasingsLibrary.EaseOutSin(reticleTime / timeForReticleReturn);
                float lerpedRadius = Mathf.Lerp(reticleImages[1].rectTransform.localPosition.y, reticleDefaultRadius, t);
                reticleImages[1].rectTransform.localPosition = new Vector2(0, lerpedRadius);
                reticleImages[2].rectTransform.localPosition = new Vector2(-lerpedRadius, 0);
                reticleImages[3].rectTransform.localPosition = new Vector2(0, -lerpedRadius);
                reticleImages[4].rectTransform.localPosition = new Vector2(lerpedRadius, 0);
            }
            reticleTime += Time.deltaTime;
        }
    }

    public static void UpdateStars()
    {
        if (instance == null) { return; }
        instance.starSparkles.text = "X " + (LevelData.starSparkleTotal + LevelData.starSparkleCheckpoint + LevelData.starSparkleTemp);    
    }

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
            healthImage.GetComponent<Image>().sprite = healthBlinkSprites[health];
            yield return new WaitForSeconds(0.5f);
        }
        healthImage.GetComponent<Image>().sprite = healthSprites[health];

    }

    public void SetReticlePosition(Vector3 screenSpacePosition)
    {
        reticleRoot.transform.position = screenSpacePosition;
    }

    public void ReticleOverLassoable()
    {
        if (!reticleHighlight)
        {
            reticleTime = 0;
            reticleHighlight = true;
            for (int i = 0; i < reticleImages.Length; i++)
            {
                reticleImages[i].sprite = reticleSelectedSprites[i];
            }
        }
    }

    public void ReticleOverNone()
    {
        if (reticleHighlight)
        {
            reticleTime = 0;
            reticleHighlight = false;
            for (int i = 0; i < reticleImages.Length; i++)
            {
                reticleImages[i].sprite = reticleSprites[i];
            }
        }
    }
    public void ShowReticle()
    {
        reticleRoot.SetActive(true);
    }

    public void HideReticle()
    {
        reticleRoot.SetActive(false);
    }

    public void SetPanHandPosition(Vector3 screenSpacePosition)
    {
        panHandImage.transform.position = screenSpacePosition;
    }

    public void ShowPanHand()
    {
        panHandImage.transform.position = reticleRoot.transform.position;
        panHandImage.gameObject.SetActive(true);
    }

    public void HidePanHand()
    {
        panHandImage.gameObject.SetActive(false);
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
