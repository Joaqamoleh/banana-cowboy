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

    public void ShootBombObjects(int num)
    {
        instance.ShootBombObjects(num == 0);
    }

    public void BlenderSpinHelper()
    {
        instance.BlenderSpinHelper();
    }

    public void GrabFruitMinion()
    {
        instance.GrabFruitMinion();
        SFXManagerPlay("MinionGrab");
    }

    public void HideFruitMinion()
    {
        instance.HideFruitMinionInHand();
    }

    public void SFXManagerPlay(string sfx)
    {
        instance.BlenderSFXManager(sfx, true);
    }

    public void SFXManagerStop(string sfx)
    {
        instance.BlenderSFXManager(sfx, false);
    }

    public void PlayBombSFX()
    {
        instance.BlenderSFXManager("BombLaunch", true);
    }
}
