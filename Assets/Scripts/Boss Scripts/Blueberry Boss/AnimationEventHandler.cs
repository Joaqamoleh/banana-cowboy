using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
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
}
