using UnityEngine;

public class CarpetUnroll : MonoBehaviour
{
    [SerializeField]
    Transform[] curveObjects;

    [SerializeField]
    float unrollSpeed = 30f;

    Vector3[] eulerAngles;

    void Start()
    {
    }

    void OnEnable()
    {
        if (eulerAngles == null)
        {
            eulerAngles = new Vector3[curveObjects.Length];
            for (int i = 0; i < curveObjects.Length; i++)
            {
                eulerAngles[i] = curveObjects[i].localEulerAngles;
            }
        }
        foreach (Transform t in curveObjects)
        {
            t.localEulerAngles = new Vector3(-60f, 0f, 0f);
        }
    }

    void Update()
    {
        for (int i = 0; i < curveObjects.Length; i++)
        {
            Transform t = curveObjects[i];
            Vector3 angle = eulerAngles[i];
            //print("Rolling towards: " + angle + " from " + t.localEulerAngles.x + " for " + i);
            float newX = Mathf.MoveTowardsAngle(t.localEulerAngles.x, angle.x, unrollSpeed * Time.deltaTime);
            t.localEulerAngles = new Vector3(newX, 0f, 0f);
        }
    }
}
