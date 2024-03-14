using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField]
    Animator playerAnimator = null;

    [SerializeField, Range(1f, 5f)]
    float walkAnimSpeed = 1.0f, runAnimSpeed = 1.0f, airAnimSpeed = 1.5f;

    void Start()
    {
        if (GetComponent<PlayerController>() != null)
        {
            PlayerController p = GetComponent<PlayerController>();
            p.OnStateChanged += PlayerStateChanged;
            p.OnLassoStateChange += LassoStateChanged;
        }
    }
    
    void PlayerStateChanged(PlayerController.State updatedState)
    {
        if (playerAnimator == null) { return; }
        // This handles the changes to the animimations based on the player state
        //print("Player state signal: New state: " + updatedState);
        switch (updatedState)
        {
            case PlayerController.State.IDLE:
                playerAnimator.Play("Base Layer.BC_Idle");
                playerAnimator.speed = 1.0f;
                break;
            case PlayerController.State.AIR:
                playerAnimator.Play("Base Layer.BC_Fall");
                playerAnimator.speed = airAnimSpeed;
                break;
            case PlayerController.State.WALK:
                playerAnimator.Play("Base Layer.BC_Walk");
                playerAnimator.speed = walkAnimSpeed;
                break;
            case PlayerController.State.RUN:
                playerAnimator.Play("Base Layer.BC_Run");
                playerAnimator.speed = runAnimSpeed;
                break;
            case PlayerController.State.SWING:
                playerAnimator.Play("Base Layer.BC_Swing");
                break;
        }
    }

    void LassoStateChanged(PlayerController.LassoState updatedState)
    {
        switch (updatedState)
        {
            case PlayerController.LassoState.NONE:
                playerAnimator.Play("Base Layer.BC_Idle");
                playerAnimator.SetLayerWeight(1, 0.0f);
                break;
            case PlayerController.LassoState.THROWN:
                playerAnimator.Play("Base Layer.BC_Lasso");
                playerAnimator.SetLayerWeight(1, 0.0f);
                playerAnimator.speed = 1.5f;
                break;
            case PlayerController.LassoState.SWING:
                break;
            case PlayerController.LassoState.PULL:
                break;
            case PlayerController.LassoState.HOLD:
                playerAnimator.Play("Lasso Layer.BC_Hold");
                playerAnimator.SetLayerWeight(1, 1.0f);
                playerAnimator.speed = 1.5f;
                break;
            case PlayerController.LassoState.TOSS:
                playerAnimator.Play("Base Layer.BC_Lasso");
                playerAnimator.SetLayerWeight(1, 0.0f);
                playerAnimator.speed = 1.5f;
                break;
            case PlayerController.LassoState.RETRACT:
                break;
        }
    }
}
