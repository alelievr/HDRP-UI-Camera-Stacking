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
    public static List<HDCameraUI> uiList = new List<HDCameraUI>();
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
        uiList.Sort((c0, c1) => c0.priority.CompareTo(c1.priority));
        using (new ProfilingScope(cmd, compositingSampler))
        {
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            foreach (var ui in uiList)
            {
                if (ui.IsActive())
                {
                    switch (ui.compositingMode)
                    {
                        default:
                        case HDCameraUI.CompositingMode.Automatic:
                            if (camera.targetTexture != null)
                                cmd.Blit(ui.renderTexture, Display.activeEditorGameViewTarget, blitWithBlendingMaterial);
                            else
                                cmd.Blit(ui.renderTexture, BuiltinRenderTextureType.CameraTarget, blitWithBlendingMaterial);
                            break;
                        case HDCameraUI.CompositingMode.Custom:
                            if (ui.compositingMaterial != null)
                            {
                                if (camera.targetTexture != null)
                                    cmd.Blit(ui.renderTexture, Display.activeEditorGameViewTarget, ui.compositingMaterial, ui.compositingMaterialPass);
                                else
                                    cmd.Blit(ui.renderTexture, BuiltinRenderTextureType.CameraTarget, ui.compositingMaterial, ui.compositingMaterialPass);
                            }
                            break;
                        case HDCameraUI.CompositingMode.Manual:
                            // The user manually composite the UI.
                            break;
                    }
                }
            }
        }
        ctx.ExecuteCommandBuffer(cmd);
        ctx.Submit();
        CommandBufferPool.Release(cmd);
    }
}
