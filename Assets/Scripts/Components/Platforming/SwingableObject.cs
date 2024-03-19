using UnityEngine;

public class SwingableObject : LassoObject
{

    [SerializeField, Range(0f, 100f)]
    float startingRadius, endingRadius;
    [SerializeField, Range(0f, 360f)]
    float startingTheta = 0f, endingTheta = 0f, startingGamma = 0f, endingGamma = 180f;
    [SerializeField, Range(0f, 40f), Tooltip("Measured in units per second")]
    float startingSwingSpeed = 10f, maxSwingSpeed = 10f;
    [SerializeField, Tooltip("The point at which the swing is based.")]
    Transform swingBasis;

    float approximateArcLength, currentArcPos, currentSwingSpeed;

    [SerializeField, Range(0f, 1)]
    float paramVisualization = 0;

    public delegate void SwingEndDelegate(Vector3 endingVelocity);
    public SwingEndDelegate SwingEnd;

    Rigidbody attachedBody;

    private void OnValidate()
    {
        if (startingSwingSpeed > maxSwingSpeed)
        {
            startingSwingSpeed = maxSwingSpeed;
        }
    }

    private void OnDrawGizmosSelected()
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

    private void Awake()
    {
        swingBasis ??= transform;
        approximateArcLength = ApproximateArcLength();
    }

    // I tried calculating this explicitly, but way too much work for something so small
    // (Look at Incomplete Elliptical Integrals) https://functions.wolfram.com/EllipticIntegrals/EllipticE2/introductions/IncompleteEllipticIntegrals/ShowAll.html
    float ApproximateArcLength()
    {
        float distance = 0;
        float delta = 0.01f;
        for (float t = delta; t <= 1f + 0.001f; t += delta)
        {
            float theta = Mathf.Lerp(startingTheta, endingTheta, t - delta);
            float gamma = Mathf.Lerp(startingGamma, endingGamma, t - delta);
            float radius = Mathf.Lerp(startingRadius, endingRadius, t - delta);
            Vector3 prev = GetSwingPosition(theta, gamma, radius);

            theta = Mathf.Lerp(startingTheta, endingTheta, t);
            gamma = Mathf.Lerp(startingGamma, endingGamma, t);
            radius = Mathf.Lerp(startingRadius, endingRadius, t);
            Vector3 next = GetSwingPosition(theta, gamma, radius);

            distance += Vector3.Distance(prev, next);
        }
        return distance;
    }

    private void Update()
    {
        if (attachedBody != null)
        {
            float t = Mathf.Clamp(currentArcPos / approximateArcLength, 0f, 1f);
            float theta = Mathf.Lerp(startingTheta, endingTheta, t);
            float gamma = Mathf.Lerp(startingGamma, endingGamma, t);
            float radius = Mathf.Lerp(startingRadius, endingRadius, t);
            attachedBody.position = GetSwingPosition(theta, gamma, radius);

            if (t >= 1.0f)
            {
                // End swing
                float prevtheta = Mathf.Lerp(startingTheta, endingTheta, t - 0.05f);
                float prevgamma = Mathf.Lerp(startingGamma, endingGamma, t - 0.05f);
                float prevradius = Mathf.Lerp(startingRadius, endingRadius, t - 0.05f);
                Vector3 velocity = (attachedBody.position - GetSwingPosition(prevtheta, prevgamma, prevradius)).normalized * currentSwingSpeed;
                SwingEnd?.Invoke(velocity);
            }

            currentArcPos += currentSwingSpeed * Time.deltaTime;
        }
    }

    public void AttachToSwingable(Rigidbody attachedObject)
    {
        currentArcPos = 0f;
        currentSwingSpeed = startingSwingSpeed;
        attachedBody = attachedObject;
        attachedBody.isKinematic = true;
    }

    public void ChangeSwingSpeed(float delta)
    {
        currentSwingSpeed = Mathf.Clamp(currentSwingSpeed + delta, 0f, maxSwingSpeed);
    }
}
