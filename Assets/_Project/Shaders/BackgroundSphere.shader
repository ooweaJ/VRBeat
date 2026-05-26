Shader "VRBeat/BackgroundSphere"
{
    Properties
    {
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _Intensity     ("Intensity",      Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Front
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _EmissionColor;
            float  _Intensity;

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos    : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_EmissionColor.rgb * _Intensity, 1);
            }
            ENDCG
        }
    }
}
