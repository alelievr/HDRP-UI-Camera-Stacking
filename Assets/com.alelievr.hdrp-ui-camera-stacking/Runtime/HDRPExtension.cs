using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#else
using UnityEngine.Experimental.Rendering.RenderGraphModule;
#endif

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