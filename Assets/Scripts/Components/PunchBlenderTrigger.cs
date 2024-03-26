using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchBlenderTrigger : MonoBehaviour
{
    [SerializeField]
    Animator evilTwinAnimator; // For future effects, such as the evil banana being scared in the glass dome or the animation of him getting punched

    [SerializeField]
    ParticleSystem glassParticle;

    [SerializeField]
    CutsceneObject PhaseOneCutscene, PhaseTwoCutscene;

    [SerializeField]
    GameObject PunchHelpIndication;

    [SerializeField]
    Rigidbody AnimatorRigidBody;

    [SerializeField]
    CutsceneTrigger PhaseTwoCutsceneTrigger;
    [SerializeField]
    float delayShowHelp = 1.5f;

    [HideInInspector]
    public bool inSecondPhase = false; // When we enter second phase, set to true

    private void Start()
    {
        PhaseOneCutscene.OnCutsceneStart += PhaseOnePunchStart;
        PhaseOneCutscene.OnCutsceneComplete += PhaseOnePunchEnd;
        PhaseTwoCutscene.OnCutsceneStart += PhaseTwoPunchStart;
        PhaseTwoCutscene.OnCutsceneComplete += PhaseTwoPunchEnd;
        PhaseTwoCutsceneTrigger.gameObject.SetActive(false);
    }

    void PhaseOnePunchStart(CutsceneObject completed)
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();
        p.AttachToRigidbody(AnimatorRigidBody);
        StartCoroutine(ShowHelp());
    }

    void PhaseOnePunchEnd(CutsceneObject completed)
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();
        p.DetachFromRigidbody(AnimatorRigidBody);
        PunchHelpIndication.SetActive(false);
        PhaseTwoCutsceneTrigger.gameObject.SetActive(true);
    }

    void PhaseTwoPunchStart(CutsceneObject completed)
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();
        p.AttachToRigidbody(AnimatorRigidBody);
        StartCoroutine(ShowHelp());
    }

    void PhaseTwoPunchEnd(CutsceneObject completed)
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();
        p.DetachFromRigidbody(AnimatorRigidBody);
        PunchHelpIndication.SetActive(false);
    }
    IEnumerator ShowHelp()
    {
        yield return new WaitForSeconds(delayShowHelp);
        PunchHelpIndication.SetActive(true);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Turn on punching for the duration of while the player is in the zone
            BananaPunch bp = other.GetComponentInParent<BananaPunch>();
            if (bp != null)
            {
                bp.canPunch = true;
            }

        }
        if (other.CompareTag("PunchHitBox"))
        {
            if (inSecondPhase)
            {
                // Play punched animation
                // evilTwinAnimator.Play("Punched");
                print("OOF! (Evil banana was punched)");
            } 
            else
            {
                print("Ouch! (Evil Banana Punched");

                // Do glass particles 
                glassParticle.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Turn off punching for the duration of while the player is in the zone
            BananaPunch bp = other.GetComponentInParent<BananaPunch>();
            if (bp != null)
            {
                bp.canPunch = false;
            }
        }
    }
}
