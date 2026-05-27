Shader "VRBeat/NeonPanel"
{
    Properties
    {
        [HDR] _BorderColor  ("Border Color (HDR)", Color) = (0, 2, 4, 1)
        _FillColor          ("Fill Color",          Color) = (0.0, 0.02, 0.06, 0.85)
        _BorderWidth        ("Border Width",        Range(0.005, 0.12)) = 0.025
        _CornerRadius       ("Corner Radius",       Range(0.0,  0.45))  = 0.12
        _GlowSize           ("Glow Size",           Range(0.0,  0.25))  = 0.07
        _GlowIntensity      ("Glow Intensity",      Range(0.0,  6.0))   = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "NeonPanel"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _BorderColor;
                half4  _FillColor;
                float  _BorderWidth;
                float  _CornerRadius;
                float  _GlowSize;
                float  _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            // Rounded rectangle SDF
            float sdRoundRect(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv - 0.5;

                // 화면 공간 미분으로 패널 종횡비 자동 계산
                float dxdu   = abs(ddx(uv.x));
                float dydu   = abs(ddy(uv.y));
                float aspect = (dxdu > 0.00001) ? (dydu / dxdu) : 1.0;

                // height-normalized 공간에서 SDF 계산
                float2 p  = float2(uv.x * aspect, uv.y);
                float  r  = _CornerRadius;
                float2 b  = max(float2(0.5 * aspect - r, 0.5 - r), 0.001);
                float  d  = sdRoundRect(p, b, r);
                float  fw = fwidth(d);

                // 내부/외부 마스크
                float inside_rect  = smoothstep( fw, -fw, d);
                float bw           = _BorderWidth * aspect;
                float inside_inner = smoothstep( fw, -fw, d + bw);
                float border       = saturate(inside_rect - inside_inner);

                // 외부 글로우 (HDR → Bloom이 번짐)
                float outerGlow = exp(-max(d,  0.0) / max(_GlowSize, 0.001))
                                * _GlowIntensity * (1.0 - inside_rect);
                // 내부 rim 글로우
                float innerGlow = exp(-max(-d, 0.0) / max(_GlowSize * 0.4, 0.001))
                                * _GlowIntensity * 0.25 * inside_inner;

                half4 col = 0;

                // 배경 fill
                col.rgb += _FillColor.rgb * inside_rect;
                col.a   += _FillColor.a   * inside_rect;

                // 네온 테두리
                col.rgb  = lerp(col.rgb, _BorderColor.rgb, border);
                col.a    = max(col.a, border);

                // 외부 글로우
                col.rgb += _BorderColor.rgb * outerGlow;
                col.a    = max(col.a, saturate(outerGlow * 0.65));

                // 내부 rim
                col.rgb += _BorderColor.rgb * innerGlow;

                col.a = saturate(col.a);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
