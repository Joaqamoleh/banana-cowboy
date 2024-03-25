using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class BlenderBoss : MonoBehaviour
{
    [Header("Phases")]
    private readonly int _totalPhases = 2;
    private int _currentPhase = 1;
    public static int temp = 1;
    bool canBeDamaged;

    [Header("Climbing")]
    public GameObject climbObjects;

    [Header("Atacks")]
    private readonly int moves = 4;
    private int currMove;
    public GameObject[] positions;
    public GameObject juiceProjectile;
    private readonly int juiceProjectileAmount = 2; // how many times blender does this attack
    private int juiceProjectileCount = 0;

    public GameObject blenderBlade;

    public Transform platform;
    public GameObject bombIndicator;
    public GameObject blueberryBombObject;
    private Vector3[] bombSpawnPos; // positions of where a bomb will drop
    private GameObject[] indicatorSpawnObject; // spawn indicator images
    public GameObject splatEffect;

    private GameObject[] spawnPoints;
    public GameObject[] minions;
    public GameObject[] cherrySpawnPoints;
    public GameObject cherryBomb;

    [Header("Cooldown")]
    public float move1Cooldown;
    public float move2Cooldown;
    public float move3Cooldown;
    public float move4Cooldown;
    private float cooldownTimer;

    public Animator modelAnimator;
    public Animator healthAnimator;
    public Animator playerAnimator;

    public GameObject origin;
    public GameObject originSpawningEnemies;
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
    private string[] attackAnnouncement = { "Get ready to be juiced!", "Prepare for a whirlwind of flavor!", "Prepare for a fruity downpour!", "Feel the force of the fruity horde!" };
    private string[] attackName = { "Juice Jet, coming your way!", "Blender Blade!", "Blueberry Bomb Blitz, coating the ground in dangerous juice!", "Minions, assemble! Cherry Bombs, rain destruction!" };
    public Coroutine currentDialog;

    [Header("Material")]
    public Material orangeSpawn;
    public enum BossStates
    {
        IDLE, JUICE, BLADE, BOMB, SPAWN, COOLDOWN, NONE
    };

    private void Start()
    {
        state = BossStates.IDLE; // Change this to idle when finshed with moves
        dialogHolder.SetActive(false);
        //        CutsceneManager.Instance().OnCutsceneEnd += CutsceneEnd;

        health = maxHealth;
        currMove = 0;
        _currentPhase = temp;

        player = GameObject.FindWithTag("Player");
        bombSpawnPos = new Vector3[6];
        indicatorSpawnObject = new GameObject[6];
        healthHolder.SetActive(true);
        climbObjects.SetActive(false);
        canBeDamaged = true;
    }

    void CutsceneEnd(CutsceneObject o)
    {
        healthHolder.SetActive(true);
        //StartCoroutine(JuiceJam()); // Give a pause before boss battle starts
    }

    bool moveToPosition;
    void JuiceAttack()
    {
        cooldownTimer = move1Cooldown - ((_currentPhase - 1) % _totalPhases) * 2;
        state = BossStates.COOLDOWN;
        juiceProjectileCount = 0;
        juiceProjectile.SetActive(false);
        moveToPosition = true;
        StartCoroutine(MoveToPosition(transform.position, origin.transform.position));
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

        yield return new WaitForSeconds(_currentPhase % 2f + 0.5f);

        doneMoving = false;
        modelAnimator.Play("BL_Juice_Attack_Start"); // TODO: setactive in animation event

        juiceProjectile.SetActive(true);
        if (positions.Length > 0)
        {
            targetPosition.y = transform.position.y;
            yield return MoveToPosition(transform.position, targetPosition);

            doneMoving = true;
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
                modelAnimator.Play("BL_Idle");
            }
        }
    }

    IEnumerator MoveToPosition(Vector3 startPosition, Vector3 targetPosition)
    {
        float duration = Vector3.Distance(startPosition, targetPosition) / (20 + (10 * ((_currentPhase - 1) % _totalPhases)));
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
        Vector3 positionSpawned = platform.position;
        positionSpawned.y += 5;
        GameObject blade = Instantiate(blenderBlade, positionSpawned, Quaternion.identity);
        yield return new WaitForSeconds(move2Cooldown); // TODO: make it shrink before destroying
        StartCoroutine(blade.GetComponent<BlenderBlade>().ShrinkSize());
    }

    void BlueberryBombs()
    {
        cooldownTimer = move3Cooldown;
        state = BossStates.COOLDOWN;
        StartCoroutine(PlaceBombs());
    }

    IEnumerator PlaceBombs()
    {
        modelAnimator.Play("BL_StartAttack_Open_Lid");
        yield return new WaitForSeconds(3f); // When all animations are done, make this an event in Animation.

        if (bombIndicator != null && platform != null)
        {
            for (int i = 0; i < 3 * _currentPhase; i++)
            {
                Vector3 minBounds = platform.position - new Vector3(27, 0f, 32);
                Vector3 maxBounds = platform.position + new Vector3(27, 0f, 32);

                Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(minBounds.x, maxBounds.x), platform.transform.position.y + 3, UnityEngine.Random.Range(minBounds.z, maxBounds.z));

                indicatorSpawnObject[i] = Instantiate(bombIndicator, spawnPosition, Quaternion.identity);
                bombSpawnPos[i] = spawnPosition;
            }
        }
        yield return new WaitForSeconds(5f);
        ShootBombs();
    }

    void ShootBombs()
    {
        for (int i = 0; i < 3 * _currentPhase; i++)
        {
            // Assuming bombSpawnPos[i] is a RectTransform
            Vector3 screenPos = bombSpawnPos[i];

            // Convert screen position to world position
            Vector3 worldPos = screenPos;

            // Set the z-coordinate to 50f or adjust as needed
            worldPos.y = 50f;

            // Instantiate the bomb using the world position
            GameObject temp = Instantiate(blueberryBombObject, worldPos, Quaternion.identity);
            temp.GetComponent<BlueberryBomb>().pos = i;
            Destroy(indicatorSpawnObject[i]);
        }
    }

    public void CreateSplat(int obj)
    {
        indicatorSpawnObject[obj] = Instantiate(splatEffect, bombSpawnPos[obj], splatEffect.transform.rotation);
    }

    void SpawnEnemies()
    {
        /*for (int i = 0; i < spawnPoints.Length; i++) // add two more spawn points for phase 2
        {
            Vector3 spawnPosition = spawnPoints[i].transform.position;
            Instantiate(minions[0], spawnPosition, spawnPoints[i].transform.rotation);
        }*/
        StartCoroutine(MoveToPosition(transform.position, originSpawningEnemies.transform.position));
        spawnPoints = GameObject.FindGameObjectsWithTag("Statue");
        foreach (GameObject point in spawnPoints)
        {
            point.SetActive(false);
            GameObject minionInstance = Instantiate(minions[0], point.transform.position, Quaternion.identity);
            minionInstance.transform.GetChild(0).GetComponent<Renderer>().material = orangeSpawn;
            Vector3 directionToMiddle = Vector3.zero - minionInstance.transform.position;
            Quaternion rotationToMiddle = Quaternion.LookRotation(directionToMiddle, Vector3.up);
            minionInstance.transform.rotation = rotationToMiddle;
        }

        /*if (_currentPhase == 2)
        {
            for (int i = 0; i < cherrySpawnPoints.Length; i++)
            {
                Instantiate(cherryBomb, cherrySpawnPoints[i].transform.position, cherrySpawnPoints[i].transform.rotation);
            }
        }*/
        cooldownTimer = move4Cooldown + 12f;
        state = BossStates.COOLDOWN;
        StartCoroutine(RestartAttackPattern());
    }

    IEnumerator RestartAttackPattern()
    {
        modelAnimator.Play("BL_Open_Lid 0 0");
        yield return new WaitForSeconds(18f);
        modelAnimator.SetTrigger("Next");
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            print("First Phase");
            temp = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            print("Second Phase");
            temp = 2;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

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
                else if (currMove % moves == 3)
                {
                    state = BossStates.SPAWN;
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
            case BossStates.SPAWN:
                PlayDialogue(attackName[currMove % moves], true);
                SpawnEnemies();
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

    public int GetPhase()
    {
        return _currentPhase;
    }

    public void Damage(int dmg)
    {
        if (canBeDamaged)
        {
            //SoundManager.Instance().PlaySFX("BossHurt");
            modelAnimator.Play("BL_Damage");
            health -= dmg;

            healthAnimator.SetTrigger("DamageWeak"); // in case we want to make weak spots have diff anim
            healthUI.fillAmount = health / (1.0f * maxHealth);
            StartCoroutine(FlashDamage());
            if (dmg == 1)
            {
                ScreenShakeManager.Instance.ShakeCamera(2, 1, 0.1f);
            }
            if (health <= 0)
            {
                if (_currentPhase == 1)
                {
                    DizzySetup();
                }
                else if (_currentPhase == 2)
                {
                    // Play final cutscene
                }
            }
            /*        if (health <= 0)
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
                    }*/
        }
    }

    void DizzySetup()
    {
        currMove = -1;
        state = BossStates.NONE;
        juiceProjectile.SetActive(false);
        StopAllCoroutines();
        modelAnimator.SetTrigger("Dizzy");
        StartCoroutine(MoveToPosition(transform.position, origin.transform.position));
        climbObjects.SetActive(true);
        PlayDialogue("Ouchie Wowchie", false);
        // Do this when punching is implemented
        if (false)
        {
            _currentPhase = 2;
            print("Starting phase 2");
            StartCoroutine(GoToSecondPhase());
        }
    }

    IEnumerator GoToSecondPhase()
    {
        canBeDamaged = false;
        float currentFillAmount = 0;
        PlayDialogue("You've honed your skills since last time, impressive! Brace yourself as I unleash the full might of the Blender!", false);
        yield return new WaitForSeconds(3f);
        while (currentFillAmount / (maxHealth * 1.0f) != 1)
        {
            currentFillAmount = Mathf.MoveTowards(currentFillAmount, maxHealth, 0.8f * Time.deltaTime);
            healthUI.fillAmount = currentFillAmount / maxHealth;
            yield return null;
        }
        canBeDamaged = true;
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


