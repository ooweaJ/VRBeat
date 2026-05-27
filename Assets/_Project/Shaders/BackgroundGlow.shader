Shader "VRBeat/BackgroundGlow"
{
    Properties
    {
        [HDR] _Color    ("Glow Color", Color) = (0, 0.5, 1, 1)
        _Intensity      ("Intensity",  Float) = 1.0
        _Softness       ("Softness",   Range(0.3, 4.0)) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                float  _Intensity;
                float  _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.pos = TransformObjectToHClip(IN.pos.xyz);
                OUT.uv  = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 수평 그라디언트: 좌우 끝 = 0, 가운데 = 1 (기둥 형태)
                float h = 1.0 - abs(IN.uv.x - 0.5) * 2.0;
                float hGrad = pow(saturate(h), _Softness);

                // 상하 부드럽게 페이드 (위아래 끝 소멸)
                float v = 1.0 - abs(IN.uv.y - 0.5) * 2.0;
                float vGrad = pow(saturate(v), 0.3);

                float glow = hGrad * vGrad * _Intensity;
                return half4(_Color.rgb * glow, 1.0);
            }
            ENDHLSL
        }
    }
}
