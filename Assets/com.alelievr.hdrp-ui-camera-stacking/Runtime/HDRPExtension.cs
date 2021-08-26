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

            fakePrepass.normalBuffer =  m_RenderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = true,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = false,
                    name = "UI PostProcess RG"
                });
            fakePrepass.depthAsColor =  m_RenderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = true,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = false,
                    name = "UI PostProcess RG"
                });
            fakePrepass.motionVectorsBuffer =  m_RenderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = true,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = false,
                    name = "UI PostProcess RG"
                });
            
            var colorHandle = m_RenderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = true,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = false,
                    name = "UI PostProcess RG"
                });

            var backHandle = m_RenderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = true,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = false,
                    name = "UI PostProcess RG"
                });

            // TODO: create "fake" render graph here

            var renderGraphParams = new RenderGraphParameters()
            {
                scriptableRenderContext = ctx,
                commandBuffer = cmd,
                currentFrameIndex = m_FrameCount
            };

            m_RenderGraph.Begin(renderGraphParams);

            var settings = hdCamera.frameSettings;
            settings.SetEnabled(FrameSettingsField.AfterPostprocess, false);

            // These post processes are not supported right now (they use depth, normal or motion vector buffer)
            settings.SetEnabled(FrameSettingsField.DepthOfField, false);
            settings.SetEnabled(FrameSettingsField.MotionBlur, false);

            typeof(HDCamera).GetProperty(nameof(HDCamera.frameSettings)).SetValue(hdCamera, settings);

            // Debug.Log(hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess));
            
            m_CurrentDebugDisplaySettings = s_NeutralDebugDisplaySettings;
            m_PostProcessSystem.BeginFrame(cmd, hdCamera, RenderPipelineManager.currentPipeline as HDRenderPipeline);
            // BeginPostProcessFrame(); // For HDRP 12.x
            TextureHandle postProcessDest = RenderPostProcess(m_RenderGraph, fakePrepass, colorHandle, backHandle, cullingResults, hdCamera);
            m_RenderGraph.Execute();
            m_RenderGraph.EndFrame();

            // TODO: copy back to target ?
        }
    }
}