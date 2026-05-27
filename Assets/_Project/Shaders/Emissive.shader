Shader "VRBeat/Emissive"
{
    // 단순 HDR 단색 출력 셰이더. 프로젝트의 Bloom이 HDR 색을 발광으로 처리.
    // 색은 MaterialPropertyBlock 의 _Color 로 렌더러별 오버라이드 가능.
    Properties
    {
        [HDR] _Color ("Color (HDR)", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return _Color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
