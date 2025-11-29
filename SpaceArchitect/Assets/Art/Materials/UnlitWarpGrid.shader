Shader "Unlit/2D/UnlitWarpGrid_Improved"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GridColor ("Grid Color", Color) = (1,1,1,1)
        _BgColor ("Background Color", Color) = (0,0,0,1)
        _LineThickness ("Line Thickness", Float) = 0.05
        _GridDensity ("Grid Density", Float) = 4.0
        _WarpStrengthX ("Warp Strength X", Float) = 0.02
        _WarpStrengthY ("Warp Strength Y", Float) = 0.6
        _Falloff ("Falloff (distance power)", Float) = 1.0
        _MaxPlanets ("Max Planets", Int) = 8
        _UseJitter ("Enable Jitter", Float) = 1
        _JitterX ("Jitter X", Float) = 0.02
        _JitterY ("Jitter Y", Float) = 0.002
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "SpriteMode"="Single" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_PLANETS 8

            sampler2D _MainTex;
            float4 _GridColor;
            float4 _BgColor;
            float _LineThickness;
            float _GridDensity;
            float _WarpStrengthX;
            float _WarpStrengthY;
            float _Falloff;
            int _PlanetCount;
            float4 _Planets[MAX_PLANETS];
            float _UseJitter;
            float _JitterX;
            float _JitterY;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xy;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float gridDistance(float coord)
            {
                float f = frac(coord);
                return min(f, 1.0 - f);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 可选 jitter
                float2 jitter = float2(0,0);
                if (_UseJitter > 0.5)
                {
                    jitter = float2(
                        (hash(i.worldPos * 100.0) - 0.5) * _JitterX,
                        (hash(i.worldPos * 50.0) - 0.5) * _JitterY
                    );
                }

                // warp 计算
                float2 warpOffset = float2(0,0);
                for (int p = 0; p < MAX_PLANETS; p++)
                {
                    if (p >= _PlanetCount) break;
                    float2 diff = i.worldPos - _Planets[p].xy;
                    float dist = max(length(diff), 0.0001);
                    float massEffect = _Planets[p].z / pow(dist, _Falloff);
                    warpOffset += massEffect * float2(_WarpStrengthX, _WarpStrengthY);
                }

                // 扭曲 UV
                float2 warpedUV = i.uv + jitter;
                warpedUV += warpOffset;

                // 网格坐标
                float2 gridUV = warpedUV * _GridDensity;

                // 计算距离线条的距离
                float dx = gridDistance(gridUV.x);
                float dy = gridDistance(gridUV.y);

                // 平滑线条
                float lineX = smoothstep(_LineThickness, 0.0, dx);
                float lineY = smoothstep(_LineThickness, 0.0, dy);

                // 交界平滑
                float gridMask = 1.0 - (1.0 - lineX) * (1.0 - lineY);

                float4 col = lerp(_BgColor, _GridColor, gridMask);
                col.a = saturate(gridMask * 1.5);

                return col;
            }
            ENDCG
        }
    }
}
