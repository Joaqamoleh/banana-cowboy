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

    private GameObject[] spawnPointsOrange;
    public GameObject[] spawnPointsBlueberry;
    public GameObject[] minions;
    public GameObject fruitInHand;
    public GameObject blueberryMinion;

    public CameraHint cameraOrienter;
    public Transform bladeCameraTarget;
    public Vector3 defaultCameraSettings = new Vector3(40, 12, 12);
    public Vector3 bladeCameraSettings = new Vector3(50, 40, 30);

    // Not sure if needed. Keep in case.
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
    private SoundPlayer _soundPlayer;

    [Header("Material")]
    public Material orangeColorSpawn;
    public Material blueColorSpawn;
    public Material firstPhaseColor;
    public Material secondPhaseColor;
    public Material secondPhaseColorGlass;
    public Renderer[] blenderLimbs;
    public Renderer blenderGlass;

    [Header("Celebration VFX")]
    public ParticleSystem confettiVFX;
    public enum BossStates
    {
        IDLE, JUICE, BLADE, BOMB, SPAWN, COOLDOWN, NONE
    };

    private void Start()
    {
        state = BossStates.NONE;
        dialogHolder.SetActive(false);
        CutsceneManager.Instance().GetCutsceneByName("Intro").OnCutsceneComplete += IntroCutsceneEnd;
        cameraOrienter.SetCameraValues(defaultCameraSettings.x, defaultCameraSettings.y, defaultCameraSettings.z, false);
        health = maxHealth;
        currMove = 0;
        _currentPhase = temp;

        player = GameObject.FindWithTag("Player");
        bombSpawnPos = new List<Vector3>();
        indicatorSpawnObject = new GameObject[6];
        healthHolder.SetActive(false);
        SetClimbObjectsActive(false);
        canBeDamaged = true;
        spawnPointsOrange = GameObject.FindGameObjectsWithTag("Statue- Orange");
        spawnPointsBlueberry = GameObject.FindGameObjectsWithTag("Statue- Blueberry");

        _soundPlayer = GetComponent<SoundPlayer>();
        Debug.Assert(_soundPlayer != null);

        // hide win UI at the beginning
        foreach (Transform child in youWinUI.transform)
        {
            child.transform.localScale = Vector3.zero;
        }

        CutsceneManager.Instance().GetCutsceneByName("PunchPhaseTwo").OnCutsceneComplete += CutsceneEndPunching;
        CutsceneManager.Instance().GetCutsceneByName("PunchPhaseOne").OnCutsceneComplete += CutsceneEndPunching;
        CutsceneManager.Instance().GetCutsceneByName("Blender Death").OnCutsceneComplete += BlenderDeathCutsceneComplete;
        CutsceneManager.Instance().GetCutsceneByName("Celebration").OnCutsceneComplete += CelebrationComplete;
    }

    void IntroCutsceneEnd(CutsceneObject o)
    {
        introDialogComplete = true;
        healthHolder.SetActive(true);
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
        juiceProjectileAmount = _currentPhase == 1 ? 2 : 3;
        cooldownTimer = move1Cooldown;
        cooldownTimer -= _currentPhase == 1 ? 2 : 8;
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
                if (_currentPhase == 1)
                {
                    foreach (GameObject point in spawnPointsOrange)
                    {
                        point.SetActive(true);
                    }
                }
                else
                {
                    foreach (GameObject point in spawnPointsBlueberry)
                    {
                        point.SetActive(true);
                    }
                }

                for (int i = 0; i < indicatorSpawnObject.Length; i++)
                {
                    if (indicatorSpawnObject[i] != null)
                    {
                        Destroy(indicatorSpawnObject[i]);
                    }
                }
                cameraOrienter.SetCameraValues(bladeCameraSettings.x, bladeCameraSettings.y, bladeCameraSettings.z, true);
                cameraOrienter.overrideCameraTarget = bladeCameraTarget;
            }
        }
    }

    // This is so we can call from animation events (time it better)
    // True means play, False means stop
    public void BlenderSFXManager(string sfx, bool sfxEvent)
    {
        if (sfxEvent)
        {
            _soundPlayer.PlaySFX(sfx);
        }
        else
        {
            _soundPlayer.StopSFX(sfx);
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
        modelAnimator.Play("BL_Blade_Windup");
        cameraOrienter.SetCameraValues(bladeCameraSettings.x, bladeCameraSettings.y, bladeCameraSettings.z, true);
        cameraOrienter.overrideCameraTarget = bladeCameraTarget;
    }

    public void BlenderSpinHelper()
    {
        StartCoroutine(BladeSpin());
    }

    IEnumerator BladeSpin()
    {
        Vector3 positionSpawned = platform.position;
        positionSpawned.y += 5;
        GameObject blade = Instantiate(blenderBlade, positionSpawned, Quaternion.identity);
        _soundPlayer.PlaySFX("BlenderBlade");
        yield return new WaitForSeconds(move2Cooldown);
        StartCoroutine(blade.GetComponent<BlenderBlade>().ShrinkSize());
        _soundPlayer.StopSFX("BlenderBlade");
        yield return new WaitForSeconds(1);
        cameraOrienter.SetCameraValues(defaultCameraSettings.x, defaultCameraSettings.y, defaultCameraSettings.z, true);
        cameraOrienter.overrideCameraTarget = null;
        //blade.GetComponent<BlenderBlade>().ShrinkSize();
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
            Vector3 previousPlayerPosition = player.transform.position;

            for (int i = 0; i < 3 * _currentPhase; i++)
            {
                if (_currentPhase == 1)
                {
                    SpawnRandomPosition();
                }
                else
                {
                    Vector3 currentPlayerPosition = player.transform.position;
                    if ((currentPlayerPosition != previousPlayerPosition || bombSpawnPos.Count == 0) && playerHealth.GetCanTakeDamage())
                    {
                        spawnPosition = currentPlayerPosition;
                        spawnPosition.y = platform.transform.position.y + 3;
                        bombSpawnPos.Add(spawnPosition);

                    }
                    else
                    {
                        SpawnRandomPosition();
                    }
                    previousPlayerPosition = currentPlayerPosition;
                    yield return new WaitForSeconds(0.3f);
                }
                indicatorSpawnObject[i] = Instantiate(bombIndicator, spawnPosition, Quaternion.identity);
                yield return new WaitForSeconds(_currentPhase == 1 ? 2 : 0.25f);
                ShootBombs(i);
            }
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
        bombSpawnPos.Add(spawnPosition);
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
        _soundPlayer.PlaySFX("BlenderSpawn");
        GameObject[] spawnPoints = _currentPhase == 1 ? spawnPointsOrange : spawnPointsBlueberry;
        foreach (GameObject point in spawnPoints)
        {
            point.SetActive(false);
            GameObject minionInstance;
            if (_currentPhase == 1)
            {
                minionInstance = Instantiate(minions[0], point.transform.position, Quaternion.identity);
                minionInstance.transform.GetChild(0).GetComponent<Renderer>().material = orangeColorSpawn;
            }
            else
            {
                minionInstance = Instantiate(minions[1], point.transform.position, Quaternion.identity);
                minionInstance.transform.GetChild(0).GetComponent<Renderer>().material = blueColorSpawn;
            }
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

    public void GrabFruitMinion()
    {
        if (currMove != 2)
        {
            fruitInHand.transform.GetChild(0).gameObject.SetActive(true);

        }
        else
        {
            fruitInHand.transform.GetChild(1).gameObject.SetActive(true);
        }
    }

    public void HideFruitMinionInHand()
    {
        foreach (Transform child in fruitInHand.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!introDialogComplete) { return; }
        //Test purposes
/*        if (Input.GetKeyDown(KeyCode.L))
        {
            Damage(1);
        }*/
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

    public void PlayDialogue(string dialog, bool announcingAttack, float time = 3)
    {
        _soundPlayer.PlaySFX("Laugh");
        // Also have talk, but idk when you want that to play
        // soundPlayer.PlaySFX("Talk");
        if (currentDialog != null)
        {
            StopCoroutine(currentDialog);
        }
        currentDialog = StartCoroutine(PlayDialogueHelper(dialog, announcingAttack, time));
    }

    IEnumerator PlayDialogueHelper(string dialog, bool announcingAttack, float time)
    {
        dialogText.text = "";
        dialogHolder.SetActive(true);
        if (announcingAttack)
        {
            dialogText.text = attackAnnouncement[currMove % moves] + " ";
        }
        dialogText.text += dialog;
        yield return new WaitForSeconds(time);
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

            healthAnimator.SetTrigger("DamageWeak");
            healthUI.fillAmount = health / (1.0f * maxHealth);
            StartCoroutine(FlashDamage());
            ScreenShakeManager.Instance.ShakeCamera(6, 3, 0.5f);

            if (health <= 0)
            {
                ScreenShakeManager.Instance.ShakeCamera(8, 3, 1f);
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
        healthHolder.SetActive(false);
        // Destroy objects when transitioning
        juiceProjectile.SetActive(false);
        GameObject[] tempObjects = GameObject.FindGameObjectsWithTag("BossAttacks");
        foreach (GameObject obj in tempObjects)
        {
            Destroy(obj);
        }

        _soundPlayer.StopSFX("BlenderJuice");

        HideFruitMinionInHand();
        StopAllCoroutines();
        _soundPlayer.PlaySFX("Dizzy");
        modelAnimator.Play("BL_Dizzy_Loop");
        StartCoroutine(MoveToPosition(transform.position, origin.transform.position));
        SetClimbObjectsActive(true);
        if (_currentPhase == 1)
        {
            PlayDialogue("Ahh curses! You insolent fruit! I'll make you all pay for this!", false);
        }
        else
        {
            PlayDialogue("No, this can't be happening! Bested by a Banana with a hat and a lasso??", false);
        }
        // Might be a case where you can't make it to the boss if it covers the front.
        for (int i = 0; i < indicatorSpawnObject.Length; i++)
        {
            if (indicatorSpawnObject[i] != null)
            {
                Destroy(indicatorSpawnObject[i]);
            }
        }
        if (_currentPhase == 2)
        {
            _soundPlayer.PlaySFX("BlenderDeath");
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().StopAllMusic();
            }
        }
    }

    void CutsceneEndPunching(CutsceneObject o)
    {
        if (_currentPhase == 1)
        {
            _currentPhase = 2;
            playerHealth.Damage(-3, Vector3.zero); // Heal player back to full. TODO: Not sure if we'll have healing items
            StartCoroutine(GoToSecondPhase());
        }
        else
        {
            LevelData.BeatLevel();

            // TODO: Before the celebration, kill on the enemies present in the scene
            EnemyController[] allEnemies = GameObject.FindObjectsOfType<EnemyController>();
            foreach (EnemyController e in allEnemies)
            {
                e.KillEnemy(EnemyController.DeathSource.OTHER, false, true);
            }
            // Play final cutscenes
            FindAnyObjectByType<PlayerAnimator>().IgnorePlayerStateChange();
            CutsceneManager.Instance().PlayCutsceneByName("Blender Death");

            /*            // playerModel.SetActive(true);
                        player.transform.position = playerWinLocation.transform.position;
                        player.transform.rotation = playerWinLocation.transform.rotation;
                        playerAnimator.applyRootMotion = true;
                        playerAnimator.SetLayerWeight(1, 0.0f);
                        // play confetti particle system here :3
                        if (confettiVFX != null) {
                            Instantiate(confettiVFX, new Vector3(playerWinLocation.transform.position.x, playerWinLocation.transform.position.y + 25,
                                playerWinLocation.transform.position.z), playerWinLocation.transform.rotation); 
                        }
                        playerAnimator.Play("Base Layer.BC_Cheer");

                        // CutsceneManager.Instance().GetCutsceneByName("Blender Death").OnCutsceneComplete += BlenderDeathCutsceneComplete;
                        // CutsceneManager.Instance().PlayCutsceneByName("Blender Death");*/
        }
    }

    void SetPlayerPos()
    {
        // playerModel.SetActive(true);
        playerModel.transform.position = playerWinLocation.transform.position;
        playerModel.transform.rotation = playerWinLocation.transform.rotation;
        playerAnimator.applyRootMotion = true;
        playerAnimator.SetLayerWeight(1, 0.0f);
        // play confetti particle system here :3
        if (confettiVFX != null)
        {
            Instantiate(confettiVFX, new Vector3(playerWinLocation.transform.position.x, playerWinLocation.transform.position.y + 25,
                playerWinLocation.transform.position.z), playerWinLocation.transform.rotation);
        }
        playerAnimator.Play("Base Layer.BC_Cheer");

        // CutsceneManager.Instance().GetCutsceneByName("Blender Death").OnCutsceneComplete += BlenderDeathCutsceneComplete;
        // CutsceneManager.Instance().PlayCutsceneByName("Blender Death");
    }

    void BlenderDeathCutsceneComplete(CutsceneObject o)
    {
        CutsceneManager.Instance().PlayCutsceneByName("Celebration");
        StartCoroutine(winUIAnimation());
        SetPlayerPos();
    }

    void CelebrationComplete(CutsceneObject o)
    {
        ComicCutsceneManager.comicSelect = false;
        ComicSelectManager.SetComicUnlock("Cutscene3", true);
        LevelSwitch.ChangeScene("Cutscene3");
    }

    IEnumerator GoToSecondPhase()
    {
        canBeDamaged = false;
        SetClimbObjectsActive(false);
        yield return new WaitForSeconds(1);
        modelAnimator.Play("BL_Recover");
        yield return new WaitForSeconds(1);
        blenderGlass.material = normalColor;
        healthHolder.SetActive(true);
        modelAnimator.Play("BL_Idle");
        float currentFillAmount = 0;
        PlayDialogue("You have gotten stronger since last time, impressive! Brace yourself as I unleash the full might of the Blender!", false, 5);
        yield return new WaitForSeconds(5f);

        StartCoroutine(ChangeColor());
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

    IEnumerator ChangeColor()
    {
        for (int i = 0; i < 5; i++)
        {
            foreach (Renderer obj in blenderLimbs)
            {
                obj.material = secondPhaseColor;
            }

            yield return new WaitForSeconds(0.2f);

            foreach (Renderer obj in blenderLimbs)
            {
                obj.material = firstPhaseColor;
            }
            yield return new WaitForSeconds(0.2f);

        }
        foreach (Renderer obj in blenderLimbs)
        {
            obj.material = secondPhaseColor;
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
        blenderGlass.material = hurtColor;
        yield return new WaitForSeconds(0.3f);
        blenderGlass.material = normalColor;
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