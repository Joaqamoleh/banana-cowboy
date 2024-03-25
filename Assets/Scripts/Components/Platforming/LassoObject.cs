using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * This class is here just to identify what is a LassoObject
 * LassoEnemy and SwingableObject are children of this class.
 */
public class LassoObject : MonoBehaviour
{
    [SerializeField]
    LassoRenderHighlight[] renderHighlights;

    [SerializeField]
    SpriteRenderer lassoIndicatorSprite;

    [SerializeField]
    Transform lassoCenterBasis;

    [Min(0f)]
    public float lassoThrowRadius = 4f, appoximateLassoRadius = 2f;


    private bool m_isLassoable = true;
    public bool isLassoable { 
        get { return m_isLassoable; } 
        set { 
            m_isLassoable = value;
            UpdateLassoIndictor();
        } 
    }
    private bool m_currentlyLassoed = false;
    public bool currentlyLassoed { 
        get { return m_currentlyLassoed; } 
        set { 
            if (m_currentlyLassoed != value)
            {
                if (m_currentlyLassoed)
                {
                    OnLassoObjectLassoed?.Invoke(this);
                } 
                else
                {
                    OnLassoObjectReleased?.Invoke(this);
                }
                m_currentlyLassoed = value; 
                UpdateLassoIndictor();
            }
        }
    }

    private bool m_inRange = false;
    public bool isInRange
    {
        get { return m_inRange; }
        set
        {
            if (value != m_inRange)
            {
                m_inRange = value;
                UpdateLassoIndictor();
            }
        }
    }

    public delegate void LassoObjectUpdate(LassoObject lassoObject);
    public event LassoObjectUpdate OnLassoObjectLassoed;
    public event LassoObjectUpdate OnLassoObjectReleased;

    public void Start()
    {
        if (lassoCenterBasis == null)
        {
            lassoCenterBasis = transform;
        }
        foreach (LassoRenderHighlight h in renderHighlights)
        {
            h.InitOriginalMaterials();
        }
        UpdateLassoIndictor();
    }

    void UpdateLassoIndictor()
    {
        if (m_isLassoable && !m_currentlyLassoed && m_inRange)
        {
            lassoIndicatorSprite.gameObject.SetActive(true);
        } 
        else
        {
            lassoIndicatorSprite.gameObject.SetActive(false);
        }
    }

    public void Select()
    {
        foreach (LassoRenderHighlight h in renderHighlights)
        {
            h.Select();
        }
    }

    public void Deselect()
    {
        foreach (LassoRenderHighlight h in renderHighlights)
        {
            h.Deselect();
        }
    }

    public Vector3 GetLassoCenterPos()
    {
        return lassoCenterBasis.position;
    }

    public Transform GetLassoCenterBasis()
    {
        return lassoCenterBasis;
    }

    public float GetAppoximateLassoRadius()
    {
        return appoximateLassoRadius;
    }

    public void Grab()
    {
        isLassoable = false;
        currentlyLassoed = true;
    }

    public void Release()
    {
        isLassoable = true;
        currentlyLassoed = false;
    }
}


[Serializable]
public class LassoRenderHighlight
{

    [SerializeField]
    Renderer materialRenderer;
    List<Material> originalMaterials = new List<Material>();

    [SerializeField]
    Material[] selectedMaterials;

    public void Select()
    {
        if (selectedMaterials == null) { return; }
        for (int i = 0; i < selectedMaterials.Length; i++)
        {
            materialRenderer.materials[i] = selectedMaterials[i];
        }
        if (originalMaterials.Count() > selectedMaterials.Length)
        {
            for (int i = selectedMaterials.Length; i < originalMaterials.Count(); i++)
            {
                materialRenderer.materials[i] = originalMaterials[i];
            }
        }
    }

    public void Deselect()
    {
        if (materialRenderer == null) { return; }

        for (int i = 0; i < originalMaterials.Count; i++)
        {
            materialRenderer.materials[i] = originalMaterials[i];
        }
    }

    public void InitOriginalMaterials()
    {
        if (originalMaterials == null || materialRenderer == null) { return; }
        originalMaterials.Clear();
        if (materialRenderer != null)
        {
            List<Material> mats = new List<Material>();
            materialRenderer.GetMaterials(mats);
            foreach (Material m in mats)
            {
                if (m != null)
                {
                    originalMaterials.Add(m);
                }
            }
        }
    }
}