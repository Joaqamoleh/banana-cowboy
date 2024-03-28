using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlenderBossAnimationManager : MonoBehaviour
{
    public BlenderBoss instance;

    public void PlayBombAttack()
    {
        instance.PlayPlaceBombs();
    }
}
