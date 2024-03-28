using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
public class BlenderBoss : MonoBehaviour
{
    [Header("Phases")]
    private readonly int _totalPhases = 2;
    private int _currentPhase = 1;
    public static int temp = 1;
    bool canBeDamaged;

    [Header("Climbing")]
    public GameObject[] climbObjects;

    [Header("Atacks")]
    private readonly int moves = 4;
    private int currMove;
    public GameObject[] positions;
    public GameObject juiceProjectile;
    private int juiceProjectileAmount; // how many times blender does this attack
    private int juiceProjectileCount = 0;

    public GameObject blenderBlade;

    public Transform platform;
    public GameObject bombIndicator;
    public GameObject blueberryBombObject;
    private List<Vector3> bombSpawnPos; // positions of where a bomb will drop
    private GameObject[] indicatorSpawnObject; // spawn indicator images
    public GameObject splatEffect;

    public GameObject[] bombObjectsFly;

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
    public Health playerHealth;
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
    private string[] attackName = { "Juice Jet, coming your way!", "Blender Blade!", "Blueberry Bomb Blitz, coating the ground in dangerous juice!", "Minions, assemble!" };
    public Coroutine currentDialog;

    public GameObject youWinUI;
    public GameObject thirdPersonCamera;

    private bool introDialogComplete = false;

    // For sfxs
    private SoundPlayer soundPlayer;

    [Header("Material")]
    public Material orangeSpawn;
    public enum BossStates
    {
        IDLE, JUICE, BLADE, BOMB, SPAWN, COOLDOWN, NONE
    };

    private void Start()
    {
        state = BossStates.NONE;
        dialogHolder.SetActive(false);
        CutsceneManager.Instance().GetCutsceneByName("Intro").OnCutsceneComplete += IntroCutsceneEnd;

        health = maxHealth;
        currMove = 3;
        _currentPhase = temp;
        
        player = GameObject.FindWithTag("Player");
        bombSpawnPos = new List<Vector3>();
        indicatorSpawnObject = new GameObject[6];
        healthHolder.SetActive(true);
        SetClimbObjectsActive(false);
        canBeDamaged = true;
        spawnPoints = GameObject.FindGameObjectsWithTag("Statue");

        soundPlayer = GetComponent<SoundPlayer>();
        Debug.Assert(soundPlayer != null);

        // hide win UI at the beginning
        foreach (Transform child in youWinUI.transform)
        {
            child.transform.localScale = Vector3.zero;
        }

        CutsceneManager.Instance().GetCutsceneByName("PunchPhaseTwo").OnCutsceneComplete += CutsceneEndPunching;
        CutsceneManager.Instance().GetCutsceneByName("PunchPhaseOne").OnCutsceneComplete += CutsceneEndPunching;
        CutsceneManager.Instance().GetCutsceneByName("Celebration").OnCutsceneComplete += CelebrationComplete;
    }

    void IntroCutsceneEnd(CutsceneObject o)
    {
        introDialogComplete = true;
        StartCoroutine(JuiceAttackStartUpHelper());
    }

    void SetClimbObjectsActive(bool b)
    {
        foreach (GameObject go in climbObjects)
        {
            go.SetActive(b);
        }
    }
    IEnumerator JuiceAttackStartUpHelper()
    {
        yield return new WaitForSeconds(1f);
        state = BossStates.IDLE;
    }

    bool moveToPosition;
    void JuiceAttack()
    {
        bombSpawnPos.Clear(); // Just in case
        juiceProjectileAmount = _currentPhase == 1? 2: 3;
        cooldownTimer = (move1Cooldown - ((_currentPhase - 1) % _totalPhases) * 5);
        if (_currentPhase == 2)
        {
            cooldownTimer += 4;
        }
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

        yield return new WaitForSeconds(_currentPhase % 2f);

        doneMoving = false;
        modelAnimator.Play("BL_Juice_Attack_Windup");
        yield return new WaitForSeconds(0.75f);
        juiceProjectile.SetActive(true);
        if (positions.Length > 0)
        {
            targetPosition.y = transform.position.y;
            yield return MoveToPosition(transform.position, targetPosition);

            doneMoving = true;
            //modelAnimator.SetTrigger("ResetJuice");
            modelAnimator.Play("BL_Juice_Attack_Reset");
            juiceProjectile.SetActive(false);

            if (juiceProjectileCount != juiceProjectileAmount)
            {
                moveToPosition = false;
                StartCoroutine(JuiceJam(positions[juiceProjectileCount % 2].transform.position));
            }
            else
            {
                StartCoroutine(MoveToPosition(transform.position, origin.transform.position));
                foreach (GameObject point in spawnPoints)
                {
                    point.SetActive(true);
                }
                for (int i = 0; i < indicatorSpawnObject.Length; i++)
                {
                    if (indicatorSpawnObject[i] != null)
                    {
                        Destroy(indicatorSpawnObject[i]);
                    }
                }
            }
        }
    }

    public void SettingJuiceBlast(bool val)
    {
        juiceProjectile.SetActive(val);
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
        yield return new WaitForSeconds(move2Cooldown);
        StartCoroutine(blade.GetComponent<BlenderBlade>().ShrinkSize());
    }

    void BlueberryBombs()
    {
        cooldownTimer = move3Cooldown;
        state = BossStates.COOLDOWN;
        PlaceBombs();
    }

    Vector3 spawnPosition = Vector3.zero;
    public void PlayPlaceBombs()
    {
        StartCoroutine(PlayPlaceBombsHelper());
    }
    IEnumerator PlayPlaceBombsHelper()
    {
        if (bombIndicator != null && platform != null)
        {
            for (int i = 0; i < 3 * _currentPhase; i++)
            {
                if (_currentPhase == 1)
                {
                    SpawnRandomPosition();
                }
                else
                {
                    spawnPosition = new Vector3(player.transform.position.x, 0, player.transform.position.z);
                    if (bombSpawnPos.Contains(spawnPosition))
                    {
                        SpawnRandomPosition();
                    }
                    yield return new WaitForSeconds(0.75f / _currentPhase);
                }

                indicatorSpawnObject[i] = Instantiate(bombIndicator, spawnPosition, Quaternion.identity);
                bombSpawnPos.Add(spawnPosition);
                if(_currentPhase == 1)
                {
                    yield return new WaitForSeconds(2);
                    ShootBombs(i);
                }
            }
        }
        if (_currentPhase == 2)
        {
            yield return new WaitForSeconds(3f / 2);
            ShootBombs_PhaseTwo();
        }
    }

    void PlaceBombs()
    {
        modelAnimator.Play("BL_Open_Lid_BlueBerryBomb");

    }
    public void ShootBombObjects(bool val)
    {
        if (_currentPhase == 1)
        {
            bombObjectsFly[0].SetActive(val);
        }
        else
        {
            bombObjectsFly[1].SetActive(val);
            bombObjectsFly[2].SetActive(val);
        }
    }

    void SpawnRandomPosition()
    {
        Vector3 minBounds = platform.position - new Vector3(27, 0f, 32);
        Vector3 maxBounds = platform.position + new Vector3(27, 0f, 32);
        spawnPosition = new Vector3(UnityEngine.Random.Range(minBounds.x, maxBounds.x), platform.transform.position.y + 3, UnityEngine.Random.Range(minBounds.z, maxBounds.z));
    }

    void ShootBombs(int num)
    {
        Vector3 screenPos = bombSpawnPos[num];
        Vector3 worldPos = screenPos;
        worldPos.y = 50f;
        GameObject temp = Instantiate(blueberryBombObject, worldPos, Quaternion.identity);
        temp.GetComponent<BlueberryBomb>().pos = num;
        Destroy(indicatorSpawnObject[num]);
    }

    void ShootBombs_PhaseTwo()
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
        Vector3 pos = bombSpawnPos[obj];
        pos.y = platform.transform.position.y + 3;
        GameObject tmp = Instantiate(splatEffect, pos, splatEffect.transform.rotation);
        indicatorSpawnObject[obj] = tmp;
    }

    void SpawnEnemies()
    {
        /*for (int i = 0; i < spawnPoints.Length; i++) // add two more spawn points for phase 2
        {
            Vector3 spawnPosition = spawnPoints[i].transform.position;
            Instantiate(minions[0], spawnPosition, spawnPoints[i].transform.rotation);
        }*/
        StartCoroutine(MoveToPosition(transform.position, originSpawningEnemies.transform.position));
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

    bool restarting = false;
    IEnumerator RestartAttackPattern()
    {
        modelAnimator.Play("BL_Open_Lid_Reset");
        restarting = true;
        yield return new WaitForSeconds(18f);
        restarting = false;
        modelAnimator.SetTrigger("Finished");
    }


    private void Update()
    {
        if (!introDialogComplete) { return; }

        if (Input.GetKeyDown(KeyCode.K))
        {
            print("First Phase");
            temp = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Input.GetKeyDown(KeyCode.L))
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
        soundPlayer.PlaySFX("Laugh");
        // Also have talk, but idk when you want that to play
        // soundPlayer.PlaySFX("Talk");
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
            if (!restarting)
            {
                modelAnimator.Play("BL_Damage");
            }
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
                DizzySetup();
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

        // Destroy objects when transitioning
        juiceProjectile.SetActive(false);
        GameObject[] tempObjects = GameObject.FindGameObjectsWithTag("BossAttacks");
        foreach (GameObject obj in tempObjects)
        {
            print("DESTROYING STUFF");
            Destroy(obj);
        }
        StopAllCoroutines();

        modelAnimator.Play("BL_Dizzy_Loop");
        StartCoroutine(MoveToPosition(transform.position, origin.transform.position));
        SetClimbObjectsActive(true);
        PlayDialogue("Ouchie Wowchie", false);
        // TODO: Put this somewhere else. I think this is fine. Might be a case where you can't make it to the boss if it covers the front.
        for (int i = 0; i < indicatorSpawnObject.Length; i++)
        {
            if (indicatorSpawnObject[i] != null)
            {
                Destroy(indicatorSpawnObject[i]);
            }
        }
        if (_currentPhase == 1)
        {
        }
        else
        {
            print("BLENDER DEFEATED");
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().StopAllMusic();
            }
        }
    }

    void CutsceneEndPunching(CutsceneObject o)
    {
        if(_currentPhase == 1){
            _currentPhase = 2;
            modelAnimator.Play("BL_Recover");
            playerHealth.Damage(-3, Vector3.zero); // Heal player back to full. TODO: Not sure if we'll have healing items
            StartCoroutine(GoToSecondPhase());
        }
        else
        {
            // Play final cutscene
            // LevelSwitch.ChangeScene("Level Select");
            print("CELEBRATION");
            FindAnyObjectByType<PlayerAnimator>().IgnorePlayerStateChange();
            CutsceneManager.Instance().PlayCutsceneByName("Celebration");
            StartCoroutine(winUIAnimation());
            

            // playerModel.SetActive(true);
            playerModel.transform.position = playerWinLocation.transform.position;
            playerModel.transform.rotation = playerWinLocation.transform.rotation;
            playerAnimator.applyRootMotion = true;
            playerAnimator.SetLayerWeight(1, 0.0f);
            playerAnimator.Play("Base Layer.BC_Cheer");

            // CutsceneManager.Instance().GetCutsceneByName("Blender Death").OnCutsceneComplete += BlenderDeathCutsceneComplete;
            // CutsceneManager.Instance().PlayCutsceneByName("Blender Death");
        }
    }

    void BlenderDeathCutsceneComplete(CutsceneObject o)
    {
        CutsceneManager.Instance().PlayCutsceneByName("Celebration");
    }

    void CelebrationComplete(CutsceneObject o)
    {
        LevelSwitch.ChangeScene("Level Select");
    }

    IEnumerator GoToSecondPhase()
    {
        canBeDamaged = false;
        SetClimbObjectsActive(false);
        modelAnimator.Play("BL_Idle");
        float currentFillAmount = 0;
        PlayDialogue("You've honed your skills since last time, impressive! Brace yourself as I unleash the full might of the Blender!", false);
        yield return new WaitForSeconds(3f);
        while (currentFillAmount / (maxHealth * 1.0f) != 1)
        {
            currentFillAmount = Mathf.MoveTowards(currentFillAmount, maxHealth, 0.8f * Time.deltaTime);
            healthUI.fillAmount = currentFillAmount / maxHealth;
            yield return null;
        }
        health = maxHealth;
        canBeDamaged = true;
        currMove = 0;
        state = BossStates.IDLE;
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

    IEnumerator winUIAnimation()
    {
        youWinUI.SetActive(true);
        // pause before animation
        yield return new WaitForSeconds(1.5f);

        // letters appear
        foreach (Transform child in youWinUI.transform)
        {
            child.transform.DOScale(0.9f, 1f);
            yield return new WaitForSeconds(0.1f);
        }

        // letters jump
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(2.0f);
            foreach (Transform child in youWinUI.transform)
            {
                child.DOJump(child.transform.position, 25, 1, 0.5f);
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}


