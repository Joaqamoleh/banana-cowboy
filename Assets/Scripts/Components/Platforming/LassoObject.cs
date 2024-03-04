using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UI;
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

    public delegate void LassoObjectUpdate(LassoObject lassoObject);
    public event LassoObjectUpdate OnLassoObjectLassoed;
    public event LassoObjectUpdate OnLassoObjectReleased;

    public void Start()
    {
        foreach (LassoRenderHighlight h in renderHighlights)
        {
            h.InitOriginalMaterials();
        }
        UpdateLassoIndictor();
    }

    void UpdateLassoIndictor()
    {
        if (m_isLassoable && !m_currentlyLassoed)
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