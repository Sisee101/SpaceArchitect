Shader "Unlit/2D/UnlitWarpGridContour"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GridColor ("Grid Color", Color) = (1,1,1,1)
        _BgColor ("Background Color", Color) = (0,0,0,1)
        _ContourColor ("Contour Line Color", Color) = (0,1,1,1)
        _LineThickness ("Line Thickness", Float) = 0.05
        _GridDensity ("Grid Density", Float) = 4.0
        _ContourDensity ("Contour Density (lines per unit)", Float) = 2.0
        _ContourTaper ("Contour Density Taper (reduce near strong gravity)", Float) = 0.5
        _ContourWidth ("Contour Line Width", Float) = 0.02
        _ContourWidthTaper ("Contour Width Taper (thicker near strong gravity)", Float) = 0.2
        _GravityScale ("Gravity Intensity Scale", Float) = 1.0
        _MinGravity ("Min Gravity Threshold", Float) = 0.01
        _MaxPlanets ("Max Planets", Int) = 8
        _ShowGrid ("Show Grid", Float) = 1.0
        _ShowContour ("Show Contour", Float) = 1.0
    }

    SubShader
    {
        // 将渲染队列下移到背景层，避免覆盖其他特效
        Tags { "Queue"="Background+50" "IgnoreProjector"="True" "RenderType"="Transparent" "SpriteMode"="Single" }
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
            float4 _ContourColor;
            float _LineThickness;
            float _GridDensity;
            float _ContourDensity;
            float _ContourTaper;
            float _ContourWidth;
            float _ContourWidthTaper;
            float _GravityScale;
            float _MinGravity;
            int _PlanetCount;
            float4 _Planets[MAX_PLANETS];
            float _ShowGrid;
            float _ShowContour;

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

            // 计算到网格线的距离
            float gridDistance(float coord)
            {
                float f = frac(coord);
                return min(f, 1.0 - f);
            }

            // 计算当前位置的总引力强度
            // 引力公式: F = G * M / r^2，这里简化为 M / r^2
            float CalculateGravityStrength(float2 worldPos)
            {
                float totalGravity = 0.0;
                
                for (int p = 0; p < MAX_PLANETS; p++)
                {
                    if (p >= _PlanetCount) break;
                    
                    float2 diff = worldPos - _Planets[p].xy;
                    float dist = max(length(diff), 0.0001);
                    float mass = _Planets[p].z;
                    
                    // 引力强度 = 质量 / 距离²
                    float gravity = mass / (dist * dist);
                    totalGravity += gravity;
                }
                
                return totalGravity * _GravityScale;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 计算引力强度
                float gravity = CalculateGravityStrength(i.worldPos);
                
                // 如果引力太小，直接返回背景色
                if (gravity < _MinGravity)
                {
                    return _BgColor;
                }
                
                fixed4 col = _BgColor;
                
                // 绘制等高线
                if (_ShowContour > 0.5)
                {
                    // 自适应等高线密度：引力越强，等高线越稀疏，避免密集噪点
                    float adaptiveDensity = _ContourDensity / (1.0 + gravity * _ContourTaper);

                    // 计算等高线值
                    float contourValue = gravity * adaptiveDensity;
                    
                    // 使用 frac 创建等高线效果
                    float contourFrac = frac(contourValue);
                    
                    // 计算到等高线的距离（在等高线附近时值接近0或1）
                    float distToContour = min(contourFrac, 1.0 - contourFrac);
                    
                    // 根据引力强度自适应线宽，强引力处线稍厚，进一步降低噪点感
                    float adaptiveWidth = lerp(_ContourWidth, _ContourWidth * (1.0 + _ContourWidthTaper), saturate(gravity));
                    
                    // 绘制等高线
                    float contourLine = smoothstep(adaptiveWidth, 0.0, distToContour);
                    
                    // 根据引力强度调整等高线颜色强度
                    float gravityIntensity = saturate(gravity * 0.1); // 调整这个值来控制颜色强度
                    float4 contourCol = lerp(_ContourColor, _GridColor, gravityIntensity);
                    contourCol.a *= contourLine;
                    
                    col = lerp(col, contourCol, contourLine);
                }
                
                // 绘制网格（可选）
                if (_ShowGrid > 0.5)
                {
                    float2 gridUV = i.uv * _GridDensity;
                    
                    float dx = gridDistance(gridUV.x);
                    float dy = gridDistance(gridUV.y);
                    
                    float lineX = smoothstep(_LineThickness, 0.0, dx);
                    float lineY = smoothstep(_LineThickness, 0.0, dy);
                    
                    float gridMask = 1.0 - (1.0 - lineX) * (1.0 - lineY);
                    
                    // 网格颜色根据引力强度调整
                    float4 gridCol = lerp(_BgColor, _GridColor, 0.3 + gravity * 0.1);
                    gridCol.a *= gridMask * 0.5; // 网格半透明
                    
                    col = lerp(col, gridCol, gridMask);
                }
                
                // 确保透明度正确
                col.a = saturate(col.a);
                
                return col;
            }
            ENDCG
        }
    }
}

