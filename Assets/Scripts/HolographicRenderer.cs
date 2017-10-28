using UnityEngine;
using System.Collections.Generic;

public class HolographicRenderer : MonoBehaviour
{
    private Renderer[] m_Renderers;

    void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Renderer> filters = new List<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is MeshRenderer)
            {
                filters.Add(renderers[i]);
                continue;
            }
            if (renderers[i] is SkinnedMeshRenderer)
            {
                filters.Add(renderers[i]);
                continue;
            }
        }
        if (filters.Count > 0)
            m_Renderers = filters.ToArray();
    }

    void OnRenderObject()
    {
        if (m_Renderers != null)
            HolographicEffect.CallRender(transform.position, m_Renderers);
    }
}
