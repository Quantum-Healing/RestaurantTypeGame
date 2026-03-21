// Shader for power wires - glows when powered, dim when off.
Shader "KitchenEmpire/Wire"
{
    Properties
    {
        _PoweredColor ("Powered Color", Color) = (0.376, 0.647, 0.980, 1)
        _UnpoweredColor ("Unpowered Color", Color) = (0.216, 0.255, 0.318, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.5
        _Powered ("Powered", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _PoweredColor;
            float4 _UnpoweredColor;
            float _GlowIntensity;
            float _Powered;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float4 baseColor = lerp(_UnpoweredColor, _PoweredColor, _Powered);

                // Animated glow when powered
                if (_Powered > 0.5)
                {
                    float glow = sin(_Time.y * 2.0) * 0.15 + 0.85;
                    baseColor.rgb *= glow * (1.0 + _GlowIntensity);

                    // Edge glow falloff
                    float edge = abs(i.uv.y - 0.5) * 2.0;
                    baseColor.a = lerp(1.0, 0.3, edge);
                }
                else
                {
                    baseColor.a = 0.6;
                }

                return baseColor;
            }
            ENDCG
        }
    }
}
