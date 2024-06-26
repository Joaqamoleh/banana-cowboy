using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Collectable : MonoBehaviour
{
    [SerializeField]
    DetectionTriggerHandler rangeTrigger, collectTrigger;

    [SerializeField]
    Collider collectHitbox;

    [SerializeField]
    Renderer[] renderers;

    [SerializeField]
    private Type type;
    public enum Type
    {
        STAR,
        HEALTH,
    }

    public enum Source
    {
        LEVEL,
        ENEMY,
        OTHER
    }

    public Source src = Source.LEVEL;

    [SerializeField]
    float timeToReachPlayer = 1.5f;
    [SerializeField]
    EasingsLibrary.FunctionName easingFunctionToUse;
    EasingsLibrary.Function easingFunction;

    [Header("Pickup VFX")]
    public ParticleSystem pickupVFX;

    float timeEnteredRange = 0f;
    Vector3 positionKey;
    SoundPlayer sfxPlayer = null;
    float destroyObjectDelay = 1.0f;

    private Transform targetToFollow = null;
    private bool bossBattle;

    private void Awake()
    {
        easingFunction = EasingsLibrary.GetFunction(easingFunctionToUse);
        positionKey = transform.position;
        if (src == Source.LEVEL)
        {
            if (!LevelData.starSparkleObjectTemp.ContainsKey(positionKey))
            {
                LevelData.starSparkleObjectTemp.Add(positionKey, false);
            }
            else if (LevelData.starSparkleObjectTemp[positionKey])
            {
                Destroy(gameObject);
                return;
            }
        }
        else if (SceneManager.GetActiveScene().name.Contains("Boss"))
        {
            targetToFollow = GameObject.Find("Player Prefab").transform;
            bossBattle = true;
        }
        Debug.Assert(rangeTrigger != null);
        Debug.Assert(collectTrigger != null);
        sfxPlayer = GetComponent<SoundPlayer>();
        rangeTrigger.OnTriggerEntered += RangeEntered;
        collectTrigger.OnTriggerEntered += CollectEntered;
    }

    private void RangeEntered(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            targetToFollow = c.transform;
            timeEnteredRange = Time.time;
        }
    }

    private void CollectEntered(Collider c)
    {
        if (c.CompareTag("Player"))
        {
            rangeTrigger.OnTriggerEntered -= RangeEntered;
            targetToFollow = null;
            if (sfxPlayer != null)
            {
                sfxPlayer.PlaySFX("Collect");
                destroyObjectDelay = sfxPlayer.GetSFX("Collect").audioClip.length;
            }
            foreach (var r in renderers)
            {
                Destroy(r);
            }
            // play VFX
            if (pickupVFX != null) {
                Instantiate(pickupVFX, transform.position, transform.rotation);
            }
            collectHitbox.enabled = false;
            // Collectable Effect
            if (type == Type.STAR)
            {
                LevelData.AddStarSparkles();
                UIManager.UpdateStars(true);
                if (src == Source.LEVEL)
                {
                    if (LevelData.starSparkleObjectTemp != null && LevelData.starSparkleObjectTemp.ContainsKey(positionKey))
                    {
                        LevelData.starSparkleObjectTemp[positionKey] = true;
                    }
                }
            }

            StartCoroutine(DelayedDeath());
        }
    }

    private void Update()
    {
        if (targetToFollow != null)
        {
            if (bossBattle) // Some cases where the star is too high to reach
            {
                transform.position += 25 * Time.deltaTime * (targetToFollow.position - transform.position).normalized;
            }
            else
            {
                float t = Mathf.Clamp((Time.time - timeEnteredRange) / timeToReachPlayer, 0f, 1f);
                transform.position += (targetToFollow.position - transform.position) * easingFunction(t);
            }
            //float dist = Vector3.Distance(targetToFollow.position, transform.position);
            //float a = 4f;
            //if (dist != 0)
            //{
            //    float t = dist / (dist + a);
            //    transform.position += (targetToFollow.position - transform.position).normalized * t * 10f;
            //}
        }
    }

    IEnumerator DelayedDeath()
    {
        yield return new WaitForSeconds(destroyObjectDelay);
        Destroy(gameObject);
    }


    //public enum TYPE
    //{
    //    STAR,
    //    HEALTH
    //}

    //public enum SOURCE 
    //{ 
    //    ENEMY,
    //    LEVEL
    //}

    //public TYPE typeOfCollectable;
    //public SOURCE locationCameFrom;
    //private bool pickedUp = false;
    //private Vector3 positionKey;
    //private SoundPlayer _sp;

    //private void Awake()
    //{
    //    if (SOURCE.ENEMY != locationCameFrom)
    //    {
    //        positionKey = transform.position;
    //        if (!LevelData.starSparkleObjectTemp.ContainsKey(positionKey))
    //        {
    //            LevelData.starSparkleObjectTemp.Add(positionKey, false);
    //        }
    //        else
    //        {
    //            pickedUp = LevelData.starSparkleObjectTemp[positionKey];
    //        }
    //        if (pickedUp)
    //        {
    //            GetComponent<Renderer>().enabled = false;
    //            GetComponent<Collider>().enabled = false;
    //            transform.parent.GetComponentInParent<CollectableFollow>().follow = false;
    //            transform.parent.parent.GetComponentInChildren<ParticleSystem>().Stop();
    //        }
    //    }
    //    _sp = GetComponent<SoundPlayer>();
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        if (_sp != null)
    //        {
    //            _sp.PlaySFX("Collect");
    //        }
    //        if (TYPE.STAR == typeOfCollectable)
    //        {
    //            LevelData.starSparkleTemp++;
    //            UIManager.UpdateStars();
    //        }
    //        else if (TYPE.HEALTH == typeOfCollectable)
    //        {
    //            other.GetComponentInParent<Health>().Damage(-1, Vector3.zero);
    //        }

    //        GetComponent<Renderer>().enabled = false;
    //        GetComponent<Collider>().enabled = false;
    //        if (SOURCE.ENEMY == locationCameFrom)
    //        {
    //            transform.root.GetComponentInChildren<ParticleSystem>().Stop();

    //            Invoke("KillMe", 1.0f);
    //        }
    //        else if (SOURCE.LEVEL == locationCameFrom)
    //        {
    //            pickedUp = true;
    //            if (LevelData.starSparkleObjectTemp != null && LevelData.starSparkleObjectTemp.ContainsKey(positionKey))
    //            {
    //                LevelData.starSparkleObjectTemp[positionKey] = true;
    //            }
    //            transform.parent.GetComponentInParent<CollectableFollow>().follow = false;
    //            transform.parent.parent.GetComponentInChildren<ParticleSystem>().Stop();
    //        }
    //    }
    //}

    //void KillMe()
    //{
    //    Destroy(transform.root.gameObject);
    //}
}
