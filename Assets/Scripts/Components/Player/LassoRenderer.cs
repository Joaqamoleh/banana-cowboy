using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Mathf;

public class LassoRenderer : MonoBehaviour
{
    LineRenderer lineRenderer;

    [SerializeField]
    Transform hipJoint, handJoint;

    [SerializeField]
    GameObject lassoTossIndicator;

    [SerializeField]
    Renderer[] lassoTossIndicatorRenderers;

    [SerializeField]
    Material lowPowMat, medPowMat, highPowMat;

    [SerializeField]
    float hipLoopRadius = 0.4f, smoothSpeed = 0.01f, wiggleSpeed = 0.2f, wiggleAmplitude = 0.2f, loopWiggleSpeed = 0.2f;

    [SerializeField, Range(0f, 90f)]
    float loopWiggleDegree = 20f;

    [SerializeField, Min(1)]
    int numLoopSegments = 12, numLineSegments = 12, numHipLoops = 3;

    Vector3[] segmentPos;
    Vector3[] segmentVel;

    [SerializeField]
    LayerMask lassoLayerMask;

    public struct LassoInfo
    {
        public PlayerController.LassoState state;
        public LassoObject o;
        public float timeForAction;
        public Transform lassoSwingCenter;
    }
    LassoInfo curInfo;
    float curTime;
    Vector3 prevLoopCenter, loopStart;
    Quaternion prevLoopRot, loopStartRot;
    float prevLoopRadius, loopStartRadius;

    /// Rendering for the lasso throw
    /// 1. Arc to the target, making it look like it was thrown to land on the target
    /// 2. Constrict lasso loop around the target
    /// 3. Send a ripple back down the rope to the player which ends the throw
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        Debug.Assert(lineRenderer != null);
        Debug.Assert(lassoTossIndicator != null);

        segmentPos = new Vector3[numLoopSegments + numLineSegments];
        segmentVel = new Vector3[numLoopSegments + numLineSegments];

        lineRenderer.positionCount = numLineSegments + numLoopSegments;        
        curInfo = new LassoInfo();
        curInfo.o = null;
        curInfo.state = PlayerController.LassoState.NONE;
    }
    private void Start()
    {
        lassoTossIndicator.SetActive(false);
        ActiveRender(true);
        SetLassoRenderState(curInfo);
    }

    private void LateUpdate()
    {
        if (segmentPos.Length != lineRenderer.positionCount) { return; }
        curTime += Time.deltaTime;
        switch(curInfo.state)
        {
            case PlayerController.LassoState.NONE:
                RenderLassoHip(); break;
            case PlayerController.LassoState.RETRACT:
                RenderLassoRetract(); break;
            case PlayerController.LassoState.SWING: 
                RenderLassoSwing(); break;
            case PlayerController.LassoState.THROWN:
                RenderLassoThrow(); break;
            case PlayerController.LassoState.PULL:
                RenderLassoPull(); break;
            case PlayerController.LassoState.HOLD:
                RenderLassoHold(); break;
        }
        lineRenderer.SetPositions(segmentPos);
    }

    public void SetLassoRenderState(LassoInfo lassoInfo)
    {
        curInfo = lassoInfo;
        switch (curInfo.state)
        {
            case PlayerController.LassoState.NONE:
                InitHipPos();
                break;
            case PlayerController.LassoState.RETRACT:
                loopStart = prevLoopCenter;
                loopStartRot = prevLoopRot;
                loopStartRadius = prevLoopRadius;
                break;
            case PlayerController.LassoState.THROWN:
                curInfo.o.GetLassoCenterBasis().LookAt(transform.position, hipJoint.up);
                loopStart = prevLoopCenter;
                loopStartRot = prevLoopRot;
                loopStartRadius = prevLoopRadius;
                break;
        }
        if (curInfo.state == PlayerController.LassoState.HOLD)
        {
            ShowTossArrow();
        }
        else
        {
            HideTossArrow();
        }
        curTime = 0.0f;
    }

    void InitHipPos()
    {
        int totalSegments = numLoopSegments + numLineSegments;
        int segmentsInLoop = (totalSegments) / numHipLoops;

        for (int loop = 0; loop < numHipLoops; loop++)
        {
            int o = loop * segmentsInLoop;
            float delta = (PI * 2f) / (segmentsInLoop - 1f);
            for (int i = 0; i < segmentsInLoop; i++)
            {
                float angle = delta * i;
                segmentPos[i + o] = GetTargetHipPos(angle);
            }
        }
        lineRenderer.SetPositions(segmentPos);
    }

    void RenderLassoHip()
    {
        int totalSegments = numLoopSegments + numLineSegments;
        int segmentsInLoop = (totalSegments) / numHipLoops;
        float segmentLength = Sin((PI * 2f) / (segmentsInLoop - 1f)) * hipLoopRadius;

        // Blend to resting
        for (int loop = 0; loop < numHipLoops; loop++)
        {
            int o = loop * segmentsInLoop;

            segmentPos[o] = GetTargetHipPos(0);
            segmentPos[segmentsInLoop - 1 + o] = GetTargetHipPos(2f * PI + 0.1f);
            float delta = (PI * 2f) / (segmentsInLoop - 1f);
            for (int seg = 1; seg < segmentsInLoop - 1; seg++)
            {
                float angle = delta * seg;
                Vector3 loopTarget = GetTargetHipPos(angle);
                Vector3 blendTarget = segmentPos[seg - 1 + o] + (segmentPos[seg + o] - segmentPos[seg - 1 + o]).normalized * segmentLength;
                float blend = EasingsLibrary.EaseOutBack((1.0f - Cos(angle)) / 2f) * 0.8f;
                Vector3 target = Vector3.Lerp(loopTarget, blendTarget, blend);
                segmentPos[seg + o] = Vector3.SmoothDamp(segmentPos[seg + o], target, ref segmentVel[seg + o], smoothSpeed);
            }
        }
        prevLoopCenter = hipJoint.position;
        prevLoopRot = Quaternion.LookRotation(hipJoint.forward, hipJoint.right);
        prevLoopRadius = hipLoopRadius;
    }

    void RenderLassoRetract()
    {
        InterpolateTowardsTarget(hipLoopRadius, loopStart, hipJoint.position, Quaternion.LookRotation(hipJoint.forward, hipJoint.right), curTime / curInfo.timeForAction);
    }

    void RenderLassoThrow()
    {
        InterpolateTowardsTarget(curInfo.o.GetAppoximateLassoRadius(), handJoint.position, curInfo.o.GetLassoCenterPos(), curInfo.o.GetLassoCenterBasis().rotation, curTime / curInfo.timeForAction);
    }

    void RenderLassoPull()
    {
        InterpolateTowardsTarget(curInfo.o.GetAppoximateLassoRadius(), handJoint.position, curInfo.o.GetLassoCenterPos(), curInfo.o.GetLassoCenterBasis().rotation, curTime / curInfo.timeForAction);
    }

    void RenderLassoHold()
    {
        if (curInfo.o == null || curInfo.o as LassoTossable == null) { return; }

        Vector3 loopCenter = curInfo.o.GetLassoCenterPos();
        Transform basis = curInfo.o.GetLassoCenterBasis();
        basis.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane((curInfo.lassoSwingCenter.position - basis.position).normalized, hipJoint.up), hipJoint.up);
        Quaternion loopRot = curInfo.o.GetLassoCenterBasis().rotation;

        Vector3 basisUp = loopRot * Vector3.up;
        Vector3 basisRight = loopRot * Vector3.right;
        Vector3 basisForward = loopRot * Vector3.forward;

        Vector3 lassoLoopStartPos = Vector3.zero;
        Vector3[] lassoLoopPositions = new Vector3[numLoopSegments];
        LoopAroundTarget(curInfo.o, ref lassoLoopPositions, ref prevLoopRadius);
        for (int i = 0; i < numLoopSegments; i++)
        {
            if (i == 0)
            {
                lassoLoopStartPos = lassoLoopPositions[i];
            }
            segmentPos[i + numLineSegments] = lassoLoopPositions[i];
        }

        segmentPos[0] = handJoint.position;
        segmentPos[numLineSegments - 1] = lassoLoopStartPos;
        for (int i = numLineSegments - 2; i > 0; i--)
        {
            float radius = (curInfo.o as LassoTossable).GetSwingRadius();
            float percentAlongLine = (float)i / (numLineSegments - 1);
            float t = Sin(percentAlongLine * 1.3f * PI);
            Vector3 swingPos = handJoint.position * (1.0f - percentAlongLine) + lassoLoopStartPos * percentAlongLine + radius * t * basisForward;
            Vector3 targetPos = swingPos * (1.0f - percentAlongLine) + percentAlongLine * segmentPos[i + 1];
            segmentPos[i] = Vector3.SmoothDamp(segmentPos[i], targetPos, ref segmentVel[i], smoothSpeed);
        }

        prevLoopCenter = loopCenter;
        prevLoopRot = loopRot;
    }

    void RenderLassoSwing()
    {
        if (curInfo.o == null) { return; }

        Vector3 lassoLoopStartPos = Vector3.zero;
        Vector3[] lassoLoopPositions = new Vector3[numLoopSegments];
        LoopAroundTarget(curInfo.o, ref lassoLoopPositions, ref prevLoopRadius);
        for (int i = 0; i < numLoopSegments; i++)
        {
            if (i == 0)
            {
                lassoLoopStartPos = lassoLoopPositions[i];
            }
            segmentPos[i + numLineSegments] = lassoLoopPositions[i];
        }

        for (int i = 0; i < numLineSegments; i++)
        {
            float posAlongLine = ((float)i / (numLineSegments - 1));
            Vector3 lineWorldPos = posAlongLine * lassoLoopStartPos + (1.0f - posAlongLine) * handJoint.position;
            segmentPos[i] = lineWorldPos;
        }

        prevLoopCenter = curInfo.o.GetLassoCenterPos();
        prevLoopRot = curInfo.o.GetLassoCenterBasis().rotation;
    }

    void InterpolateTowardsTarget(float targetRad, Vector3 start, Vector3 targetPos, Quaternion targetRot, float t)
    {
        Vector3 loopCenter = Vector3.Lerp(loopStart, targetPos, t);
        Quaternion loopRot = Quaternion.Slerp(loopStartRot, targetRot, t);
        float loopRadius = Lerp(loopStartRadius, targetRad, t);
        Vector3 basisForward = loopRot * Vector3.forward;
        Vector3 basisRight = loopRot * Vector3.right;
        Vector3 basisUp = loopRot * Vector3.up;

        // Create loop
        // At theta == 0, position uses only forward vector
        // At theta == PI / 2 position uses only right vector
        segmentPos[numLineSegments] = GetPosOnCircle(0, loopRadius, basisRight, basisForward, loopCenter);
        segmentPos[numLineSegments + numLoopSegments - 1] = GetPosOnCircle(0, loopRadius, basisRight, basisForward, loopCenter);
        float delta = (PI * 2f) / (numLoopSegments - 1f);
        for (int i = 1; i < numLoopSegments - 1; i++)
        {
            float angle = delta * i;
            Vector3 target = GetPosOnCircle(angle, loopRadius, basisRight, basisForward, loopCenter);
            segmentPos[i + numLineSegments] = Vector3.SmoothDamp(segmentPos[i + numLineSegments], target, ref segmentVel[i + numLineSegments], smoothSpeed);
        }

        for (int i = 0; i < numLineSegments; i++)
        {
            float percentAlongLine = i / (numLineSegments - 1f);
            segmentPos[i] = start * (1.0f - percentAlongLine) + percentAlongLine * segmentPos[numLineSegments]
                + basisForward * Sin(t * wiggleSpeed * 2 * PI + percentAlongLine * 4 * PI) * wiggleAmplitude
                + basisRight * Cos(t * wiggleSpeed * 2 * PI + percentAlongLine * 4 * PI) * wiggleAmplitude
                + basisUp * Sin(t * wiggleSpeed * 2 * PI + percentAlongLine * 4 * PI) * wiggleAmplitude;
        }

        prevLoopCenter = loopCenter;
        prevLoopRadius = loopRadius;
        prevLoopRot = loopRot;
    }

    void LoopAroundTarget(LassoObject o, ref Vector3[] loopPositions, ref float largestRadiusDist)
    {
        float raycastTestRadius = o.GetAppoximateLassoRadius();
        Vector3 loopCenterPos = o.GetLassoCenterPos();
        Transform basis = o.GetLassoCenterBasis();

        for (int i = 0; i < loopPositions.Length; i++)
        {
            float gamma = (float)i / (loopPositions.Length - 1) * PI * 2f;
            Vector3 loopSegmentPos = new Vector3(
                raycastTestRadius * Sin(gamma),
                0f,
                raycastTestRadius * Cos(gamma));

            loopSegmentPos = loopSegmentPos.x * basis.right + loopSegmentPos.z * basis.forward + loopCenterPos;

            RaycastHit hit;
            if (Physics.Raycast(loopSegmentPos, (loopCenterPos - loopSegmentPos).normalized, out hit, raycastTestRadius + 10f, lassoLayerMask, QueryTriggerInteraction.Ignore))
            {
                loopPositions[i] = hit.point + (hit.point - loopCenterPos).normalized * 0.2f;
            }
            else
            {
                loopPositions[i] = loopSegmentPos;
            }

            if (Vector3.Distance(loopPositions[i], loopCenterPos) > largestRadiusDist)
            {
                largestRadiusDist = Vector3.Distance(loopPositions[i], loopCenterPos);
            }
        }
    }

    Vector3 GetTargetHipPos(float angle)
    {
        //return Cos(angle) * hipLoopRadius * hipJoint.up +
        //       Sin(angle) * hipLoopRadius * hipJoint.forward + hipJoint.position;
        return GetPosOnCircle(angle, hipLoopRadius, hipJoint.forward, hipJoint.up, hipJoint.position);
    }

    Vector3 GetPosOnCircle(float angle, float radius, Vector3 up, Vector3 right, Vector3 pos)
    {
        return Cos(angle) * radius * right + 
               Sin(angle) * radius * up + pos;
    }

    static void PerformFABRIK(ref Vector3[] positions, float[] distances, Vector3 root, Vector3 target, int iterations, float tolerance)
    {
        float totalDist = Vector3.Distance(root, target);
        float reach = distances.Sum();
        if (totalDist > reach) 
        {
            // Out of reach
            for (int i = 0; i < positions.Length - 2; i++)
            {
                float r = Vector3.Distance(target, positions[i]);
                float gamma = distances[i] / r;
                positions[i + 1] = (1 - gamma) * positions[i] + target * gamma;
            }
        }
        else
        {
            Vector3 b = positions[0];
            int iter = 0;
            float dif = Vector3.Distance(positions[positions.Length - 1], target);
            while (dif > tolerance && iter < iterations)
            {
                // Forward Reaching
                for (int i = positions.Length - 2; i >= 0; i--)
                {
                    float r = Vector3.Distance(positions[i], positions[i + 1]);
                    float gamma = distances[i] / r;
                    positions[i] = (1 - gamma) * positions[i + 1] + gamma * positions[i];
                }
                positions[0] = b;
                for (int i = 0; i < positions.Length - 2; i++)
                {
                    float r = Vector3.Distance(positions[i], positions[i + 1]);
                    float gamma = distances[i] / r;
                    positions[i + 1] = (1 - gamma) * positions[i] + gamma * positions[i + 1];
                }
                dif = Vector3.Distance(positions[positions.Length - 1], target);
                iter++;
            }
        }
    }

    public void ActiveRender(bool render)
    {
        lineRenderer.positionCount = render ? numLineSegments + numLoopSegments : 0;
    }

    public void ShowTossArrow()
    {
        lassoTossIndicator.SetActive(true);
    }

    public void HideTossArrow()
    {
        lassoTossIndicator.SetActive(false);
    }

    public void SetTossArrowDirection(Vector3 direction, LassoTossable.TossStrength strength)
    {
        LassoTossable o = curInfo.o as LassoTossable;
        if (o == null) 
        { 
            HideTossArrow(); 
            return; 
        }

        Material indicatorMat = lowPowMat;
        if (strength == LassoTossable.TossStrength.STRONG)
        {
            indicatorMat = highPowMat;
        }
        else if (strength == LassoTossable.TossStrength.MEDIUM)
        {
            indicatorMat = medPowMat;
        }

        foreach (Renderer r in lassoTossIndicatorRenderers)
        {
            r.material = indicatorMat;
        }
        Vector3 pos = o.GetLassoCenterPos() + direction * o.GetSwingRadius();
        Quaternion rot = Quaternion.LookRotation(direction, o.GetLassoCenterBasis().up);
        lassoTossIndicator.transform.SetPositionAndRotation(pos, rot);
    }

    //// right is camera right, so should always be relatively close to expected right
    //public void RenderThrowLasso(float t, LassoObject o, float angleRatio)
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }
    //    Transform basis = o.GetLassoCenterBasis();

    //    Vector3 start = lassoHandJointPos.position;
    //    Vector3 end = o.GetLassoCenterPos();

    //    if (t == 0)
    //    {
    //        if (lassoTossIndicator.activeSelf)
    //        {
    //            lassoTossIndicator.SetActive(false);
    //        }

    //        LoopAroundTarget(o, ref lassoLoopPositions);

    //        basis.LookAt(start, basis.up);
    //        basis.Rotate(Vector3.up, 180f);
    //    }


    //    Vector3 forward = basis.forward;
    //    Vector3 up = basis.up;
    //    Vector3 right = basis.right;

    //    Vector3 loopCenterPos = Vector3.zero;
    //    float loopRadius;
    //    float dist = Vector3.Distance(start, end);
    //    float maxYHeight = (dist / 4f) * angleRatio;

    //    if (t <= throwT1Time)
    //    {
    //        float T = t / throwT1Time;
    //        if (T < 0.5f)
    //        {
    //            loopCenterPos.y = EasingsLibrary.EaseOutCubic(T * 2f) * maxYHeight;
    //        }
    //        else
    //        {
    //            loopCenterPos.y = (1f - EasingsLibrary.EaseInCubic((T * 2f - 1f))) * maxYHeight;
    //        }

    //        loopCenterPos.z = EasingsLibrary.EaseOutCirc(T) * dist;

    //        loopRadius = EasingsLibrary.EaseOutBack(T) * o.lassoThrowRadius;
    //    } 
    //    else
    //    {
    //        loopCenterPos.y = 0f;
    //        loopCenterPos.z = dist;
    //        loopRadius = o.lassoThrowRadius;
    //    }

    //    Vector3 loopStartingSegmentWorld = end;


    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        float gamma = (float)i / (numberOfLoopSegments - 1) * PI * 2f;
    //        Vector3 loopSegmentPos = new Vector3(
    //            loopCenterPos.x + loopRadius * Sin(gamma),
    //            loopCenterPos.y,
    //            loopCenterPos.z + loopRadius * -Cos(gamma));

    //        Vector3 loopWorldPos = loopSegmentPos.x * right + loopSegmentPos.y * up + loopSegmentPos.z * forward + start;


    //        if (t > throwT1Time)
    //        {
    //            float T = (t - throwT1Time) / throwT2Time;
    //            float ease = EasingsLibrary.EaseInCirc(T);
    //            loopWorldPos = (1f - ease) * loopWorldPos + lassoLoopPositions[i] * ease;
    //            //print("Easing with T: " + T + " and ease " + ease + " to current: " + loopWorldPos);
    //        }

    //        if (i == 0)
    //        {
    //            loopStartingSegmentWorld = loopWorldPos;
    //        }
    //        lineRenderer.SetPosition(i + numberOfLineSegments, loopWorldPos);
    //    }

    //    for (int i = 0; i < numberOfLineSegments; i++)
    //    {
    //        float posAlongLine = ((float) i / (numberOfLineSegments - 1));
    //        Vector3 lineWorldPos = posAlongLine * loopStartingSegmentWorld + (1.0f - posAlongLine) * start;
    //        lineRenderer.SetPosition(i, lineWorldPos);
    //    }

    //    lastLoopCenterPos = loopCenterPos;
    //}

    //public void RenderLassoSwing(SwingableObject o)
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

    //    // Scrunches up the lasso based on length
    //    Vector3 loopCenterPos = o.GetLassoCenterPos();

    //    Vector3 lassoLoopStartPos = Vector3.zero;
    //    LoopAroundTarget(o, ref lassoLoopPositions);
    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        if (i == 0)
    //        {
    //            lassoLoopStartPos = lassoLoopPositions[i];
    //        }
    //        lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
    //    }

    //    for (int i = 0; i < numberOfLineSegments; i++)
    //    {
    //        float posAlongLine = ((float)i / (numberOfLineSegments - 1));
    //        Vector3 lineWorldPos = posAlongLine * lassoLoopStartPos + (1.0f - posAlongLine) * lassoHandJointPos.position;
    //        lineRenderer.SetPosition(i, lineWorldPos);
    //    }

    //    lastLoopCenterPos = loopCenterPos;
    //}

    //public void RenderLassoPull(float t, LassoTossable o, Transform lassoSwingCenter)
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

    //    if (t == 0)
    //    {
    //        if (lassoTossIndicator.activeSelf)
    //        {
    //            lassoTossIndicator.SetActive(false);
    //        }
    //    }

    //    // Scrunches up the lasso based on length
    //    Vector3 loopCenterPos = o.GetLassoCenterPos();

    //    Vector3 lassoLoopStartPos = Vector3.zero;
    //    LoopAroundTarget(o, ref lassoLoopPositions);
    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        if (i == 0)
    //        {
    //            lassoLoopStartPos = lassoLoopPositions[i];
    //        }
    //        lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
    //    }

    //    for (int i = 0; i < numberOfLineSegments; i++)
    //    {
    //        float posAlongLine = ((float)i / (numberOfLineSegments - 1));
    //        Vector3 lineWorldPos = posAlongLine * lassoLoopStartPos + (1.0f - posAlongLine) * lassoHandJointPos.position;
    //        lineRenderer.SetPosition(i, lineWorldPos);
    //    }

    //    lastLoopCenterPos = loopCenterPos;

    //}

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="t"></param> Here t ranges from [0, infinity)
    ///// <param name="o"></param>
    ///// <param name="lassoSwingCenter"></param>
    //public void RenderLassoHold(float t, LassoTossable o, Transform lassoSwingCenter, Vector3 predictedTossDirection, LassoTossable.TossStrength strength)
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }
    //    Vector3 loopCenterPos = o.GetLassoCenterPos();

    //    if (t == 0)
    //    {
    //        if (!lassoTossIndicator.activeSelf)
    //        {
    //            lassoTossIndicator.SetActive(true);
    //        }
    //    }

    //    Vector3 lassoLoopStartPos = Vector3.zero;
    //    LoopAroundTarget(o, ref lassoLoopPositions);
    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        if (i == 0)
    //        {
    //            lassoLoopStartPos = lassoLoopPositions[i];
    //        }
    //        lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
    //    }

    //    Vector3 up = lassoSwingCenter.up;
    //    Vector3 forward = Vector3.ProjectOnPlane((o.transform.position - lassoSwingCenter.position).normalized, up);
    //    float dist = Vector3.Distance(o.transform.position, lassoHandJointPos.position);
    //    //print("Up is " + up + " forward " + forward);
    //    for (int i = 0; i < numberOfLineSegments; i++)
    //    {
    //        if (i ==  numberOfLineSegments - 1)
    //        {
    //            lassoLinePositions[i] = lassoLoopStartPos;
    //        }
    //        else if (i == 0)
    //        {
    //            lassoLinePositions[i] = lassoHandJointPos.position;
    //        }
    //        else
    //        {
    //            //float percentAlongLine = EasingsLibrary.EaseOutBack((float) i / (numberOfLineSegments - 1));
    //            float percentAlongLine = (float) i / (numberOfLineSegments - 1);
    //            float radius = o.GetSwingRadius();
    //            lassoLinePositions[i] = dist * percentAlongLine * up + lassoSwingCenter.position + 2f * radius * EasingsLibrary.EaseInOutBack(percentAlongLine) * forward;
    //            lassoLinePositions[i - 1] = (1f - percentAlongLine) * lassoLinePositions[i - 1] + percentAlongLine * lassoLinePositions[i];
    //        }
    //        lineRenderer.SetPosition(i, lassoLinePositions[i]);
    //    }

    //    Material indicatorMat = lowPowMat;
    //    if (strength == LassoTossable.TossStrength.STRONG)
    //    {
    //        indicatorMat = highPowMat;
    //    }
    //    else if (strength == LassoTossable.TossStrength.MEDIUM)
    //    {
    //        indicatorMat = medPowMat;
    //    }

    //    foreach (Renderer r in lassoTossIndicatorRenderers)
    //    {
    //        r.material = indicatorMat;
    //    }

    //    lassoTossIndicator.transform.position = loopCenterPos + predictedTossDirection * o.GetSwingRadius();
    //    lassoTossIndicator.transform.rotation = Quaternion.LookRotation(predictedTossDirection, up);

    //    lastLoopCenterPos = loopCenterPos;
    //}



    //public void RenderLassoToss(float t, LassoTossable o, Transform lassoSwingCenter)
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

    //    if (t == 0)
    //    {
    //        if (lassoTossIndicator.activeSelf)
    //        {
    //            lassoTossIndicator.SetActive(false);
    //        }
    //        ResetLassoVelocities();
    //    }

    //    Vector3 loopCenterPos = o.GetLassoCenterPos() - t * lassoSwingCenter.up;

    //    Vector3 lassoLoopStartPos = Vector3.zero;
    //    Vector3[] targets = new Vector3[numberOfLoopSegments];
    //    LoopAroundTarget(o, ref targets);
    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        lassoLoopPositions[i] = Vector3.SmoothDamp(lassoLoopPositions[i], targets[i], ref lassoLoopVelocities[i], 0.07f);
    //        if (i == 0)
    //        {
    //            lassoLoopStartPos = lassoLoopPositions[i];
    //        }
    //        lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
    //    }

    //    Vector3 up = lassoSwingCenter.up;
    //    Vector3 forward = Vector3.ProjectOnPlane((loopCenterPos - lassoSwingCenter.position).normalized, up);
    //    float dist = Vector3.Project(loopCenterPos, up).magnitude - Vector3.Project(lassoSwingCenter.position, up).magnitude;

    //    for (int i = 0; i < numberOfLineSegments; i++)
    //    {
    //        Vector3 target;
    //        float percentAlongLine = EasingsLibrary.EaseOutBack((float)i / (numberOfLineSegments - 1));
    //        if (i == numberOfLineSegments - 1)
    //        {
    //            target = lassoLoopStartPos;
    //        }
    //        else if (i == 0)
    //        {
    //            target = lassoHandJointPos.position;
    //        }
    //        else
    //        {
    //            float radius = o.GetSwingRadius();
    //            target = dist * percentAlongLine * up + lassoSwingCenter.position + 2f * radius * EasingsLibrary.EaseInOutBack(percentAlongLine) * forward;
    //        }
    //        lassoLinePositions[i] = Vector3.SmoothDamp(lassoLinePositions[i], target, ref lassoLineVelocities[i], 0.07f);

    //        lineRenderer.SetPosition(i, lassoLinePositions[i]);
    //    }

    //    lastLoopCenterPos = loopCenterPos;
    //}

    //public void RenderLassoRetract(float t, Transform lassoSwingCenter, float totalTime)
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

    //    if (t == 0)
    //    {

    //        if (lassoTossIndicator.activeSelf)
    //        {
    //            lassoTossIndicator.SetActive(false);
    //        }
    //        ResetLassoVelocities();
    //    }

    //    Vector3 loopCenterPos = Vector3.Lerp(lastLoopCenterPos, lassoHandJointPos.position, t);
    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        lassoLoopPositions[i] = Vector3.SmoothDamp(lassoLoopPositions[i], hipJointPos.position, ref lassoLoopVelocities[i], totalTime / 5f);
    //        lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);

    //    }

    //    for (int i = 0; i < numberOfLineSegments; i++)
    //    {
    //        lassoLinePositions[i] = Vector3.SmoothDamp(lassoLinePositions[i], hipJointPos.position, ref lassoLineVelocities[i], totalTime / 5f);
    //        lineRenderer.SetPosition(i, lassoLinePositions[i]);

    //    }


    //    if (t >= 1f)
    //    {
    //        lastLoopCenterPos = loopCenterPos;
    //    }
    //}


    //public void RenderLassoAtHip()
    //{
    //    if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }
    //    // Loop lasso around joint of hip
    //    Vector3 loopTopPos = hipJointPos.position + hipJointPos.up * hipRadius;
    //    Vector3 loopCenterPos = hipJointPos.position;

    //    if (lassoTossIndicator.activeSelf)
    //    {
    //        lassoTossIndicator.SetActive(false);
    //    }

    //    // Use lastLoop center for an approximation of the speed we are moving at
    //    Vector3 swayVel = lastLoopCenterPos - loopCenterPos;


    //    Vector3 loopStartingSegmentWorld = loopCenterPos;

    //    // TODO: Revist, will require springs and tracking previous velocity
    //    for (int i = 0; i < numberOfLoopSegments; i++)
    //    {
    //        float gamma = (float)i / (numberOfLoopSegments - 1) * PI * 2f;
    //        Vector3 loopPos = loopCenterPos 
    //            + hipJointPos.up * hipRadius * Sin(gamma) 
    //            + hipJointPos.forward * hipRadius * Cos(gamma);
    //        //float swayAmount = Clamp(swayVel.magnitude, 0f, hipMaxSway) * Clamp(Vector3.Distance(loopPos, hipJointPos.position) / (hipRadius * 2f), 0f, 1f); // Might need to normalize this distance in some way based on radius
    //        //lastLoopVelocities[i] = Vector3.ClampMagnitude(-swayVel.normalized * swayAmount;
    //        if (i == 0)
    //        {
    //            loopStartingSegmentWorld = loopPos;
    //        }
    //        lineRenderer.SetPosition(i + numberOfLineSegments, loopPos);
    //    }

    //    for (int i = numberOfLineSegments - 1; i >= 0; i--)
    //    {
    //        float gamma = ((float)i / (numberOfLineSegments - 1) * numCyclesOnHip) * PI * 2f;
    //        Vector3 linePos = loopCenterPos
    //            + hipJointPos.up * hipRadius * Sin(gamma)
    //            + hipJointPos.forward * hipRadius * Cos(gamma);

    //        if (i == numberOfLineSegments - 1)
    //        {
    //            linePos = loopStartingSegmentWorld;
    //        }
    //        lineRenderer.SetPosition(i, linePos);
    //    }


    //    lastLoopCenterPos = loopCenterPos;
    //}

    void ResetLassoVelocities()
    {
        for (int i = 0; i < segmentVel.Length; i++)
        {
            segmentVel[i] = Vector3.zero;
        }
    }
}
