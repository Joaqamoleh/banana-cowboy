using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableCannon : MonoBehaviour
{
    [SerializeField]
    SoundPlayer sfxPlayer;

    [SerializeField]
    Renderer[] renderers;

    [SerializeField]
    Collider[] colliders;

    bool isBroken = false;


    private void Start()
    {
        sfxPlayer = GetComponent<SoundPlayer>();
    }
    public void BreakCannon()
    {
        isBroken = true;
        StartCoroutine(KillMe());
    }

    public bool IsBroken()
    {
        return isBroken;
    }

    IEnumerator KillMe()
    {
        sfxPlayer.PlaySFX("Break");
        foreach (var r in renderers) {
            Destroy(r);
        }
        foreach (var c in colliders)
        {
            Destroy(c);
        }
        yield return new WaitForSeconds(sfxPlayer.GetSFX("Break").audioClip.length + 0.1f);
        Destroy(gameObject);
    }
}
