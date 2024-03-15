using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlenderBoss : MonoBehaviour
{
    [Header("Atacks")]
    private readonly int moves = 3;
    private int currMove;
    public GameObject[] positions;
    public GameObject juiceProjectile;
    private readonly int juiceProjectileAmount = 2; // how many times blender does this attack
    private int juiceProjectileCount = 0;

    public GameObject blenderBlade;

    public Transform platform;
    public GameObject bombIndicator;

    [Header("Cooldown")]
    public float move1Cooldown;
    public float move2Cooldown;
    public float move3Cooldown;
    private float cooldownTimer;

    public Animator modelAnimator;
    public Animator attackAnimator;
    public Animator healthAnimator;
    public Animator playerAnimator;

    public GameObject origin;
    public GameObject player;
    public GameObject playerModel;
    public GameObject playerWinLocation;

    public BossStates state;

    [Header("Damage")]
    public int maxHealth;
    private int health;
    public GameObject healthHolder;
    public Image healthUI;
    public Material normalColor;
    public Material hurtColor;

    [Header("Dialogue")]
    public GameObject dialogHolder;
    public TMP_Text dialogText;
    private string[] attackAnnouncement = { "Get ready to be juiced!", "Prepare for a whirlwind of flavor!", "Feel the force of the fruity horde!" };
    private string[] attackName = { "Juice Jet, coming your way!", "Blender Blitz!", "Minions, assemble! Cherry Bombs, rain destruction!" };
    public Coroutine currentDialog;

    public enum BossStates
    {
        IDLE, JUICE, BLADE, BOMB, COOLDOWN, NONE
    };

    private void Start()
    {
        state = BossStates.IDLE; // Change this to idle when finshed with moves
        dialogHolder.SetActive(false);
//        CutsceneManager.Instance().OnCutsceneEnd += CutsceneEnd;

        health = maxHealth;
        currMove = 2;

        player = GameObject.FindWithTag("Player");

       // healthHolder.SetActive(true);
    }

    void CutsceneEnd(CutsceneObject o)
    {
        healthHolder.SetActive(true);
        //StartCoroutine(JuiceJam()); // Give a pause before boss battle starts
    }

    bool moveToPosition;
    void JuiceAttack()
    {
        cooldownTimer = move1Cooldown;
        state = BossStates.COOLDOWN;
        juiceProjectileCount = 0;
        juiceProjectile.SetActive(false);
        moveToPosition = true;
        StartCoroutine(JuiceJam(positions[juiceProjectileCount].transform.position));
    }

    public bool doneMoving = true;
    IEnumerator JuiceJam(Vector3 targetPosition)
    {
        juiceProjectileCount++;

        if (moveToPosition)
        {
            yield return MoveToPosition(transform.position, positions[1].transform.position);
        }

        yield return new WaitForSeconds(2f);

        doneMoving = false;
        juiceProjectile.SetActive(true);
        attackAnimator.SetTrigger("JuiceAttack");
        if (positions.Length > 0)
        {
            targetPosition.y = transform.position.y;
            yield return MoveToPosition(transform.position, targetPosition);

            doneMoving = true;
            attackAnimator.SetTrigger("StopJuice");
            if (juiceProjectileCount != juiceProjectileAmount)
            {
                moveToPosition = false;
                juiceProjectile.SetActive(false);
                StartCoroutine(JuiceJam(positions[juiceProjectileCount].transform.position));
            }
            else
            {
                juiceProjectile.SetActive(false);
                StartCoroutine(MoveToPosition(transform.position, origin.transform.position));
            }
        }
        else
        {
            Debug.LogWarning("Target object is not assigned!");
        }
    }

    IEnumerator MoveToPosition(Vector3 startPosition, Vector3 targetPosition)
    {
        float duration = Vector3.Distance(startPosition, targetPosition) / 20;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }

    void BlenderBlades()
    {
        cooldownTimer = move2Cooldown;
        state = BossStates.COOLDOWN;
        StartCoroutine(BladeSpin());
    }

    IEnumerator BladeSpin()
    {
        print("SPINNING BLADE");
        GameObject blade = Instantiate(blenderBlade);
        yield return new WaitForSeconds(move2Cooldown); // TODO: make it shrink before destroying
        Destroy(blade);
    }

    void BlueberryBombs()
    {
        cooldownTimer = move3Cooldown;
        state = BossStates.COOLDOWN;
        StartCoroutine(PlaceBombs());
    }

    IEnumerator PlaceBombs()
    {
        if (bombIndicator != null && platform != null)
        {
            for (int i = 0; i < 3; i++)
            {
                // Calculate boundaries based on platform position and spawn area size
                Vector3 minBounds = platform.position - new Vector3(26, 0f, 26);
                Vector3 maxBounds = platform.position + new Vector3(26, 0f, 26);

                // Generate a random position within the boundaries
                Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(minBounds.x, maxBounds.x), bombIndicator.transform.position.y, UnityEngine.Random.Range(minBounds.z, maxBounds.z));

                // Spawn the object at the random position
                Instantiate(bombIndicator, spawnPosition, Quaternion.identity);
            }
        }
        else
        {
            Debug.LogWarning("Please assign objectToSpawn and platform in the inspector.");
        }
        yield return new WaitForSeconds(1f);
    }

    IEnumerator ShootBombs()
    {
        yield return new WaitForSeconds(1f);
    }

    private void Update()
    {
        switch (state)
        {
            case BossStates.IDLE:
                if (currMove % moves == 0)
                {
                    state = BossStates.JUICE;
                }
                else if (currMove % moves == 1)
                {
                    state = BossStates.BLADE;
                }
                else if (currMove % moves == 2)
                {
                    state = BossStates.BOMB;
                }
                break;
            case BossStates.JUICE:
                PlayDialogue(attackName[currMove % moves], true);
                JuiceAttack();
                break;
            case BossStates.BLADE:
                PlayDialogue(attackName[currMove % moves], true);
                BlenderBlades();
                break;
            case BossStates.BOMB:
                PlayDialogue(attackName[currMove % moves], true);
                BlueberryBombs();
                break;
            case BossStates.COOLDOWN:
                Cooldown();
                break;
            default:
                break;
        }
    }

    public void PlayDialogue(string dialog, bool announcingAttack)
    {
        if (currentDialog != null)
        {
            StopCoroutine(currentDialog);
        }
        currentDialog = StartCoroutine(PlayDialogueHelper(dialog, announcingAttack));
    }

    IEnumerator PlayDialogueHelper(string dialog, bool announcingAttack)
    {
        dialogText.text = "";
        dialogHolder.SetActive(true);
        if (announcingAttack)
        {
            dialogText.text = attackAnnouncement[currMove % moves] + " ";
        }
        dialogText.text += dialog;
        yield return new WaitForSeconds(3f);
        dialogHolder.SetActive(false);
    }

    void Cooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0)
        {
            currMove++;
            state = BossStates.IDLE;
        }
    }

    public void Damage(int dmg)
    {
        //SoundManager.Instance().PlaySFX("BossHurt");
        health -= dmg;
        if (health == maxHealth - 1 || health == maxHealth - 2)
        {
            PlayDialogue("Ha! You got lucky, punk! But don't think it's gonna be that easy!", false);
        }
        healthAnimator.SetTrigger("DamageWeak"); // in case we want to make weak spots have diff anim
        healthUI.fillAmount = health / (1.0f * maxHealth);
        StartCoroutine(FlashDamage());
        if (dmg == 1)
        {
            ScreenShakeManager.Instance.ShakeCamera(2, 1, 0.1f);
        }
        else
        {
            PlayDialogue("Ouch! Right in the juice box! You got some skills, but I'm not going down that easy!", false);
            ScreenShakeManager.Instance.ShakeCamera(6, 4, 1.5f);
        }
        if (health <= 0)
        { 

            // play cutscene
            CutsceneManager.Instance().PlayCutsceneByName("Win");

            // disable player controls and move player to a specific spot
            playerModel.transform.position = playerWinLocation.transform.position;
            playerModel.transform.rotation = playerWinLocation.transform.rotation;
            playerAnimator.applyRootMotion = true;
            playerAnimator.SetLayerWeight(1, 0.0f);
            playerAnimator.Play("Base Layer.BC_Cheer");


            // play boss animations
            modelAnimator.SetTrigger("Death");
            enabled = false;
            // TODO: GO TO SOME SORT OF WIN SCREEN. FOR NOW GO TO MAIN MENU
            // LevelSwitch.ChangeScene("Menu");
            CutsceneManager.Instance().OnCutsceneEnd += FinalCutsceneEnd;
        }
    }

    void FinalCutsceneEnd(CutsceneObject o)
    {
        LevelSwitch.ChangeScene("Menu");
    }

    // might be over kill. might 
    void BossDeathSetup()
    {
        dialogHolder.SetActive(false);

        StopAllCoroutines(); // stop all coroutines 'MUST DO THIS'
        currMove = -1; // make none of the moves work
    }

    IEnumerator FlashDamage()
    {
        yield return new WaitForSeconds(0.3f);
    }
}
