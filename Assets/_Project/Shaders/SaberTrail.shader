Shader "VRBeat/SaberTrail"
{
    Properties
    {
        _BaseColor ("Color (HDR)", Color) = (1,1,1,1)
        _MainTex   ("Trail Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+1"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "SaberTrail"
            Blend SrcAlpha One      // Additive: 밝을수록 더 밝아짐
            ZWrite Off
            ZTest LEqual
            Cull Off                // 양면 렌더링

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 col;
                col.rgb = _BaseColor.rgb * IN.color.rgb * tex.rgb;
                col.a   = IN.color.a * tex.a;
                return col;
            }
            ENDHLSL
        }
    }
}
