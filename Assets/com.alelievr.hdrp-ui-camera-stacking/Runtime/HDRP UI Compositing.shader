Shader "Hidden/HDRP/UI_Compositing"
{
    Properties
    {
        _MainTex2D("Main Texture", 2D) = "white" {}
        _MainTex2DArray("Main Texture", 2DArray) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/TextureXR.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    // We don't support XR so it's fine to do that + this makes it compatible with camera Render Textures
    TEXTURE2D(_MainTex2D);
    TEXTURE2D_ARRAY(_MainTex2DArray);
    int _Is2DArray;

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings FullscreenVertex(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        output.uv = output.positionCS.xy * 0.5 + 0.5;
        return output;
    }

    float4 Compositing(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float2 uv = varyings.uv;
        if (_Is2DArray == 0)
            return SAMPLE_TEXTURE2D_LOD(_MainTex2D, s_linear_clamp_sampler, uv, 0);
        else
            return SAMPLE_TEXTURE2D_ARRAY_LOD(_MainTex2DArray, s_linear_clamp_sampler, uv, 0, 0); // VR not supported
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "HDRP UI Compositing"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment Compositing
                #pragma vertex FullscreenVertex
            ENDHLSL
        }
    }
    Fallback Off
}
