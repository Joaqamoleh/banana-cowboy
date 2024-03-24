using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    [SerializeField]
    SoundPlayer lassoSFXPlayer, playerSFXPlayer;

    private void Start()
    {
        PlayerController pc = GetComponent<PlayerController>();
        pc.OnJumpPressed += PlayerJump;
        pc.OnLeftGround += PlayerLeftGround;
        pc.OnLanded += PlayerLanded;
        pc.OnStateChanged += PlayerStateUpdate;
        pc.OnLassoStateChange += LassoStateUpdate;
        pc.OnLassoTossed += LassoToss;
        GetComponent<Health>().OnDamaged += PlayerDamaged;
    }
    void PlayerJump()
    {
        playerSFXPlayer.PlaySFX("PlayerJump");
    }

    void PlayerLeftGround()
    {
        playerSFXPlayer.StopSFX("PlayerWalk");
        playerSFXPlayer.StopSFX("PlayerRun");
    }

    void PlayerLanded(Rigidbody hitGround)
    {
        playerSFXPlayer.PlaySFX("PlayerLand");
    }

    void PlayerDamaged(int damage, bool hasDied)
    {
        if (hasDied)
        {
            playerSFXPlayer.PlaySFX("PlayerDeath");
        }
        else
        {
            playerSFXPlayer.PlaySFX("PlayerHurt");
        }
    }

    void LassoToss(LassoTossable.TossStrength strength)
    {
        if (strength == LassoTossable.TossStrength.WEAK)
        {
            lassoSFXPlayer.PlaySFX("LassoWeakThrow");
        }
        else if (strength == LassoTossable.TossStrength.MEDIUM)
        {
            lassoSFXPlayer.PlaySFX("LassoMediumThrow");
        }
        else
        {
            lassoSFXPlayer.PlaySFX("LassoStrongThrow");
        }
    }

    void PlayerStateUpdate(PlayerController.State updated)
    {
        switch (updated)
        {
            default:
            case PlayerController.State.IDLE:
                playerSFXPlayer.StopSFX("PlayerRun");
                playerSFXPlayer.StopSFX("PlayerWalk");
                break;
            case PlayerController.State.WALK:
                playerSFXPlayer.PlaySFX("PlayerWalk");
                playerSFXPlayer.StopSFX("PlayerRun");
                break;
            case PlayerController.State.RUN:
                playerSFXPlayer.PlaySFX("PlayerRun");
                playerSFXPlayer.StopSFX("PlayerWalk");
                break;

        }
    }
    void LassoStateUpdate(PlayerController.LassoState state)
    {
        switch (state)
        {
            case PlayerController.LassoState.RETRACT:
            case PlayerController.LassoState.NONE:
                lassoSFXPlayer.StopSFX("LassoSpin");
                break;
            case PlayerController.LassoState.PULL:
                break;
            case PlayerController.LassoState.THROWN:
                lassoSFXPlayer.PlaySFX("LassoThrow");
                break;
            case PlayerController.LassoState.HOLD:
                lassoSFXPlayer.PlaySFX("LassoSpin");
                break;
            default:
                break;
        }
    }

}
