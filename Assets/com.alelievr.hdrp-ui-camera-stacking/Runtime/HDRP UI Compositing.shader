Shader "Hidden/HDRP/UI_Compositing"
{
    Properties
    {
        _MainTex("Main Texture", 2DArray) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

                #pragma vertex vert_img
                #pragma fragment frag
        
                #include "UnityCG.cginc"

                Texture2DArray _MainTex;
                sampler s_linear_clamp_sampler;

                float4 frag(v2f_img i) : COLOR
                {
                    // TODO: VR support
                    return _MainTex.SampleLevel(s_linear_clamp_sampler, float3(i.uv, 0), 0);
                }
            ENDCG
        }
    }
}
