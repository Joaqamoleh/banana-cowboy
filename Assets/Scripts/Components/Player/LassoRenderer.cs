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
    Material lowPowMat, medPowMat, highPowMat, swingMat;

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
    Vector3 prevLoopCenter, loopStart, lassoTossPos;
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
            case PlayerController.LassoState.TOSS:
                RenderLassoToss(); break;
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
        if (curInfo.state == PlayerController.LassoState.HOLD || curInfo.state == PlayerController.LassoState.SWING)
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

    void RenderLassoToss()
    {
        prevLoopCenter = lassoTossPos;
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

    public void SetTossArrowDirection(Vector3 pos, Vector3 direction, LassoTossable.TossStrength strength)
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
        Quaternion rot = Quaternion.LookRotation(direction, o.GetLassoCenterBasis().up);
        lassoTossIndicator.transform.SetPositionAndRotation(pos, rot);
        lassoTossPos = pos;
    }

    public void SetSwingArrowDirection(Vector3 pos)
    {
        SwingableObject o = curInfo.o as SwingableObject;
        Material indicatorMat = swingMat;
        foreach (Renderer r in lassoTossIndicatorRenderers)
        {
            r.material = indicatorMat;
        }

        Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(o.GetApproximateSwingDirection(), hipJoint.up).normalized, o.GetLassoCenterBasis().up);
        lassoTossIndicator.transform.SetPositionAndRotation(pos, rot);
        lassoTossPos = pos;
    }
}
