using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class GizmosLibrary : MonoBehaviour
{
    public static void DrawGrizmosCircle(Vector3 pos, float radius, Color c)
    {
        DrawGizmosCircle(pos, radius, Vector3.up, Vector3.right, c);
    }

    public static void DrawGizmosCircle(Vector3 pos, float radius, Vector3 right, Color c)
    {
        Vector3 forward = Vector3.Cross(Vector3.up, right).normalized;
        if (forward.magnitude == 0)
        {
            forward = Vector3.Cross(Vector3.right, right).normalized;
        }
        Vector3 up = Vector3.Cross(right, forward).normalized;
        DrawGizmosCircle(pos, radius, up, right, c);
    }

    public static void DrawGizmosCircle(Vector3 pos, float radius, Vector3 normal, Vector3 right, Color c)
    {
        DrawGizmosEllipse(pos, radius, radius, normal, right, c);
    }

    public static void DrawGizmosEllipse(Vector3 pos, float rightDist, float forwardDist, Vector3 normal, Vector3 right, Color c)
    {
        Gizmos.color = c;
        Vector3 forward = Vector3.Cross(normal, right).normalized;
        float twoPi = 2 * Mathf.PI;
        float delta = 0.03f * twoPi;
        for (float theta = delta; theta <= twoPi + 0.1f; theta += delta)
        {
            Vector3 prev = forwardDist * Mathf.Sin(theta - delta) * forward + Mathf.Cos(theta - delta) * rightDist * right + pos;
            Vector3 next = forwardDist * Mathf.Sin(theta) * forward + Mathf.Cos(theta) * rightDist * right + pos;
            Gizmos.DrawLine(prev, next);
        }
    }
}
