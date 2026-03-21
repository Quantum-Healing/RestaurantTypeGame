// Cartoony toon shader for PlateUp!-style 2.5D isometric look.
// Features:
// - Flat/stepped shading (2-3 bands)
// - Thick outlines
// - Bright, saturated colors
// - No complex PBR, just clean cartoony visuals
Shader "KitchenEmpire/ToonIsometric"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.7, 0.7, 0.8, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _OutlineColor ("Outline Color", Color) = (0.1, 0.1, 0.15, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.03)) = 0.008
        _Selected ("Selected", Float) = 0
        _SelectionColor ("Selection Color", Color) = (0.29, 0.85, 0.5, 1)
        _TopColor ("Top Color", Color) = (1, 1, 1, 1)
        _FrontColor ("Front Color", Color) = (0.8, 0.8, 0.8, 1)
        _SideColor ("Side Color", Color) = (0.7, 0.7, 0.7, 1)
        _AccentColor ("Accent Color", Color) = (1, 0.9, 0.3, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // Pass 1: Outline
        Pass
        {
            Name "Outline"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Pass 2: Main toon shading
        Pass
        {
            Name "ToonShading"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4 _MainColor;
            float4 _ShadowColor;
            float _ShadowThreshold;
            float _Selected;
            float4 _SelectionColor;
            float4 _TopColor;
            float4 _FrontColor;
            float4 _SideColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Determine face color based on normal direction
                // Top face: normal points up
                // Front face: normal points toward camera (positive z in iso)
                // Side face: normal points sideways
                float topFactor = saturate(dot(normal, float3(0, 1, 0)));
                float frontFactor = saturate(dot(normal, float3(0, 0, -1)));
                float sideFactor = saturate(dot(normal, float3(1, 0, 0)));

                float4 baseColor = _MainColor;
                baseColor = lerp(baseColor, _TopColor, topFactor * 0.8);
                baseColor = lerp(baseColor, _FrontColor, frontFactor * 0.6);
                baseColor = lerp(baseColor, _SideColor, sideFactor * 0.6);

                // Stepped toon lighting
                float NdotL = dot(normal, lightDir);
                float toon = NdotL > _ShadowThreshold ? 1.0 : 0.7;

                float4 finalColor = baseColor * toon;
                finalColor = lerp(finalColor, finalColor * _ShadowColor, 1.0 - toon);

                // Selection glow
                if (_Selected > 0.5)
                {
                    float pulse = sin(_Time.y * 3.0) * 0.1 + 0.9;
                    finalColor = lerp(finalColor, _SelectionColor, 0.3 * pulse);
                }

                finalColor.a = 1.0;
                return finalColor;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
