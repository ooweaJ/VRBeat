Shader "VRBeat/NoteBlock"
{
    Properties
    {
        [HDR] _Color          ("Note Color (HDR)", Color) = (0.08, 0.01, 0.01, 1)
        [HDR] _CircleColor    ("Center Circle Color (HDR)", Color) = (5, 5, 5, 1)
        [HDR] _EdgeColor      ("Edge Glow (HDR)", Color) = (10, 1.5, 1.5, 1)
        [HDR] _InnerGlowColor ("Inner Glow Color (HDR)", Color) = (6, 0.5, 0.5, 1)
        _EdgeWidth            ("Edge Width", Range(0.02, 0.20)) = 0.09
        _CircleRadius         ("Circle Radius", Range(0.05, 0.6)) = 0.32
        _InnerGlowRadius      ("Inner Glow Radius", Range(0.1, 1.0)) = 0.60
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        // ── 메인 패스 ──────────────────────────────────────────
        Pass
        {
            Name "NoteBlock"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back

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
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _CircleColor;
                half4  _EdgeColor;
                half4  _InnerGlowColor;
                float  _EdgeWidth;
                float  _CircleRadius;
                float  _InnerGlowRadius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // 중심 원형 글로우 마스크 (하드 코어 + 소프트 후광)
            float CircleGlow(float2 uv, float radius)
            {
                float d    = length((uv - 0.5) * 2.0);
                float core = 1.0 - smoothstep(0.0,          radius * 0.55, d); // 밝은 코어
                float halo = 1.0 - smoothstep(radius * 0.5, radius,        d); // 부드러운 후광
                return saturate(core * 2.0 + halo);
            }

            // 테두리 발광 마스크
            float EdgeMask(float2 uv, float w)
            {
                float2 d   = min(uv, 1.0 - uv);
                float  edg = min(d.x, d.y);
                return 1.0 - smoothstep(0.0, w, edg);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv  = IN.uv;

                // 중심 원형 (화살표 대신)
                float circle = CircleGlow(uv, _CircleRadius);

                // 테두리 발광
                float edge = EdgeMask(uv, _EdgeWidth);

                // 부드러운 내부 후광 (원 주변 넓은 글로우)
                float centerDist = length((uv - 0.5) * 2.0);
                float innerGlow  = pow(saturate(1.0 - centerDist / _InnerGlowRadius), 2.5);

                half4 col  = _Color;                             // 어두운 바디
                col.rgb   += _InnerGlowColor.rgb * innerGlow;    // 내부 후광 (소프트)
                col.rgb   += _CircleColor.rgb * circle;          // 밝은 중심 원 (블룸 유발)
                col.rgb   += _EdgeColor.rgb * edge;              // 엣지 HDR 발광

                col.a = 1;
                return col;
            }
            ENDHLSL
        }

        // ── 그림자 패스 ────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS  = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                OUT.positionHCS = ApplyShadowClamping(posCS);
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ── Depth Normals ─────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vertDN
            #pragma fragment fragDN
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionHCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vertDN(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 fragDN(Varyings IN) : SV_Target
            {
                return half4(IN.normalWS * 0.5 + 0.5, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
