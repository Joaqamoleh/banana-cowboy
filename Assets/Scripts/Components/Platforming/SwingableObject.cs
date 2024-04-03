using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using static SwingableObject;
using static Unity.Mathematics.math;

public class SwingableObject : LassoObject
{

    [SerializeField, Range(0f, 100f)]
    float startingRadius, endingRadius;
    [SerializeField, Range(0f, 360f)]
    float startingTheta = 0f, endingTheta = 0f, startingGamma = 0f, endingGamma = 180f;
    [SerializeField, Range(0f, 100f), Tooltip("Measured in units per second")]
    float minSwingSpeed = 10f, maxSwingSpeed = 10f;
    [SerializeField, Range(0f, 10f), Tooltip("This is multiplied by the current velocity of the player at the end of a swing.")]
    float endingSwingMultForce = 1f;

    [SerializeField]
    SwingType type = SwingType.ONE_TIME;
    public enum SwingType
    {
        ONE_TIME,
        OSCILATING,
        REPEAT,
    }
    
    [SerializeField, Tooltip("The point at which the swing is based.")]
    Transform swingBasis;
    Transform characterBasis = null;

    [SerializeField]
    Transform model;
    [SerializeField]
    bool reorientModel = false, reorientSwing = false;

    Quaternion modelInitial;

    float approximateArcLength, currentArcPos, currentSwingSpeed, swingMult = 1.0f;
    Vector3 lastPos, startPos, attachedStart;
    bool pullingToStart = false;

    [SerializeField, Range(0f, 1)]
    float paramVisualization = 0;

    [SerializeField]
    SplineContainer splineMovement = null;

    [SerializeField, Min(0f)]
    float splineMoveSpeed = 1f;

    [SerializeField, Range(0f, 4f)]
    float gracePeroidSeconds = 0.4f;
    float lastGracePeroidUpdate = -0.4f;

    private float splineTotalDist, splineCurrentDist;

    public enum MoveType
    {
        FORWARD,
        OSCILATING,
    }
    [SerializeField]
    MoveType splineMoveType = MoveType.FORWARD;

    Rigidbody attachedBody;

    public delegate void SwingEndDelegate(Vector3 endingVel);
    public event SwingEndDelegate SwingEnd;

    protected void OnValidate()
    {
        if (minSwingSpeed > maxSwingSpeed)
        {
            minSwingSpeed = maxSwingSpeed;
        }
    }

    protected void OnDrawGizmosSelected()
    {
        if (swingBasis == null)
        {
            swingBasis = transform;
        }

        Vector3 circleCenter = swingBasis.position - startingRadius * Mathf.Sin(startingGamma * Mathf.Deg2Rad) * swingBasis.up;
        float circleRadius = startingRadius * Mathf.Cos(startingGamma * Mathf.Deg2Rad);

        // Draw starting circle of the swing
        GizmosLibrary.DrawGizmosCircle(circleCenter, circleRadius, swingBasis.up, swingBasis.right, Color.blue);

        // Draw current
        Vector3 current = GetSwingPosition(startingTheta, startingGamma, startingRadius);
        Gizmos.DrawSphere(current, 0.15f);

        // Draw ending circle of the swing
        circleCenter = swingBasis.position - endingRadius * Mathf.Sin(endingGamma * Mathf.Deg2Rad) * swingBasis.up;
        circleRadius = endingRadius * Mathf.Cos(endingGamma * Mathf.Deg2Rad);

        // Draw starting circle of the swing
        GizmosLibrary.DrawGizmosCircle(circleCenter, circleRadius, swingBasis.up, swingBasis.right, Color.cyan);

        // Draw current
        current = GetSwingPosition(endingTheta, endingGamma, endingRadius);
        Gizmos.DrawSphere(current, 0.15f);


        // Draw Path
        Gizmos.color = Color.yellow;
        float delta = 0.05f;
        for (float t = delta; t <= 1f + 0.01f; t += delta)
        {
            float theta = Mathf.Lerp(startingTheta, endingTheta, t - delta);
            float gamma = Mathf.Lerp(startingGamma, endingGamma, t - delta);
            float radius = Mathf.Lerp(startingRadius, endingRadius, t - delta);
            Vector3 prev = GetSwingPosition(theta, gamma, radius);

            theta = Mathf.Lerp(startingTheta, endingTheta, t);
            gamma = Mathf.Lerp(startingGamma, endingGamma, t);
            radius = Mathf.Lerp(startingRadius, endingRadius, t);
            Vector3 next = GetSwingPosition(theta, gamma, radius);
            Gizmos.DrawLine(prev, next);
        }

        Gizmos.color = Color.green;
        current = GetSwingPosition(
            Mathf.Lerp(startingTheta, endingTheta, paramVisualization), 
            Mathf.Lerp(startingGamma, endingGamma, paramVisualization), 
            Mathf.Lerp(startingRadius, endingRadius, paramVisualization)
        );
        Gizmos.DrawSphere(current, 0.15f);
    }

    Vector3 GetSwingPosition(float theta, float gamma, float radius)
    {
        return radius * Mathf.Cos(theta * Mathf.Deg2Rad) * Mathf.Cos(gamma * Mathf.Deg2Rad) * swingBasis.right +
            radius * Mathf.Sin(gamma * Mathf.Deg2Rad) * -swingBasis.up +
            radius * Mathf.Sin(theta * Mathf.Deg2Rad) * Mathf.Cos(gamma * Mathf.Deg2Rad) * swingBasis.forward
            + swingBasis.position;
    }

    Vector3 GetSwingPosition(Vector3 thetaGammaRadius)
    {
        return GetSwingPosition(thetaGammaRadius.x, thetaGammaRadius.y, thetaGammaRadius.z);
    }
    // I tried calculating this explicitly, but way too much work for something so small
    // (Look at Incomplete Elliptical Integrals) https://functions.wolfram.com/EllipticIntegrals/EllipticE2/introductions/IncompleteEllipticIntegrals/ShowAll.html
    float ApproximateArcLength()
    {
        float distance = 0;
        float delta = 0.01f;
        for (float t = delta; t <= 1f + 0.001f; t += delta)
        {
            Vector3 prev = GetSwingPosition(GetThetaGammaRadius(t - delta));
            Vector3 next = GetSwingPosition(GetThetaGammaRadius(t));
            distance += Vector3.Distance(prev, next);
        }
        return distance;
    }
    protected void Awake()
    {
        if (swingBasis == null)
        {
            swingBasis = transform;
        }
        if (model != null)
        {
            modelInitial = model.rotation;
        }
        if (splineMovement != null)
        {
            splineTotalDist = splineMovement.CalculateLength();
            splineCurrentDist = 0f;
            transform.position = splineMovement.EvaluatePosition(0f);
        }
        approximateArcLength = ApproximateArcLength();
    }
    protected void Update()
    {
        if (attachedBody != null)
        {
            if (Time.time - lastGracePeroidUpdate > gracePeroidSeconds)
            {
                lastPos = attachedBody.position;
                lastGracePeroidUpdate = Time.time;
            } 
            if (pullingToStart)
            {
                float dist = Vector3.Distance(attachedBody.position, startPos);
                float total = Vector3.Distance(startPos, attachedStart);
                float t = (total - dist + 80f * Time.deltaTime) / total;
                attachedBody.position = Vector3.Lerp(attachedStart, startPos, t);
                if (dist < 0.5f)
                {
                    pullingToStart = false;
                }
            } 
            else
            {
                if (approximateArcLength == 0f)
                {
                    attachedBody.position = GetSwingPosition(GetThetaGammaRadius(0f));
                }
                else
                {
                    float t = currentArcPos / approximateArcLength;

                    if (type == SwingType.REPEAT)
                    {
                        t = frac(t);
                    }
                    else if (type == SwingType.ONE_TIME && t >= 1.0f)
                    {
                        Vector3 vel = EndSwing();
                        SwingEnd?.Invoke(vel);
                        return;
                    }
                    else if (type == SwingType.OSCILATING)
                    {
                        if (t >= 1.0f)
                        {
                            swingMult = -1.0f;
                        } 
                        else if (t <= 0.0f)
                        {
                            swingMult = 1.0f;
                        }
                    }

                    Vector3 p = GetThetaGammaRadius(t);
                    currentSwingSpeed = Mathf.Lerp(minSwingSpeed, maxSwingSpeed, Mathf.Sin(p.y)) * swingMult;
                    attachedBody.position = GetSwingPosition(p);
                    currentArcPos += currentSwingSpeed * Time.deltaTime;
                }
            }

            if (splineMovement != null)
            {
                float t = splineCurrentDist / splineTotalDist;
                //splineMovement.Spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);
                transform.position = splineMovement.EvaluatePosition(t); // , Quaternion.LookRotation(tangent, up));

                
                if (splineMoveType == MoveType.FORWARD)
                {
                    if (splineCurrentDist > splineTotalDist)
                    {
                        Vector3 vel = EndSwing();
                        SwingEnd?.Invoke(vel);
                        return;
                    }
                    if (splineCurrentDist < splineTotalDist)
                    {
                        splineCurrentDist += splineMoveSpeed * Time.deltaTime;
                    }
                }
                else
                {
                    if ((splineCurrentDist + splineMoveSpeed * Time.deltaTime > splineTotalDist) || (splineCurrentDist + splineMoveSpeed * Time.deltaTime <= 0.0f))
                    {
                        splineMoveSpeed = -splineMoveSpeed;
                    }
                    splineCurrentDist += splineMoveSpeed * Time.deltaTime;
                }
            }

            if (reorientModel && model != null)
            {
                Vector3 forward = (attachedBody.position - transform.position).normalized;
                Vector3 right = Vector3.Cross(transform.up, forward);
                Vector3 up = Vector3.Cross(forward, right);
                model.rotation = Quaternion.LookRotation(forward, up);
            }
        }
        else
        {
            if (model != null && reorientModel)
            {
                model.rotation = Quaternion.Slerp(model.rotation, modelInitial, Time.deltaTime * 8f);
            }
            if (splineMovement != null)
            {
                splineCurrentDist = Mathf.Lerp(splineCurrentDist, 0f, Time.deltaTime);
                transform.position = splineMovement.EvaluatePosition(splineCurrentDist / splineTotalDist);
            }
        }
    }

    public void AttachToSwingable(Rigidbody attachedObject, Transform characterBasis)
    {
        currentArcPos = 0f;
        attachedBody = attachedObject;
        attachedBody.isKinematic = true;
        lastPos = attachedBody.position;
        this.characterBasis = characterBasis;
        Vector3 up = characterBasis.up;
        if (reorientSwing)
        {
            Vector3 right = Vector3.ProjectOnPlane((swingBasis.position - attachedBody.transform.position), up);
            Vector3 forward = Vector3.Cross(up, right);
            swingBasis.rotation = Quaternion.LookRotation(forward, up);
        }
        if (Vector3.Distance(attachedBody.position, startPos) > 1.0f) {
            startPos = GetSwingPosition(GetThetaGammaRadius(0));
            pullingToStart = true;
            attachedStart = attachedBody.position;
        }
    }

    public Vector3 EndSwing()
    {
        float t = Mathf.Clamp((currentArcPos + 0.01f * currentSwingSpeed) / approximateArcLength, 0f, 1f);
        Vector3 characterUp = characterBasis.up;
        Vector3 velocity = Vector3.ProjectOnPlane((GetSwingPosition(GetThetaGammaRadius(t)) - lastPos), characterUp).normalized * Mathf.Abs(currentSwingSpeed);
        attachedBody = null;
        if (splineMovement != null)
        {
            float s = splineCurrentDist / splineTotalDist;
            splineMovement.Spline.Evaluate(s, out float3 pos, out float3 tangent, out float3 up);
            velocity += Vector3.ProjectOnPlane(splineMovement.transform.TransformDirection((Vector3)tangent), characterUp).normalized * Mathf.Abs(splineMoveSpeed);
        }
        isLassoable = true;
        return velocity * endingSwingMultForce;
    }

    public Vector3 GetApproximateSwingDirection()
    {
        return (attachedBody.position - lastPos).normalized;
    }

    Vector3 GetThetaGammaRadius(float t)
    {
        float theta = Mathf.Lerp(startingTheta, endingTheta, t);
        float gamma = Mathf.Lerp(startingGamma, endingGamma, t);
        float radius = Mathf.Lerp(startingRadius, endingRadius, t);
        return new Vector3(theta, gamma, radius);
    }
}
