using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrangeBoss : MonoBehaviour
{
    [Header("Atacks")]
    public GameObject orangeSliceBoomerangs;
    public GameObject minions;
    private readonly int moves = 3;
    private int currMove;
    public bool indicating = false;
    public bool boomerangSpinning = false;

    [Header("Cooldown")]
    public float boomerangCooldown;
    public float spawnCooldown;
    public float peelCooldown;
    public float peelAnimationTime;
    private float cooldownTimer;

    public Animator modelAnimator;
    public Animator healthAnimator;
    public Animator playerAnimator;

    public GameObject[] spawnPoints;
    public GameObject origin;
    public GameObject player;
    public GameObject playerModel;
    public List<GameObject> boomerangObjects;
    public List<GameObject> weakSpots;
    public GameObject playerWinLocation;

    public BossStates state;

    [Header("Damage")]
    public int maxHealth;
    private int health;
    public GameObject healthHolder;
    public Image healthUI;
    public Material normalColor;
    public Material hurtColor;
    public GameObject[] partsOfModel;

    public enum BossStates
    {
        IDLE, BOOMERANG, PEEL, SPAWN, COOLDOWN, NONE
    };

    private void Start()
    {
        state = BossStates.NONE;

        CutsceneManager.Instance().OnCutsceneEnd += CutsceneEnd;
        // need to disable player controls

        // start beginning cutscene first 
        //

        //state = BossStates.PEEL;
        resetAnimations = new List<string>();
        health = maxHealth;
        currMove = 0;

        player = GameObject.FindWithTag("Player");
        indicating = false;
        boomerangSpinning = false;

    }

    void CutsceneEnd(CutsceneObject o)
    {
        healthHolder.SetActive(true);
        StartCoroutine(BoomerangStartUpHelper()); // Give a pause before boss battle starts
    }

    private void Update()
    {
        if (player != null && !indicating)
        {
            transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));
        }

        switch (state)
        {
            case BossStates.IDLE:
                if (currMove % moves == 0)
                {
                    state = BossStates.BOOMERANG;
                }
                else if (currMove % moves == 1)
                {
                    state = BossStates.SPAWN;
                }
                else if (currMove % moves == 2)
                {
                    state = BossStates.PEEL;
                }
                break;
            case BossStates.BOOMERANG:
                SpawnBoomerangs();
                break;
            case BossStates.PEEL:
                PeelSlam();
                break;
            case BossStates.SPAWN:
                SpawnEnemies();
                break;
            case BossStates.COOLDOWN:
                Cooldown();
                break;
            default:
                break;
        }
    }

    void SpawnBoomerangs()
    {
        cooldownTimer = 3.5f + boomerangCooldown;
        state = BossStates.COOLDOWN;
        StartCoroutine(BoomerangStartup());
    }

    IEnumerator SpinningBoomerangs()
    {
        while (boomerangSpinning)
        {
            yield return new WaitForEndOfFrame();
            SoundManager.Instance().PlaySFX("OrangeBossBoomerangs");
        }
    }
    IEnumerator BoomerangStartUpHelper()
    {
        yield return new WaitForSeconds(2f);
        state = BossStates.IDLE;
    }

    IEnumerator BoomerangStartup()
    {
        modelAnimator.SetTrigger("Boomerang Attack");
        yield return new WaitForSeconds(2.5f);
        for (int i = 0; i < 5; i++)
        {
            GameObject boomerangRight = SpawnBoomerang(spawnPoints[0].transform.position + spawnPoints[0].transform.right, i);
            GameObject boomerangLeft = SpawnBoomerang(spawnPoints[1].transform.position - spawnPoints[1].transform.right, i);
            boomerangLeft.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            boomerangObjects.Add(boomerangLeft);
            boomerangObjects.Add(boomerangRight);
            StartCoroutine(DestroyBoomerangs(boomerangRight, boomerangLeft));
        }
        indicating = true;
        yield return new WaitForSeconds(1.5f);
        indicating = false;
        foreach (GameObject b in boomerangObjects)
        {
            b.GetComponent<CircularMovement>().SetCollider(true);
        }
        boomerangObjects.Clear();
        boomerangSpinning = true;
        StartCoroutine(SpinningBoomerangs());
    }

    void SpawnEnemies()
    {
        // Add animation here

        SoundManager.Instance().PlaySFX("OrangeBossSummon");
        modelAnimator.SetTrigger("Spawn Orange");
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            /*Vector3 temp = UnityEngine.Random.onUnitSphere;
            Vector3 spawnPosition = transform.position + temp * UnityEngine.Random.Range(60, sizeOfArena);
            print(temp+", "+spawnPosition);
            spawnPosition.y = 0;*/

            /*            int rand = UnityEngine.Random.Range(0, 2);
            */
            Vector3 spawnPosition = spawnPoints[i].transform.position;
            Instantiate(minions, spawnPosition, transform.rotation);
            // TODO: Make substate random
        }
        cooldownTimer = spawnCooldown;
        state = BossStates.COOLDOWN;
    }

    void PeelSlam()
    {
        // add animation here
        indicating = true;
        for (int i = 0; i < reset.Length; i++)
        {
            if (!resetAnimations.Contains(reset[i]))
            {
                resetAnimations.Add(reset[i]);
            }
        }
        StartCoroutine(PeelSlamAnimation()); // want time between slices
        cooldownTimer = peelAnimationTime + peelCooldown;
        state = BossStates.COOLDOWN;
    }

    int[] nums = { 0, 1, 2, 3 }; // doing this for shuffle
    string[] triggerNames = { "LF Peel Attack", "LB Peel Attack", "RF Peel Attack", "RB Peel Attack" };
    string[] layerNames = { "Left Front Slice", "Left Back Slice", "Right Front Slice", "Right Back Slice" };
    List<string> resetAnimations;
    string[] reset = { "LF Peel Reset", "LB Peel Reset", "RF Peel Reset", "RB Peel Reset" };
    IEnumerator PeelSlamAnimation()
    {
        ShuffleArray(nums);
        modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex("Body and Arms"), 1.0f);

        for (int i = 0; i < nums.Length; i++)
        {
            modelAnimator.SetTrigger("Peel Attack"); // body and arms only
            print("CURRENTLY ON "+ layerNames[nums[i]]);
            modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex(layerNames[nums[i]]), 1.0f);
            modelAnimator.SetTrigger(triggerNames[nums[i]]);

            yield return new WaitForSeconds(3.0f);
            modelAnimator.ResetTrigger("Peel Attack");
        }
        /*       modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex(layerNames[nums[0]]), 1.0f);
               modelAnimator.SetTrigger(triggerNames[nums[0]]);
               yield return new WaitForSeconds(3.0f);

               modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex(layerNames[nums[1]]), 1.0f);
               modelAnimator.SetTrigger(triggerNames[nums[1]]);
               yield return new WaitForSeconds(3.0f);

               modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex(layerNames[nums[2]]), 1.0f);
               modelAnimator.SetTrigger(triggerNames[nums[2]]);
               yield return new WaitForSeconds(3.0f);

               modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex(layerNames[nums[3]]), 1.0f);
               modelAnimator.SetTrigger(triggerNames[nums[3]]);
               yield return new WaitForSeconds(3.0f);*/
        StartCoroutine(PeelSlamCooldown());
    }

    void ShuffleArray(int[] array)
    {
        int randomIndex;
        int temp;
        // Fisher-Yates shuffle algorithm
        for (int i = array.Length - 1; i > 0; i--)
        {
            randomIndex = Random.Range(0, i + 1);
            temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    IEnumerator PeelSlamCooldown()
    {
        yield return new WaitForSeconds(peelCooldown); // TODO: Create a list then remove if player touches peel. If still in dictionary, loop and call reset.
        modelAnimator.SetTrigger("Peel Reset");
        foreach (string temp in resetAnimations)
        {
            modelAnimator.SetTrigger(temp);
        }
        indicating = false;
        yield return new WaitForSeconds(1.5f); // set weight to 0 after animation is done playing // TODO: Maybe do same thing here
        modelAnimator.SetLayerWeight(1, 0.0f);
        modelAnimator.SetLayerWeight(2, 0.0f);
        modelAnimator.SetLayerWeight(3, 0.0f);
        modelAnimator.SetLayerWeight(4, 0.0f);
        modelAnimator.SetLayerWeight(5, 0.0f);
    }

    public void ResetPeel(int index, string nameOfCondition)
    {
        modelAnimator.SetTrigger("Peel Reset");
        modelAnimator.SetTrigger(nameOfCondition);
        resetAnimations.Remove(nameOfCondition);
        //modelAnimator.SetLayerWeight(index, 0.0f); // this messes it up maybe somehow?
        HideWeakSpot(index);
    }

    private GameObject SpawnBoomerang(Vector3 position, int radiusAdd)
    {
        GameObject boomerang = Instantiate(orangeSliceBoomerangs, position, Quaternion.identity);
        CircularMovement circularMovement = boomerang.GetComponent<CircularMovement>();
        circularMovement.target = origin.transform;
        circularMovement.direction = position.x > transform.position.x ? 1 : -1;
        circularMovement.angle = (position.x < transform.position.x ? 180f + transform.eulerAngles.y : 0f + transform.eulerAngles.y) * Mathf.Deg2Rad;
        circularMovement.radius += (radiusAdd * 7);
        circularMovement.SetCollider(false);
        return boomerang;
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

    IEnumerator DestroyBoomerangs(GameObject x, GameObject y)
    {
        yield return new WaitForSeconds(boomerangCooldown);
        Destroy(x);
        Destroy(y);
        boomerangSpinning = false;
    }

    public void Damage(int dmg)
    {
        health -= dmg;
        healthAnimator.SetTrigger("DamageWeak"); // in case we want to make weak spots have diff anim
        healthUI.fillAmount = health / (1.0f * maxHealth);
        StartCoroutine(FlashDamage());
        if (dmg == 1)
        {
            ScreenShakeManager.Instance.ShakeCamera(2, 1, 0.1f);
        }
        else
        {
            ScreenShakeManager.Instance.ShakeCamera(6, 4, 1.5f);
        }
        if (health <= 0)
        {
            print("BOSS DEFEATED");
            healthHolder.SetActive(false);
            LevelData.BeatLevel();

            // remove all other animation layer weights
            modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex("Left Front Slice"), 0.0f);
            modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex("Left Back Slice"), 0.0f);
            modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex("Right Front Slice"), 0.0f);
            modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex("Right Back Slice"), 0.0f);
            modelAnimator.SetLayerWeight(modelAnimator.GetLayerIndex("Body and Arms"), 0.0f);

            BossDeathSetup();

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
        indicating = true; // Keeps boss from moving
        boomerangSpinning = false; // stop spinning boomerangs
        state = BossStates.NONE;// put it in a state where it does nothing
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy); // Destroy all enemies stuff
        }
        HideWeakSpots(); // hide all hitboxes
        StopAllCoroutines(); // stop all coroutines 'MUST DO THIS'
        currMove = -1; // make none of the moves work
    }

    IEnumerator FlashDamage()
    {
        for (int i = 0; i < partsOfModel.Length; i++)
        {
            partsOfModel[i].GetComponent<Renderer>().material = hurtColor;
        }
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < partsOfModel.Length; i++)
        {
            partsOfModel[i].GetComponent<Renderer>().material = normalColor;
        }
        yield return new WaitForSeconds(0.3f);
    }

    public void ShowWeakSpot(int weakSpotIndex)
    {
        weakSpots[weakSpotIndex].SetActive(true);
    }

    public void HideWeakSpots()
    {
        foreach(GameObject o in weakSpots)
        {
            o.SetActive(false);
        }
    }

    public void HideWeakSpot(int weakSpotIndex)
    {
        weakSpots[weakSpotIndex].SetActive(false);
    }
}
