using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class CannonController : MonoBehaviour
{
    [SerializeField]
    Transform bombIndicatorPrefab;
    Transform bombIndicatorInstance;

    [SerializeField]
    CutsceneObject introCutscene;

    [SerializeField]
    BlueberryBomb bombPrefab;

    SoundPlayer soundPlayer;

    [SerializeField]
    GravityObject playerGravity;

    [SerializeField]
    LayerMask groundMask;

    bool fireBombs = false;

    [SerializeField]
    float delayBetweenBombShots = 10f, delayBeforeSpawning = 1.5f, spawnHeight = 60f;
    float lastBombShot = 0;

    public void Update()
    {
        if (fireBombs && Time.time - lastBombShot > delayBetweenBombShots && playerGravity.IsOnGround())
        {
            StartCoroutine(FireBomb());
        }
    }

    private void Start()
    {
        introCutscene.OnCutsceneComplete += StartFirstFiring;
        soundPlayer = GetComponent<SoundPlayer>();
    }

    void StartFirstFiring(CutsceneObject i)
    {
        ShouldFireBombs(true);
    }

    IEnumerator FireBomb()
    {
        lastBombShot = Time.time;
        soundPlayer.PlaySFX("Shoot");
        Vector3 spawnIndicatorPos = playerGravity.characterOrientation.position;
        if (Physics.Raycast(playerGravity.characterOrientation.position, -playerGravity.characterOrientation.up, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
        {
            spawnIndicatorPos = hit.point;
        }
        bombIndicatorInstance = Instantiate(bombIndicatorPrefab, spawnIndicatorPos, playerGravity.characterOrientation.rotation);
        yield return new WaitForSeconds(delayBeforeSpawning);
        BlueberryBomb bombInstance = Instantiate(bombPrefab);
        bombInstance.transform.SetPositionAndRotation(bombIndicatorInstance.position + bombIndicatorInstance.up * spawnHeight, Quaternion.identity * Quaternion.FromToRotation(Vector3.up, bombIndicatorInstance.up));
        bombInstance.onExplode += DestroyIndicator;
    }

    void DestroyIndicator()
    {
        if (bombIndicatorInstance != null)
        {
            Destroy(bombIndicatorInstance.gameObject);
            bombIndicatorInstance = null;
        }
    }

    public void ShouldFireBombs(bool fireBombs)
    {
        this.fireBombs = fireBombs;
        if (fireBombs)
        {
            lastBombShot = Time.time;
        }
        else
        {
            StopAllCoroutines();
        }
    }
}
