using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineMovement : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    SplineContainer spline;

    [SerializeField]
    Transform objectToMove;

    [SerializeField]
    float speed = 10f, curPos = 0f;

    float splineLength;

    [SerializeField, Tooltip("Set true to play on start")]
    bool active = false;


    public void OnValidate()
    {
        if (spline != null)
        {
            splineLength = spline.CalculateLength();
        }

        if (objectToMove != null && splineLength != 0f)
        {
            objectToMove.SetPositionAndRotation(spline.EvaluatePosition(curPos / splineLength), Quaternion.LookRotation(spline.EvaluateTangent(curPos / splineLength), objectToMove.up));
        }
    }

    private void Awake()
    {
        if (objectToMove == null)
        {
            objectToMove = transform;
        }
    }



    public void ResetMovement()
    {
        curPos = 0;
    }

    public void SetSplineActive(bool active)
    {
        this.active = active;
    }

    public bool IsComplete()
    {
        return curPos >= splineLength || !active;
    }

    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            if (curPos <= splineLength)
            {
                objectToMove.SetPositionAndRotation(spline.EvaluatePosition(curPos / splineLength), Quaternion.LookRotation(spline.EvaluateTangent(curPos / splineLength), objectToMove.up));
                curPos += speed * Time.deltaTime;
            }
            else
            {
                objectToMove.SetPositionAndRotation(spline.EvaluatePosition(1f), Quaternion.LookRotation(spline.EvaluateTangent(1f), objectToMove.up));
            }
        }
    }
}
