using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
#endif

public static class CameraStackingCompositing
{
    public static ProfilingSampler compositingSampler;
    public static HashSet<HDCameraUI> uiList = new HashSet<HDCameraUI>();
    public static Dictionary<Camera, HDAdditionalCameraData> hdAdditionalCameraData = new Dictionary<Camera, HDAdditionalCameraData>();
    public static Material blitWithBlendingMaterial;

    static CameraStackingCompositing()
    {
        OnLoad();
    }

    [RuntimeInitializeOnLoadMethod]
    static void OnLoad()
    {
        RenderPipelineManager.endCameraRendering -= EndCameraRendering;
        RenderPipelineManager.endCameraRendering += EndCameraRendering;
        compositingSampler = new ProfilingSampler("Composite UI Camera Stacking");

        if (blitWithBlendingMaterial == null)
            blitWithBlendingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/HDRP/UI_Compositing"));
    }

    static void EndCameraRendering(ScriptableRenderContext ctx, Camera camera)
    {
        if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline))
            return;

        // Only composite game camera with UI for now
        if (camera.cameraType != CameraType.Game)
            return;

        // Also skip camera that have a custom render (like UI only cameras)
        if (!hdAdditionalCameraData.TryGetValue(camera, out var hdData))
            hdData = hdAdditionalCameraData[camera] = camera.GetComponent<HDAdditionalCameraData>();
        if (hdData == null)
            hdData = hdAdditionalCameraData[camera] = camera.GetComponent<HDAdditionalCameraData>();

        if (hdData?.hasCustomRender == true)
            return;


        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, compositingSampler))
        {
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            foreach (var ui in uiList.OrderBy(e => e.priority))
                cmd.Blit(ui.renderTexture, BuiltinRenderTextureType.CameraTarget, blitWithBlendingMaterial);
        }
        ctx.ExecuteCommandBuffer(cmd);
        ctx.Submit();
    }
}
