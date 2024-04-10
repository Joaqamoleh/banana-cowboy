using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    [SerializeField]
    SoundPlayer sfxPlayer;

    public delegate void SwordAnimationEvent(bool active);
    public event SwordAnimationEvent OnSwordAnimChange;

    public void SetSwordActive()
    {
        OnSwordAnimChange?.Invoke(true);
    }

    public void SetSwordInactive()
    {
        OnSwordAnimChange?.Invoke(false);
    }

    public void DrawSwordSFX()
    {
        sfxPlayer.PlaySFX("Draw");
    }

    public void SwingSwordSFX()
    {
        sfxPlayer.PlaySFX("Swing");
    }
    
    public void StartDizzySFX()
    {
        sfxPlayer.PlaySFX("Dizzy");
    }

    public void StopDizzySFX()
    {
        sfxPlayer.StopSFX("Dizzy");
    }
}
