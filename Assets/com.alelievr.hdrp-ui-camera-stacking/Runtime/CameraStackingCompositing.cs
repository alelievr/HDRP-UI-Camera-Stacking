using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using UnityEngine.Experimental.Rendering;


#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
#endif

public static class CameraStackingCompositing
{
    public static ProfilingSampler compositingSampler;
    public static List<HDCameraUI> uiList = new();
    public static Dictionary<Camera, HDAdditionalCameraData> hdAdditionalCameraData = new();
    public static Material compositingMaterial;
    public static Material backgroundBlitMaterial;
    static MaterialPropertyBlock uiProperties = new();

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

        TryInitMaterials();
    }

    static void TryInitMaterials()
    {
        if (compositingMaterial != null && backgroundBlitMaterial != null)
            return;

        var uiCompositingShader = Shader.Find("Hidden/HDRP/UI_Compositing");
        var initShader = Shader.Find("Hidden/HDRP/InitTransparentUIBackground");
        if (compositingMaterial == null && uiCompositingShader != null)
            compositingMaterial = CoreUtils.CreateEngineMaterial(uiCompositingShader);
        if (backgroundBlitMaterial == null && initShader != null)
            backgroundBlitMaterial = CoreUtils.CreateEngineMaterial(initShader);
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

        TryInitMaterials();

        var cmd = CommandBufferPool.Get();
        uiList.Sort((c0, c1) => c0.priority.CompareTo(c1.priority));
        using (new ProfilingScope(cmd, compositingSampler))
        {
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            foreach (var ui in uiList)
            {
                if (ui.IsActive())
                {
                    // Check if the target camera in HDCameraUI matches the current camera
                    switch (ui.targetCamera)
                    {
                        case HDCameraUI.TargetCamera.Main:
                            if (camera != Camera.main)
                                continue;
                            break;
                        case HDCameraUI.TargetCamera.Layer:
                            if (((1 << camera.gameObject.layer) & ui.targetCameraLayer) == 0)
                                continue;
                            break;
                        case HDCameraUI.TargetCamera.Specific:
                            if (camera != ui.targetCameraObject)
                                continue;
                            break;
                    }

                    // Render the UI of the camera using the current back buffer as clear value
                    RenderTexture target = camera.targetTexture;
                    ui.DoRenderUI(ctx, cmd, target);

                    bool bindArray = ui.renderTexture.dimension == TextureDimension.Tex2DArray;
                    uiProperties.SetTexture("_MainTex2D", bindArray ? Texture2D.whiteTexture : ui.renderTexture);
                    uiProperties.SetTexture("_MainTex2DArray", bindArray ? ui.renderTexture : (Texture)TextureXR.GetWhiteTexture());
                    uiProperties.SetInt("_Is2DArray", bindArray ? 1 : 0);

                    if (HDUtils.TryGetAdditionalCameraDataOrDefault(camera).xrRendering)
                    {
                        var display = XRSystem.GetActiveDisplay();
                        if (display == null || !display.running)
                        {
                            FinalCompositing(target, camera.pixelRect);
                            continue;
                        }
                        var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                        int mirrorBlitMode = display.GetPreferredMirrorBlitMode();
                        display.GetMirrorViewBlitDesc(null, out var blitDesc, mirrorBlitMode);
                        for (int i = 0; i < blitDesc.blitParamsCount; ++i)
                        {
                            blitDesc.GetBlitParameter(i, out var blitParams);
                            FinalCompositing(blitParams.srcTex, new Rect(0, 0, blitParams.srcTex.width, blitParams.srcTex.height), blitParams.srcTexArraySlice);
                        }
                    }
                    else
                    {
                        FinalCompositing(target, camera.pixelRect);
                    }

                    void FinalCompositing(RenderTexture outputRT, Rect viewport, int rtSliceIndex = 0)
                    {
                        cmd.SetRenderTarget(outputRT, 0, CubemapFace.Unknown, rtSliceIndex);
                        cmd.SetViewport(viewport);
                        // Do the UI compositing
                        switch (ui.compositingMode)
                        {
                            default:
                            case HDCameraUI.CompositingMode.Automatic:
                                if (camera.targetTexture != null)
                                    cmd.DrawProcedural(Matrix4x4.identity, compositingMaterial, 0, MeshTopology.Triangles, 3, 1, uiProperties);
                                else
                                    cmd.DrawProcedural(Matrix4x4.identity, compositingMaterial, 0, MeshTopology.Triangles, 3, 1, uiProperties);
                                break;
                            case HDCameraUI.CompositingMode.Custom:
                                if (ui.compositingMaterial != null)
                                {
                                    if (camera.targetTexture != null)
                                        cmd.DrawProcedural(Matrix4x4.identity, ui.compositingMaterial, ui.compositingMaterialPass, MeshTopology.Triangles, 3, 1, uiProperties);
                                    else
                                        cmd.DrawProcedural(Matrix4x4.identity, ui.compositingMaterial, ui.compositingMaterialPass, MeshTopology.Triangles, 3, 1, uiProperties);
                                }
                                break;
                            case HDCameraUI.CompositingMode.Manual:
                                // The user manually composite the UI.
                                break;
                        }
                    }
                }
            }
        }
        ctx.ExecuteCommandBuffer(cmd);
        ctx.Submit();
        CommandBufferPool.Release(cmd);
        // Set back the render target to camera one otherwise it causes side effects.
        RenderTexture.active = null;
    }
}
