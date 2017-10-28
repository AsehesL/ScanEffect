using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class HolographicEffect : MonoBehaviour
{

    private static HolographicEffect instance;
    
    /// <summary>
    /// 渲染世界空间扫描效果
    /// </summary>
    public Shader worldRenderShader;
    /// <summary>
    /// 渲染全息效果
    /// </summary>
    public Shader effectShader;

    /// <summary>
    /// 扫描最大持续时间
    /// </summary>
    public float maxScanTime;
    /// <summary>
    /// 扫描淡出时间
    /// </summary>
    public float fadeScanTime;

    /// <summary>
    /// 最大停留时间
    /// </summary>
    public float maxStayTime;
    /// <summary>
    /// 结束淡出时间
    /// </summary>
    public float fadeOutTime;

    /// <summary>
    /// 全息效果纹理
    /// </summary>
    public Texture2D holographicTex;

    /// <summary>
    /// 半径
    /// </summary>
    public float radius;
    /// <summary>
    /// 淡出
    /// </summary>
    public float fade;
    /// <summary>
    /// 淡出宽度
    /// </summary>
    public float fadeWidth;

    private Material m_ReplaceMaterial;
    private Material m_EffectMaterial;
    private Material m_WorldRenderMaterial;

    private CommandBuffer m_CommandBuffer;
    private RenderTexture m_RenderTexture;

    private bool m_IsInitialized;

    private float m_ScanTime;
    private bool m_IsScaning;

    private float m_CurrentTime;
    private bool m_IsShowingEffect;

    private Camera m_Camera;

    void Awake()
    {
        if (m_IsInitialized)
            return;
        instance = this;

        if (effectShader == null || !effectShader.isSupported)
            return;
        if (worldRenderShader == null || !worldRenderShader.isSupported)
            return;
        m_ReplaceMaterial = new Material(Shader.Find("Unlit/Color"));
        m_EffectMaterial = new Material(effectShader);
        m_WorldRenderMaterial = new Material(worldRenderShader);
        if (holographicTex)
            m_EffectMaterial.SetTexture("_EffectTex", holographicTex);

        m_EffectMaterial.SetVector("_EffectScale", new Vector4((float) Screen.width/10, (float) Screen.height/10, 0, 0));

        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
        m_CommandBuffer = new CommandBuffer();
        m_CommandBuffer.name = "[Holographic Effect]";
        m_Camera = GetComponent<Camera>();
        m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_CommandBuffer);

        m_Camera.depthTextureMode |= DepthTextureMode.Depth;

        m_IsInitialized = true;
    }


    void Update()
    {
        if (m_IsShowingEffect)
        {
            m_CurrentTime += Time.deltaTime;
            if (m_CurrentTime > maxStayTime)
            {
                m_IsShowingEffect = false;
            }
        }
        if (m_IsScaning)
        {
            m_ScanTime += Time.deltaTime;
            if (m_ScanTime > maxScanTime)
            {
                m_IsScaning = false;
            }
        }
    }

    void OnDestroy()
    {
        if (m_ReplaceMaterial)
            Destroy(m_ReplaceMaterial);
        m_ReplaceMaterial = null;
        if (m_EffectMaterial)
            Destroy(m_EffectMaterial);
        m_EffectMaterial = null;
        if (m_WorldRenderMaterial)
            Destroy(m_WorldRenderMaterial);
        m_WorldRenderMaterial = null;
        if (m_CommandBuffer != null)
            m_CommandBuffer.Release();
        m_CommandBuffer = null;
        if (m_RenderTexture)
            Destroy(m_RenderTexture);
        m_RenderTexture = null;
    }

    private static bool IsInitialized()
    {
        if (instance == null)
            return false;
        return instance.m_IsInitialized;
    }

    public static void CallRender(Renderer[] renderer)
    {
        if (!IsInitialized())
            return;
        if (instance.m_IsShowingEffect)
        {
            if (renderer == null)
                return;
            for (int i = 0; i < renderer.Length; i++)
            {
                instance.m_CommandBuffer.DrawRenderer(renderer[i], instance.m_ReplaceMaterial);
            }
        }
    }

    public static bool CallScan(Vector3 worldPosition)
    {
        if (!IsInitialized())
            return false;
        if (instance.m_IsScaning)
            return false;
        instance.m_WorldRenderMaterial.SetVector("internalCentPos", worldPosition);
        instance.m_IsScaning = true;
        instance.m_IsShowingEffect = true;
        instance.m_CurrentTime = 0;
        instance.m_ScanTime = 0;
        return true;
    }

    void OnPostRender()
    {
        if (!m_IsInitialized)
            return;
        m_CommandBuffer.Clear();
        m_CommandBuffer.SetRenderTarget(m_RenderTexture);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.black);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {

        if (m_IsScaning || m_IsShowingEffect)
        {
            m_WorldRenderMaterial.SetMatrix("internalCameraToWorld", m_Camera.cameraToWorldMatrix);
            m_WorldRenderMaterial.SetVector("internalArg", new Vector4(radius*m_ScanTime, fade, fadeWidth, 1));
            float scanFade = 1 - Mathf.Clamp01((m_ScanTime - fadeScanTime)/(maxScanTime - fadeScanTime));
            float efade = 1 - Mathf.Clamp01((m_CurrentTime - fadeOutTime)/(maxStayTime - fadeOutTime));
            m_WorldRenderMaterial.SetVector("internalFade", new Vector4(scanFade, efade, 1, 1));
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
            Graphics.Blit(m_RenderTexture, rt, m_WorldRenderMaterial);
            RenderEffect(src, dst, rt);
            RenderTexture.ReleaseTemporary(rt);
        }
        else
        {
            Graphics.Blit(src, dst);
        }
    }

    private void RenderEffect(RenderTexture src, RenderTexture dst, RenderTexture rt)
    {
        m_EffectMaterial.SetTexture("_PreTex", rt);

        Graphics.Blit(src, dst, m_EffectMaterial);
    }
}
