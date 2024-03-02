using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LassoRenderer : MonoBehaviour
{
    [SerializeField]
    HingeJoint lassoStrandPrefab; // Needs a rigidbody and hingeJoint component. On the Rope physics layer
    List<HingeJoint> lassoStrandJoints;

    int hipLoopInterval = 6; // Every 6 joints, we attach the lassoStrand to the hip joint

}
