using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// TODO: Create menu item with correct setup

namespace UnityEngine.Rendering.HighDefinition
{
    [ExecuteAlways]
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class HDCameraUI : MonoBehaviour
    {
        HDAdditionalCameraData cameraData;

        // TODO: share with other RTHandle
        RTHandle uiHandle;

        Material uiBlit;

        // Start is called before the first frame update
        [System.NonSerialized]
        new bool enabled = false;

        void OnEnable()
        {
            if (enabled)
                OnDisable();
            enabled = true;
            cameraData = GetComponent<HDAdditionalCameraData>();
            cameraData.customRender -= RenderUI;
            cameraData.customRender += RenderUI;

            uiHandle = RTHandles.Alloc(
                Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                useDynamicScale: true, name: "HDRP UI Buffer"
            );

            // TODO: move to another object
            RenderPipelineManager.endCameraRendering -= ApplyUI;
            RenderPipelineManager.endCameraRendering += ApplyUI;

            Debug.Log("OnEnable");

            uiBlit = CoreUtils.CreateEngineMaterial("Hidden/Unlit/UIBlit");
        }

        void OnDisable()
        {
            Debug.Log("OnDisable");
            enabled = false;
            cameraData.customRender -= RenderUI;
            RenderPipelineManager.endCameraRendering -= ApplyUI;
            uiHandle.Release();
            CoreUtils.Destroy(uiBlit);
            uiHandle = null;
        }

        void OnDestroy()
        {
            uiHandle?.Release();
        }

        void RenderUI(ScriptableRenderContext ctx, HDCamera hdCamera)
        {
            // TODO: cache
            if (hdCamera.camera != GetComponent<Camera>() && hdCamera.camera.cameraType == CameraType.Game)
                return;

            Debug.Log("Custom Render");
            // ctx.DrawUIOverlay(hdCamera.camera);

            ScriptableRenderContext.EmitGeometryForCamera(hdCamera.camera);
            // ScriptableRenderContext.EmitWorldGeometryForSceneView(hdCamera.camera);

            hdCamera.camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = ctx.Cull(ref cullingParameters);

            // var drawSettings = new DrawingSettings();
            // drawSettings.SetShaderPassName(0, HDShaderPassNames.s_ForwardOnlyName);
            // drawSettings.SetShaderPassName(1, HDShaderPassNames.s_ForwardName);
            // drawSettings.SetShaderPassName(2, HDShaderPassNames.s_SRPDefaultUnlitName);
            // drawSettings.perObjectData = PerObjectData.MotionVectors;
            // drawSettings.sortingSettings = new SortingSettings(hdCamera.camera) { criteria = SortingCriteria.CommonTransparent };

            // var filter = new FilteringSettings(RenderQueueRange.all, hdCamera.camera.cullingMask);
            // filter.excludeMotionVectorObjects = false;
            // filter = FilteringSettings.defaultValue;

            // var stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            // // Bind UI buffer:
            Debug.Log(hdCamera.camera);
            var cmd = CommandBufferPool.Get("test2");
            CoreUtils.SetRenderTarget(cmd, uiHandle);
            cmd.ClearRenderTarget(false, true, Color.clear);
            
            // ctx.DrawRenderers(cullingResults, ref drawSettings, ref filter);

            ShaderTagId[] litForwardTags = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName, new ShaderTagId("Default")};

            var result = new RendererListDesc(litForwardTags, cullingResults, hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.transparent,
                sortingCriteria = SortingCriteria.CommonTransparent,
                excludeObjectMotionVectors = false,
                layerMask = hdCamera.camera.cullingMask,
            };

            CoreUtils.DrawRendererList(ctx, cmd, RendererList.Create(result));

            // var result = new RendererListDesc(litForwardTags, cullingResults, hdCamera.camera)
            // {
            //     rendererConfiguration = PerObjectData.None,
            //     renderQueueRange = ,
            //     sortingCriteria = SortingCriteria.CommonTransparent,
            //     excludeObjectMotionVectors = false,
            //     layerMask = ,
            // };

            // var cmd = CommandBufferPool.Get("UI Rendering");
            // CoreUtils.DrawRendererList(ctx, cmd, );
            // ctx.ExecuteCommandBuffer(cmd);

            ctx.ExecuteCommandBuffer(cmd);
        }

        void ApplyUI(ScriptableRenderContext ctx, Camera camera)
        {
            if (camera.cameraType != CameraType.Game)
                return;

            if (uiHandle != null)
            {
                var cmd = CommandBufferPool.Get("Apply UI Buffer");

                uiBlit.SetTexture("_MainTex2", uiHandle);
                cmd.Blit(Texture2D.whiteTexture, BuiltinRenderTextureType.CameraTarget, uiBlit);

                ctx.ExecuteCommandBuffer(cmd);
            }
        }
    }
}
