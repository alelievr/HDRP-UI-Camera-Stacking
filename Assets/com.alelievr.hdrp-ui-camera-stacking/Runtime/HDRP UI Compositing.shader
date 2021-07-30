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
                    float4 c = _MainTex.SampleLevel(s_linear_clamp_sampler, float3(i.uv, 0), 0);

                    // float lum = c.r*.3 + c.g*.59 + c.b*.11;
                    // float3 bw = float3( lum, lum, lum ); 
                    
                    // float4 result = c;
                    // result.rgb = lerp(c.rgb, bw, _bwBlend);
                    return c;
                }
            ENDCG
        }
    }
}
