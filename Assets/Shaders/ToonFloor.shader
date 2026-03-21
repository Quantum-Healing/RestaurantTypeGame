// Simple floor shader with checkerboard pattern for the isometric grid.
Shader "KitchenEmpire/ToonFloor"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.91, 0.84, 0.72, 1)
        _ColorB ("Color B", Color) = (0.87, 0.79, 0.66, 1)
        _GridColor ("Grid Line Color", Color) = (0, 0, 0, 0.08)
        _GridWidth ("Grid Line Width", Range(0, 0.1)) = 0.02
        _Highlight ("Highlight", Float) = 0
        _HighlightColor ("Highlight Color", Color) = (0.29, 0.85, 0.5, 0.4)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _ColorA;
            float4 _ColorB;
            float4 _GridColor;
            float _GridWidth;
            float _Highlight;
            float4 _HighlightColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Checkerboard based on world position
                float2 grid = floor(i.worldPos.xz * 2.0);
                float checker = fmod(grid.x + grid.y, 2.0);
                float4 color = lerp(_ColorA, _ColorB, checker);

                // Grid lines
                float2 frac_pos = frac(i.worldPos.xz * 2.0);
                float2 grid_line = smoothstep(0, _GridWidth, frac_pos) *
                                   smoothstep(0, _GridWidth, 1.0 - frac_pos);
                float line = 1.0 - min(grid_line.x, grid_line.y);
                color = lerp(color, _GridColor, line * _GridColor.a);

                // Highlight overlay
                if (_Highlight > 0.5)
                {
                    color = lerp(color, _HighlightColor, _HighlightColor.a);
                }

                color.a = 1.0;
                return color;
            }
            ENDCG
        }
    }
}
