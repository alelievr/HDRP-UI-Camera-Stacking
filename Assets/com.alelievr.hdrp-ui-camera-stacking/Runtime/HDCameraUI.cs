using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// When this component is added to a camera, it replaces the standard rendering by a single optimized pass to render the GUI in screen space.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class HDCameraUI : MonoBehaviour
{
    /// <summary>
    /// Select which layer mask to use to render the UI.
    /// </summary>
    [Tooltip("Select which layer mask to use to render the UI.")]
    public LayerMask uiLayerMask = 1 << 5;

    /// <summary>
    /// Select in which order the UI cameras are composited, higher priority will be executed before.
    /// </summary>
    [Tooltip("Select in which order the UI cameras are composited, higher priority will be executed before.")]
    public float priority = 0;

    /// <summary>
    /// Use this property to apply a post process shader effect on the UI. The shader must be compatible with Graphics.Blit().
    /// see https://github.com/h33p/Unity-Graphics-Demo/blob/master/Assets/Asset%20Store/PostProcessing/Resources/Shaders/Blit.shader
    /// </summary>
    [Tooltip("Apply a post process effect on the UI buffer. The shader must be compatible with Graphics.Blit().")]
    public Material customPostEffect;

    /// <summary>
    /// Internal render texture used to store the UI before compositing with the main camera.
    /// </summary>
    [HideInInspector]
    public RenderTexture renderTexture;

    /// <summary>
    /// Disables the culling typically done every frame while rendering the UI. This is an optimization and only works if your GUI elements are not dynamic.
    /// </summary>
    [Tooltip("Disables the culling typically done every frame while rendering the UI. This is an optimization and only works if your GUI elements are not dynamic.")]
    public bool oneTimeCulling;

    /// <summary>
    /// Specifies the graphics format to use when rendering the UI.
    /// </summary>
    [Tooltip("Specifies the graphics format to use when rendering the UI.")]
    public GraphicsFormat graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;

    [System.NonSerialized]
    bool updateCulling = true;
    CullingResults cullingResults;
    [SerializeField]
    internal bool showAdvancedSettings;
    [SerializeField]
    Shader blitWithBlending; // Force the serialization of the shader in the scene so it ends up in the build

    HDAdditionalCameraData data;
    ShaderTagId[] hdTransparentPassNames;
    ProfilingSampler cullingSampler;
    ProfilingSampler renderingSampler;
    ProfilingSampler uiCameraStackingSampler;

    // Start is called before the first frame update
    void OnEnable()
    {
        data = GetComponent<HDAdditionalCameraData>();

        if (data == null)
            return;

        data.customRender -= DoRenderUI;
        data.customRender += DoRenderUI;

        hdTransparentPassNames = new ShaderTagId[]
        {
            HDShaderPassNames.s_TransparentBackfaceName,
            HDShaderPassNames.s_ForwardOnlyName,
            HDShaderPassNames.s_ForwardName,
            HDShaderPassNames.s_SRPDefaultUnlitName
        };

        // TODO: add option to have depth buffer
        // TODO: Add VR support
        renderTexture = new RenderTexture(1, 1, 0, graphicsFormat, 1);
        renderTexture.dimension = TextureDimension.Tex2DArray;
        renderTexture.volumeDepth = 1;

        cullingSampler = new ProfilingSampler("UI Culling");
        renderingSampler = new ProfilingSampler("UI Rendering");
        uiCameraStackingSampler = new ProfilingSampler("Render UI Camera Stacking");

        if (blitWithBlending == null)
            blitWithBlending = Shader.Find("Hidden/HDRP/UI_Compositing");

        CameraStackingCompositing.uiList.Add(this);
    }

    void OnDisable()
    {
        if (data == null)
            return;

        data.customRender -= DoRenderUI;
        CameraStackingCompositing.uiList.Remove(this);
    }

    void UpdateRenderTexture(Camera camera)
    {
        if (camera.pixelWidth != renderTexture.width
            || camera.pixelHeight != renderTexture.height
            || renderTexture.graphicsFormat != graphicsFormat)
        {
            renderTexture.Release();
            renderTexture.width = camera.pixelWidth;
            renderTexture.height = camera.pixelHeight;
            renderTexture.graphicsFormat = graphicsFormat;
            renderTexture.Create();
        }
    }

    void CullUI(CommandBuffer cmd, ScriptableRenderContext ctx, Camera camera)
    {
        // TODO: add an option to reuse the culling from last frame
        if (updateCulling)
        {
            using (new ProfilingScope(cmd, cullingSampler))
            {
                camera.TryGetCullingParameters(out var cullingParameters);
                cullingParameters.cullingOptions = CullingOptions.None;
                cullingParameters.cullingMask = (uint)uiLayerMask.value;
                cullingResults = ctx.Cull(ref cullingParameters);
            }
        }

        // Disables one time culling when not in play mode to be able to correctly edit the UI
        if (oneTimeCulling && !Application.isPlaying)
            updateCulling = false;

    }

    void RenderUI(CommandBuffer cmd, ScriptableRenderContext ctx, Camera camera)
    {
        using (new ProfilingScope(cmd, renderingSampler))
        {
            CoreUtils.SetRenderTarget(cmd, renderTexture, ClearFlag.All);

            var drawSettings = new DrawingSettings
            {
                sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent | SortingCriteria.CanvasOrder | SortingCriteria.RendererPriority }
            };
            for (int i = 0; i < hdTransparentPassNames.Length; i++)
                drawSettings.SetShaderPassName(i, hdTransparentPassNames[i]);

            var filterSettings = new FilteringSettings(RenderQueueRange.all, uiLayerMask);

            ctx.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            ctx.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }
    }

    void DoRenderUI(ScriptableRenderContext ctx, HDCamera hdCamera)
    {
        var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp == null)
            return;

        UpdateRenderTexture(hdCamera.camera);

        var cmd = CommandBufferPool.Get();

        // Setup render context for rendering GUI
        ScriptableRenderContext.EmitGeometryForCamera(hdCamera.camera);
        ctx.SetupCameraProperties(hdCamera.camera, hdCamera.xr.enabled);

        // Setup HDRP camera properties to render HDRP shaders
        hdrp.UpdateCameraCBuffer(cmd, hdCamera);

        using (new ProfilingScope(cmd, uiCameraStackingSampler))
        {
            CullUI(cmd, ctx, hdCamera.camera);
            RenderUI(cmd, ctx, hdCamera.camera);
        }
        ctx.ExecuteCommandBuffer(cmd);
        ctx.Submit();
    }
}
