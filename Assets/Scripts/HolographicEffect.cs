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
    /// <summary>
    /// 
    /// </summary>
    public float effectSize = 10;

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

        m_EffectMaterial.SetVector("_EffectScale", new Vector4(((float) Screen.width)/ effectSize, ((float) Screen.height)/ effectSize, 0, 0));

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

    public static void CallRender(Vector3 worldPosition, Renderer[] renderer)
    {
        if (!IsInitialized())
            return;
        if (instance.m_IsShowingEffect)
        {
            if (renderer == null)
                return;
            Vector3 pjpos = instance.m_Camera.worldToCameraMatrix.MultiplyPoint(worldPosition);
            pjpos = instance.m_Camera.projectionMatrix.MultiplyPoint(pjpos);
            if (pjpos.x < -1 || pjpos.x > 1 || pjpos.y < -1 || pjpos.y > 1 || pjpos.z < -1 || pjpos.z > 1)
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
            Matrix4x4 frustumCorners = Matrix4x4.identity;

            float fovWHalf = m_Camera.fieldOfView * 0.5f;

            Vector3 toRight = m_Camera.transform.right * m_Camera.nearClipPlane * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * m_Camera.aspect;
            Vector3 toTop = m_Camera.transform.up * m_Camera.nearClipPlane * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            Vector3 topLeft = (m_Camera.transform.forward * m_Camera.nearClipPlane - toRight + toTop);
            float camScale = topLeft.magnitude * m_Camera.farClipPlane / m_Camera.nearClipPlane;

            topLeft.Normalize();
            topLeft *= camScale;

            Vector3 topRight = (m_Camera.transform.forward * m_Camera.nearClipPlane + toRight + toTop);
            topRight.Normalize();
            topRight *= camScale;

            Vector3 bottomRight = (m_Camera.transform.forward * m_Camera.nearClipPlane + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= camScale;

            Vector3 bottomLeft = (m_Camera.transform.forward * m_Camera.nearClipPlane - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= camScale;

            frustumCorners.SetRow(0, topLeft);
            frustumCorners.SetRow(1, topRight);
            frustumCorners.SetRow(2, bottomRight);
            frustumCorners.SetRow(3, bottomLeft);
            m_WorldRenderMaterial.SetMatrix("_FrustumCorners", frustumCorners);




            //m_WorldRenderMaterial.SetMatrix("internalCameraToWorld", m_Camera.cameraToWorldMatrix);
            m_WorldRenderMaterial.SetVector("internalArg", new Vector4(radius*m_ScanTime, fade, fadeWidth, 1));
            float scanFade = 1 - Mathf.Clamp01((m_ScanTime - fadeScanTime)/(maxScanTime - fadeScanTime));
            float efade = 1 - Mathf.Clamp01((m_CurrentTime - fadeOutTime)/(maxStayTime - fadeOutTime));
            m_WorldRenderMaterial.SetVector("internalFade", new Vector4(scanFade, efade, 1, 1));
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
            //Graphics.Blit(m_RenderTexture, rt, m_WorldRenderMaterial);
            CustomGraphicsBlit(m_RenderTexture, rt, m_WorldRenderMaterial);
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

    private static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial)
    {
        //Graphics.Blit(source, dest, fxMaterial);
        //return;
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        fxMaterial.SetPass(0);

        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }
}
