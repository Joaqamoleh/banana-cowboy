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
        cherrySpawner.gameObject.SetActive(true);
    }

    public void Awake()
    {
        initPos = cherryBombBarrel.transform.position;
        initRot = cherryBombBarrel.transform.rotation;
        cherrySpawner.gameObject.SetActive(false);
    }

    public void RespawnPole()
    {
        poleObject.SetActive(true);
        cherrySpawner.gameObject.SetActive(false);
        cherryBombBarrel.transform.SetPositionAndRotation(initPos, initRot);
    }

    public bool IsBroken()
    {
        return poleObject.activeSelf;
    }
}
