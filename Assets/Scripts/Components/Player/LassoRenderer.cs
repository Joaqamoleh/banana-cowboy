using UnityEngine;
using static UnityEngine.Mathf;

public class LassoRenderer : MonoBehaviour
{
    LineRenderer lineRenderer;

    [SerializeField]
    Transform hipJointPos, lassoHandJointPos;
    [SerializeField]
    float hipMaxSway = 1.0f, hipRadius = 0.4f;

    [SerializeField, Min(1)]
    int numberOfLoopSegments = 10, numberOfLineSegments = 10, numCyclesOnHip = 2;

    [SerializeField, Range(0f, 1f)]
    float throwT1Time = 0.8f, throwT2Time = 0.1f, throwT3Time = 0.1f, lassoTossDrag = 0.2f;



    Vector3[] lassoLoopPositions;
    Vector3[] lassoLoopVelocities;
    Vector3[] lassoLinePositions;
    Vector3[] lassoLineVelocities;
    Vector3 lastLoopCenterPos;


    [SerializeField]
    LayerMask lassoLayerMask, groundLayermask;

    /// Rendering for the lasso throw
    /// 1. Arc to the target, making it look like it was thrown to land on the target
    /// 2. Constrict lasso loop around the target
    /// 3. Send a ripple back down the rope to the player which ends the throw
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        Debug.Assert(lineRenderer != null);
        lassoLoopPositions = new Vector3[numberOfLoopSegments];
        lassoLoopVelocities = new Vector3[numberOfLoopSegments];
        lassoLinePositions = new Vector3[numberOfLineSegments];
        lassoLineVelocities = new Vector3[numberOfLineSegments];
        lineRenderer.positionCount = numberOfLineSegments + numberOfLoopSegments;        
    }
    private void Start()
    {
    }

    public void StartRenderLasso()
    {
        lineRenderer.positionCount = numberOfLineSegments + numberOfLoopSegments;
    }

    public void StopRenderLasso()
    {
        lineRenderer.positionCount = 0;
    }
    void LoopAroundTarget(LassoObject o, ref Vector3[] loopPositions)
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
                raycastTestRadius * -Cos(gamma));

            loopSegmentPos = loopSegmentPos.x * basis.right + loopSegmentPos.y * basis.up + loopSegmentPos.z * basis.forward + loopCenterPos;

            RaycastHit hit;
            if (Physics.Raycast(loopSegmentPos, (loopCenterPos - loopSegmentPos).normalized, out hit, raycastTestRadius + 10f, lassoLayerMask, QueryTriggerInteraction.Ignore))
            {
                loopPositions[i] = hit.point + (hit.point - loopCenterPos).normalized * 0.2f;
            }
            else
            {
                loopPositions[i] = loopSegmentPos;
            }
        }
    }

    // right is camera right, so should always be relatively close to expected right
    public void RenderThrowLasso(float t, LassoObject o, float angleRatio)
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }
        Transform basis = o.GetLassoCenterBasis();

        Vector3 start = lassoHandJointPos.position;
        Vector3 end = o.GetLassoCenterPos();

        if (t == 0)
        {
            LoopAroundTarget(o, ref lassoLoopPositions);
            
            basis.LookAt(start, basis.up);
            basis.Rotate(Vector3.up, 180f);
        }


        Vector3 forward = basis.forward;
        Vector3 up = basis.up;
        Vector3 right = basis.right;

        Vector3 loopCenterPos = Vector3.zero;
        float loopRadius;
        float dist = Vector3.Distance(start, end);
        float maxYHeight = (dist / 4f) * angleRatio;

        if (t <= throwT1Time)
        {
            float T = t / throwT1Time;
            if (T < 0.5f)
            {
                loopCenterPos.y = EasingsLibrary.EaseOutCubic(T * 2f) * maxYHeight;
            }
            else
            {
                loopCenterPos.y = (1f - EasingsLibrary.EaseInCubic((T * 2f - 1f))) * maxYHeight;
            }

            loopCenterPos.z = EasingsLibrary.EaseOutCirc(T) * dist;

            loopRadius = EasingsLibrary.EaseOutBack(T) * o.lassoThrowRadius;
        } 
        else
        {
            loopCenterPos.y = 0f;
            loopCenterPos.z = dist;
            loopRadius = o.lassoThrowRadius;
        }

        Vector3 loopStartingSegmentWorld = end;


        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            float gamma = (float)i / (numberOfLoopSegments - 1) * PI * 2f;
            Vector3 loopSegmentPos = new Vector3(
                loopCenterPos.x + loopRadius * Sin(gamma),
                loopCenterPos.y,
                loopCenterPos.z + loopRadius * -Cos(gamma));

            Vector3 loopWorldPos = loopSegmentPos.x * right + loopSegmentPos.y * up + loopSegmentPos.z * forward + start;


            if (t > throwT1Time)
            {
                float T = (t - throwT1Time) / throwT2Time;
                float ease = EasingsLibrary.EaseInCirc(T);
                loopWorldPos = (1f - ease) * loopWorldPos + lassoLoopPositions[i] * ease;
                //print("Easing with T: " + T + " and ease " + ease + " to current: " + loopWorldPos);
            }

            if (i == 0)
            {
                loopStartingSegmentWorld = loopWorldPos;
            }
            lineRenderer.SetPosition(i + numberOfLineSegments, loopWorldPos);
        }

        for (int i = 0; i < numberOfLineSegments; i++)
        {
            float posAlongLine = ((float) i / (numberOfLineSegments - 1));
            Vector3 lineWorldPos = posAlongLine * loopStartingSegmentWorld + (1.0f - posAlongLine) * start;
            lineRenderer.SetPosition(i, lineWorldPos);
        }

        lastLoopCenterPos = loopCenterPos;
    }

    public void RenderLassoSwing(SwingableObject o)
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

        // Scrunches up the lasso based on length
        Vector3 loopCenterPos = o.GetLassoCenterPos();

        Vector3 lassoLoopStartPos = Vector3.zero;
        LoopAroundTarget(o, ref lassoLoopPositions);
        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            if (i == 0)
            {
                lassoLoopStartPos = lassoLoopPositions[i];
            }
            lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
        }

        for (int i = 0; i < numberOfLineSegments; i++)
        {
            float posAlongLine = ((float)i / (numberOfLineSegments - 1));
            Vector3 lineWorldPos = posAlongLine * lassoLoopStartPos + (1.0f - posAlongLine) * lassoHandJointPos.position;
            lineRenderer.SetPosition(i, lineWorldPos);
        }

        lastLoopCenterPos = loopCenterPos;
    }

    public void RenderLassoPull(float t, LassoTossable o, Transform lassoSwingCenter)
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

        // Scrunches up the lasso based on length
        Vector3 loopCenterPos = o.GetLassoCenterPos();

        Vector3 lassoLoopStartPos = Vector3.zero;
        LoopAroundTarget(o, ref lassoLoopPositions);
        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            if (i == 0)
            {
                lassoLoopStartPos = lassoLoopPositions[i];
            }
            lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
        }

        for (int i = 0; i < numberOfLineSegments; i++)
        {
            float posAlongLine = ((float)i / (numberOfLineSegments - 1));
            Vector3 lineWorldPos = posAlongLine * lassoLoopStartPos + (1.0f - posAlongLine) * lassoHandJointPos.position;
            lineRenderer.SetPosition(i, lineWorldPos);
        }

        lastLoopCenterPos = loopCenterPos;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param> Here t ranges from [0, infinity)
    /// <param name="o"></param>
    /// <param name="lassoSwingCenter"></param>
    public void RenderLassoHold(float t, LassoTossable o, Transform lassoSwingCenter)
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }
        Vector3 loopCenterPos = o.GetLassoCenterPos();

        Vector3 lassoLoopStartPos = Vector3.zero;
        LoopAroundTarget(o, ref lassoLoopPositions);
        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            if (i == 0)
            {
                lassoLoopStartPos = lassoLoopPositions[i];
            }
            lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
        }

        Vector3 up = lassoSwingCenter.up;
        Vector3 forward = Vector3.ProjectOnPlane((o.transform.position - lassoSwingCenter.position).normalized, up);
        float dist = Vector3.Distance(o.transform.position, lassoHandJointPos.position);
        //print("Up is " + up + " forward " + forward);
        for (int i = 0; i < numberOfLineSegments; i++)
        {
            if (i ==  numberOfLineSegments - 1)
            {
                lassoLinePositions[i] = lassoLoopStartPos;
            }
            else if (i == 0)
            {
                lassoLinePositions[i] = lassoHandJointPos.position;
            }
            else
            {
                //float percentAlongLine = EasingsLibrary.EaseOutBack((float) i / (numberOfLineSegments - 1));
                float percentAlongLine = (float) i / (numberOfLineSegments - 1);
                float radius = o.GetSwingRadius();
                lassoLinePositions[i] = dist * percentAlongLine * up + lassoSwingCenter.position + 2f * radius * EasingsLibrary.EaseInOutBack(percentAlongLine) * forward;
                lassoLinePositions[i - 1] = (1f - percentAlongLine) * lassoLinePositions[i - 1] + percentAlongLine * lassoLinePositions[i];
                //if (t != 0)
                //{
                //}
            }
            lineRenderer.SetPosition(i, lassoLinePositions[i]);
        }

        lastLoopCenterPos = loopCenterPos;
    }



    public void RenderLassoToss(float t, LassoTossable o, Transform lassoSwingCenter)
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

        if (t == 0)
        {
            ResetLassoVelocities();
        }

        Vector3 loopCenterPos = o.GetLassoCenterPos() - t * lassoSwingCenter.up;

        Vector3 lassoLoopStartPos = Vector3.zero;
        Vector3[] targets = new Vector3[numberOfLoopSegments];
        LoopAroundTarget(o, ref targets);
        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            lassoLoopPositions[i] = Vector3.SmoothDamp(lassoLoopPositions[i], targets[i], ref lassoLoopVelocities[i], 0.07f);
            if (i == 0)
            {
                lassoLoopStartPos = lassoLoopPositions[i];
            }
            lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);
        }

        Vector3 up = lassoSwingCenter.up;
        Vector3 forward = Vector3.ProjectOnPlane((loopCenterPos - lassoSwingCenter.position).normalized, up);
        float dist = Vector3.Project(loopCenterPos, up).magnitude - Vector3.Project(lassoSwingCenter.position, up).magnitude;

        for (int i = 0; i < numberOfLineSegments; i++)
        {
            Vector3 target;
            float percentAlongLine = EasingsLibrary.EaseOutBack((float)i / (numberOfLineSegments - 1));
            if (i == numberOfLineSegments - 1)
            {
                target = lassoLoopStartPos;
            }
            else if (i == 0)
            {
                target = lassoHandJointPos.position;
            }
            else
            {
                float radius = o.GetSwingRadius();
                target = dist * percentAlongLine * up + lassoSwingCenter.position + 2f * radius * EasingsLibrary.EaseInOutBack(percentAlongLine) * forward;
            }
            lassoLinePositions[i] = Vector3.SmoothDamp(lassoLinePositions[i], target, ref lassoLineVelocities[i], 0.07f);
            
            lineRenderer.SetPosition(i, lassoLinePositions[i]);
        }

        lastLoopCenterPos = loopCenterPos;
    }

    public void RenderLassoRetract(float t, Transform lassoSwingCenter, float totalTime)
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }

        if (t == 0)
        {
            ResetLassoVelocities();
        }

        Vector3 loopCenterPos = Vector3.Lerp(lastLoopCenterPos, lassoHandJointPos.position, t);
        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            lassoLoopPositions[i] = Vector3.SmoothDamp(lassoLoopPositions[i], hipJointPos.position, ref lassoLoopVelocities[i], totalTime / 5f);
            lineRenderer.SetPosition(i + numberOfLineSegments, lassoLoopPositions[i]);

        }

        for (int i = 0; i < numberOfLineSegments; i++)
        {
            lassoLinePositions[i] = Vector3.SmoothDamp(lassoLinePositions[i], hipJointPos.position, ref lassoLineVelocities[i], totalTime / 5f);
            lineRenderer.SetPosition(i, lassoLinePositions[i]);

        }


        if (t >= 1f)
        {
            lastLoopCenterPos = loopCenterPos;
        }
    }


    public void RenderLassoAtHip()
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0) { return; }
        // Loop lasso around joint of hip
        Vector3 loopTopPos = hipJointPos.position + hipJointPos.up * hipRadius;
        Vector3 loopCenterPos = hipJointPos.position;

        // Use lastLoop center for an approximation of the speed we are moving at
        Vector3 swayVel = lastLoopCenterPos - loopCenterPos;


        Vector3 loopStartingSegmentWorld = loopCenterPos;

        // TODO: Revist, will require springs and tracking previous velocity
        for (int i = 0; i < numberOfLoopSegments; i++)
        {
            float gamma = (float)i / (numberOfLoopSegments - 1) * PI * 2f;
            Vector3 loopPos = loopCenterPos 
                + hipJointPos.up * hipRadius * Sin(gamma) 
                + hipJointPos.forward * hipRadius * Cos(gamma);
            //float swayAmount = Clamp(swayVel.magnitude, 0f, hipMaxSway) * Clamp(Vector3.Distance(loopPos, hipJointPos.position) / (hipRadius * 2f), 0f, 1f); // Might need to normalize this distance in some way based on radius
            //lastLoopVelocities[i] = Vector3.ClampMagnitude(-swayVel.normalized * swayAmount;
            if (i == 0)
            {
                loopStartingSegmentWorld = loopPos;
            }
            lineRenderer.SetPosition(i + numberOfLineSegments, loopPos);
        }

        for (int i = numberOfLineSegments - 1; i >= 0; i--)
        {
            float gamma = ((float)i / (numberOfLineSegments - 1) * numCyclesOnHip) * PI * 2f;
            Vector3 linePos = loopCenterPos
                + hipJointPos.up * hipRadius * Sin(gamma)
                + hipJointPos.forward * hipRadius * Cos(gamma);

            if (i == numberOfLineSegments - 1)
            {
                linePos = loopStartingSegmentWorld;
            }
            lineRenderer.SetPosition(i, linePos);
        }


        lastLoopCenterPos = loopCenterPos;
    }

    void ResetLassoVelocities()
    {
        for (int i = 0; i < lassoLoopVelocities.Length; i++)
        {
            lassoLoopVelocities[i] = Vector3.zero;
        }

        for (int i = 0; i < lassoLineVelocities.Length; i++)
        {
            lassoLineVelocities[i] = Vector3.zero;
        }
    }
}
