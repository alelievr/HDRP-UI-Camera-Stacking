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
    }
}