using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueberryAnimationManager : MonoBehaviour
{
    public GameObject attackHitbox;

    public void SetHitbox(int isEnabled)
    {
        if (attackHitbox != null)
        {
            Collider collider = attackHitbox.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = isEnabled == 0;
                if (isEnabled != 0)
                {
                    AllowDamage(1);
                    attackHitbox.GetComponent<MeleeHitbox>().AttackEnded();
                }
            }
        }
    }

    public void AllowDamage(int activeHitbox)
    {
        attackHitbox.GetComponent<MeleeHitbox>().SetDamagePlayer(activeHitbox == 0);
        attackHitbox.GetComponent<MeleeHitbox>().PerformAttack();
    }
}
