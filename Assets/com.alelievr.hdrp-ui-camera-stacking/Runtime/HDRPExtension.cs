using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        public void UpdateCameraCBuffer(CommandBuffer cmd, HDCamera hdCamera)
        {
            hdCamera.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB);

            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
        }

        public void ApplyPostProcessOnRenderTexture(CommandBuffer cmd, ScriptableRenderContext ctx, HDCamera hdCamera, CullingResults cullingResults, RenderTexture target)
        {
            PrepassOutput fakePrepass = new PrepassOutput();

            // TODO: create "fake" render graph here

            var renderGraphParams = new RenderGraphParameters()
            {
                scriptableRenderContext = ctx,
                commandBuffer = cmd,
                currentFrameIndex = m_FrameCount
            };

            m_RenderGraph.Begin(renderGraphParams);
 
            var backHandle = m_RenderGraph.CreateTexture(
                new TextureDesc(target.width, target.height, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    dimension = TextureDimension.Tex2DArray,
                    enableRandomWrite = true,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = false,
                    name = "UI PostProcess BackBuffer"
                });

            var settings = hdCamera.frameSettings;

            RTHandles.SetReferenceSize(target.width, target.height, MSAASamples.None);

            // These post processes and features are not supported right now (they use depth, normal or motion vector buffer)
            settings.SetEnabled(FrameSettingsField.AfterPostprocess, false);
            settings.SetEnabled(FrameSettingsField.DepthOfField, false);
            settings.SetEnabled(FrameSettingsField.MotionBlur, false);
            settings.SetEnabled(FrameSettingsField.MotionVectors, false);

            // TODO: If there are problems with bloom, disable it in frame settings

            typeof(HDCamera).GetProperty(nameof(HDCamera.frameSettings)).SetValue(hdCamera, settings);

            var colorBuffer = m_RenderGraph.ImportTexture(RTHandles.Alloc(target));
            Debug.Log(target.dimension);

            // Patch debug display settings for 11.x and above
            m_CurrentDebugDisplaySettings = s_NeutralDebugDisplaySettings;

            m_PostProcessSystem.BeginFrame(cmd, hdCamera, RenderPipelineManager.currentPipeline as HDRenderPipeline);
            // BeginPostProcessFrame(); // For HDRP 12.x
            TextureHandle postProcessDest = RenderPostProcess(m_RenderGraph, fakePrepass, colorBuffer, backHandle, cullingResults, hdCamera);

            // m_RenderGraph.defaultResources.blackTextureXR;
            m_PostProcessSystem.Render(
                m_RenderGraph,
                hdCamera,
                m_BlueNoise,
                colorBuffer,
                m_RenderGraph.defaultResources.blackTextureXR,
                m_RenderGraph.defaultResources.blackTextureXR,
                m_RenderGraph.defaultResources.blackTextureXR,
                m_RenderGraph.defaultResources.blackTextureXR,
                m_RenderGraph.defaultResources.blackTextureXR,
                backHandle, // We probably need another target here :(
                true
            );

            using (var builder = m_RenderGraph.AddRenderPass<FinalBlitPassData>("UI Post Process to Render Target", out var passData))
            {
                int viewIndex = 0; // TODO: VR
                passData.parameters = PrepareFinalBlitParameters(hdCamera, viewIndex); // todo viewIndex
                passData.source = builder.ReadTexture(postProcessDest);
                passData.destination = builder.WriteTexture(m_RenderGraph.ImportTexture(RTHandles.Alloc(target)));

                builder.SetRenderFunc(
                    (FinalBlitPassData data, RenderGraphContext context) =>
                    {
                        BlitFinalCameraTexture(data.parameters, context.renderGraphPool.GetTempMaterialPropertyBlock(), data.source, data.destination, context.cmd);
                    });
            }


            // TODO: VR support
            // for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            // BlitFinalCameraTexture(m_RenderGraph, hdCamera, postProcessDest, m_RenderGraph.ImportBackbuffer(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget)), 0);

            m_RenderGraph.Execute();
        }
    }
}