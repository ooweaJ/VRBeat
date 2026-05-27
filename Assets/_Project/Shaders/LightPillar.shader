Shader "VRBeat/LightPillar"
{
    Properties
    {
        [HDR] _Color         ("Color (HDR)",     Color)        = (0, 0.5, 1, 1)
        _Intensity           ("Intensity",        Float)        = 1.0
        _RadialPower         ("Radial Softness",  Range(0.2, 4.0)) = 0.8
        _HeightFadePower     ("Height Fade",      Range(0.3, 4.0)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                float  _Intensity;
                float  _RadialPower;
                float  _HeightFadePower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.worldPos));

                // 카메라를 정면으로 바라보는 면 = 밝음
                // 옆으로 돌아가는 면 = 점점 투명 (기둥 느낌)
                float NdotV    = abs(dot(normal, viewDir));
                float radial   = pow(NdotV, _RadialPower);

                // 위아래 끝으로 갈수록 페이드
                float v        = IN.uv.y;
                float hFade    = pow(saturate(1.0 - abs(v - 0.5) * 2.0), _HeightFadePower);

                float brightness = radial * hFade * _Intensity;
                return half4(_Color.rgb * brightness, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
