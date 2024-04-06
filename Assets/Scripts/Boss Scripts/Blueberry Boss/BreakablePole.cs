using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakablePole : MonoBehaviour
{
    [SerializeField]
    GameObject poleObject;

    [SerializeField]
    GameObject cherryBombBarrel;

    [SerializeField]
    CherryBombSpawner cherrySpawner;


    Vector3 initPos;
    Quaternion initRot;

    public void BreakPole()
    {
        poleObject.SetActive(false);
        if (cherrySpawner != null)
        {
            cherrySpawner.gameObject.SetActive(true);
        }
    }

    public void Awake()
    {
        initPos = cherryBombBarrel.transform.position;
        initRot = cherryBombBarrel.transform.rotation;
        if (cherrySpawner != null)
        {
            cherrySpawner.gameObject.SetActive(false);
        }
    }

    public void RespawnPole()
    {
        poleObject.SetActive(true);
        cherryBombBarrel.transform.SetPositionAndRotation(initPos, initRot);
        if (cherrySpawner != null)
        {
            cherrySpawner.gameObject.SetActive(false);
        }
    }

    public bool IsBroken()
    {
        return poleObject.activeSelf;
    }
}
