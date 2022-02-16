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
    public static Material compositingMaterial;
    static MaterialPropertyBlock uiProperties = new MaterialPropertyBlock();

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

        if (compositingMaterial == null)
            compositingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/HDRP/UI_Compositing"));
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

                    uiProperties.SetTexture("_MainTex", ui.renderTexture);

                    if (camera.targetTexture != null)
                        cmd.SetRenderTarget(camera.targetTexture);
                    else
                        cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                    cmd.SetViewport(camera.pixelRect);

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
        ctx.ExecuteCommandBuffer(cmd);
        ctx.Submit();
        CommandBufferPool.Release(cmd);
    }
}
