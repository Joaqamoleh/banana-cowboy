using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerPlatforms : MonoBehaviour
{
    private Coroutine _platformCoroutine = null;
    public Collider collisionBox;
    //public Collider gravityBox;
    public Material[] colors;

    // TODO: Fix the hitbox. IDK what it is for leaves
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (_platformCoroutine == null)
                _platformCoroutine = StartCoroutine(StartTimer());
            else
            {
                StopCoroutine(_platformCoroutine);
                _platformCoroutine = StartCoroutine(StartTimer());
            }
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            /*if (_platformCoroutine != null)
            {
                Debug.Log("Exited");
                GetComponent<Renderer>().material = colors[0];
                StopCoroutine(_platformCoroutine);
                _platformCoroutine = null;
            }*/
        }
    }

    IEnumerator StartTimer()
    {
        // TODO: Test lowering the amount
        yield return new WaitForSeconds(0.75f);
        SoundManager.Instance().PlaySFX("BreakableCrackFirst");
        GetComponent<Renderer>().material = colors[1];
        yield return new WaitForSeconds(0.75f);
        SoundManager.Instance().PlaySFX("BreakableCrackSecond");
        GetComponent<Renderer>().material = colors[2];
        yield return new WaitForSeconds(0.75f);
        SoundManager.Instance().PlaySFX("BreakableCrackLast");
        GetComponent<Renderer>().enabled = false;
        collisionBox.enabled = false;
        //gravityBox.enabled = false;
        yield return new WaitForSeconds(3);
        GetComponent<Renderer>().material = colors[0];
        GetComponent<Renderer>().enabled = true;
        collisionBox.enabled = true;
        //gravityBox.enabled = true;

    }
}
