using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueShooterController : EnemyController
{
    // STATES:
    // IDLE     ---- Player Enters Detection --->    SHOOTING
    // SHOOTING ---- Player Exits Outer Detection ---> IDLE


    // SHOOTING STATES: go in order
    // PREPARING / AIMING SHOT, SHOOTING, COOLDOWN
    // Can only exit shooting in preparing or cooldown state. Aiming and shooting will go to completion

    private enum ShooterState
    {
        IDLE,
        PREPARING_SHOT,
        AIMING,
        SHOOTING,
        COOLDOWN,
    }
    private ShooterState state;

    [SerializeField]
    private DetectionTriggerHandler sightAgroRange, loseAgroRange;

    private Transform target;
}
