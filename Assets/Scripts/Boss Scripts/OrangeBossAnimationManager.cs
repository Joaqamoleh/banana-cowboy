using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrangeBossAnimationManager : MonoBehaviour
{
    public OrangeBoss instance;

    [Header("Screen Shaking")]
    public float intensity;
    public float frequency;
    public float time;

    [SerializeField]
    SoundPlayer sfxsPlayer;

    public void ShowWeakSpot(int weakSpotIndex)
    {
        instance.ShowWeakSpot(weakSpotIndex);
    }

    public void PlaySlamSFX()
    {
        //SoundManager.Instance().PlaySFX("OrangeBossPeelSlam");
        sfxsPlayer.PlaySFX("OrangeBossPeelSlam");
    }

    public void BoomerangStartupSFX()
    {
        //SoundManager.Instance().PlaySFX("OrangeBossBoomerangStartup");
        sfxsPlayer.PlaySFX("OrangeBossBoomerangStartup");
    }

    public void HideWeakSpots()
    {
        instance.HideWeakSpots();
    }

    public void ShakeCamera()
    {
        ScreenShakeManager.Instance.ShakeCamera(4, 2, 0.1f);
    }
}
