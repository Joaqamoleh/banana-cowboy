using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * This is a component added to any planet, platform, or object to which we
 * want the player and other gravity objects to be attracted to. The direction
 * of gravity is determined by AttractionDirection and can be Radial (as it is
 * for planets) or in some local X / Y / Z direction relative to the gravity attractor.
 * 
 * For Radial attractors, the center of gravity is also chose as the direction to which
 * the object is attracted. This can be useful in the case of half-spheres or other
 * non-uniform spherical objects, as, by default, the center of gravity is
 * at the center of the object, which may not be what is wanted. (However,
 * in most cases, it will be).
 * 
 * A Trigger CollisionObject must be placed on the G
 */
public class GravityAttractor : MonoBehaviour
{
    public enum GravityType
    {
        SPHERE,
        PARALLEL,
        CYLINDER,
    }

    public float gravity = 60;
    public int priority = 0; // Higher priority overrides lower priority
    [Tooltip("Types are: Sphere, Parallel, and Cylinder. Parallel will pull striaght down in y, " +
        "sphere will pull towards the center of gravity in all directions, cylinder will pull only in relative x and z," +
        "ignoring how far up y the object is")]
    public GravityType type = GravityType.PARALLEL;
    [Tooltip("This is only required for sphere and cylinder when you want to change the center of gravity from the center of the trigger" +
        "transform. Otherwise ignore it")]
    public Transform centerOfGravity = null;

    public void Awake()
    {
        if (centerOfGravity == null)
        {
            centerOfGravity = transform;
        }
    }

    public Vector3 GetGravityDirection(Transform body)
    {
        switch (type)
        {
            case GravityType.SPHERE:
                return (centerOfGravity.position - body.position).normalized;
            case GravityType.PARALLEL:
                return -transform.up;
            case GravityType.CYLINDER:
                return -transform.up; // TODO
            default:
                return -transform.up;
        }
    }

    public float GetGravityForce()
    {
        return gravity;
    }

    public int GetPriority()
    {
        return priority;
    }
}