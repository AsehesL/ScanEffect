using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class LedSystem : MonoBehaviour
{

    private static LedSystem instance;

    public Shader replaceShader;
    public Shader worldRenderShader;
    public Shader effectShader;

    public float maxScanTime;
    public float fadeScanTime;

    public float maxLedTime;
    public float fadeLedTime;

    public Texture2D ledTex;

    public float radius;
    public float fade;
    public float fadeWidth;

    private Material m_ReplaceMaterial;
    private Material m_EffectMaterial;
    private Material m_WorldRenderMaterial;

    private CommandBuffer m_CommandBuffer;
    private RenderTexture m_RenderTexture;

    private bool m_IsInitialized;

    private float m_ScanTime;
    private bool m_IsScaning;

    private float m_LedTime;
    private bool m_IsShowingLed;

    private Camera m_Camera;

    void Awake()
    {
        if (m_IsInitialized)
            return;
        instance = this;

        if (replaceShader == null || !replaceShader.isSupported)
            return;
        if (effectShader == null || !effectShader.isSupported)
            return;
        if (worldRenderShader == null || !worldRenderShader.isSupported)
            return;
        m_ReplaceMaterial = new Material(replaceShader);
        m_EffectMaterial = new Material(effectShader);
        m_WorldRenderMaterial = new Material(worldRenderShader);
        if (ledTex)
            m_EffectMaterial.SetTexture("_LedTex", ledTex);

        m_EffectMaterial.SetVector("_LedScale", new Vector4((float) Screen.width/10, (float) Screen.height/10, 0, 0));

        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
        m_CommandBuffer = new CommandBuffer();
        m_CommandBuffer.name = "[Led Effect]";
        m_Camera = GetComponent<Camera>();
        m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_CommandBuffer);

        m_Camera.depthTextureMode |= DepthTextureMode.Depth;

        m_IsInitialized = true;
    }


    void Update()
    {
        if (m_IsShowingLed)
        {
            m_LedTime += Time.deltaTime;
            if (m_LedTime > maxLedTime)
            {
                m_IsShowingLed = false;
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
        if (instance.m_IsShowingLed)
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
        instance.m_IsShowingLed = true;
        instance.m_LedTime = 0;
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

        if (m_IsScaning || m_IsShowingLed)
        {
            m_WorldRenderMaterial.SetMatrix("internalCameraToWorld", m_Camera.cameraToWorldMatrix);
            m_WorldRenderMaterial.SetVector("internalArg", new Vector4(radius*m_ScanTime, fade, fadeWidth, 1));
            float scanFade = 1 - Mathf.Clamp01((m_ScanTime - fadeScanTime)/(maxScanTime - fadeScanTime));
            float ledFade = 1 - Mathf.Clamp01((m_LedTime - fadeLedTime)/(maxLedTime - fadeLedTime));
            m_WorldRenderMaterial.SetVector("internalFade", new Vector4(scanFade, ledFade, 1, 1));
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
            Graphics.Blit(m_RenderTexture, rt, m_WorldRenderMaterial);
            RenderLed(src, dst, rt);
            RenderTexture.ReleaseTemporary(rt);
        }
        else
        {
            Graphics.Blit(src, dst);
        }
    }

    private void RenderLed(RenderTexture src, RenderTexture dst, RenderTexture rt)
    {
        m_EffectMaterial.SetTexture("_PreTex", rt);

        Graphics.Blit(src, dst, m_EffectMaterial);
    }
}
