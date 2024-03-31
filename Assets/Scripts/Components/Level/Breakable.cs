using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField]
    SoundPlayer sfxPlayer;

    [SerializeField]
    Renderer[] renderers;

    [SerializeField]
    Collider[] colliders;

    [SerializeField]
    GameObject root;

    float breakDelay = 1.0f;
    public void Break()
    {
        if (sfxPlayer != null)
        {
            sfxPlayer.PlaySFX("Break");
            breakDelay = sfxPlayer.GetSFX("Break").audioClip.length + 0.1f;
        }

        foreach (var r in renderers)
        {
            Destroy(r);
        }

        foreach (var c in colliders)
        {
            Destroy(c);
        }

        StartCoroutine(BreakDelay());
    }

    IEnumerator BreakDelay()
    {
        yield return new WaitForSeconds(breakDelay);
        Destroy(root);
    }
}
