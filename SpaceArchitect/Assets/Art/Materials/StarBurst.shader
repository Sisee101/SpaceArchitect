Shader "Unlit/StarBurst"
{
    Properties
    {
        _CenterColor ("Center Color (HDR)", Color) = (2, 2, 2, 1)
        _MidColor ("Mid Color (HDR)", Color) = (2, 0.5, 1.5, 1)
        _OuterColor ("Outer Color (HDR)", Color) = (1, 0.2, 1.5, 1)
        _Intensity ("Intensity", Float) = 2.0
        _Size ("Size", Range(0.1, 2.0)) = 1.0
        _Falloff ("Falloff", Range(0.5, 5.0)) = 2.0
        _RayCount ("Ray Count", Range(4, 16)) = 8
        _RayWidth ("Ray Width", Range(0.01, 0.5)) = 0.1
        _RayIntensity ("Ray Intensity", Range(0, 2)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        _Softness ("Softness", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _CenterColor;
            float4 _MidColor;
            float4 _OuterColor;
            float _Intensity;
            float _Size;
            float _Falloff;
            float _RayCount;
            float _RayWidth;
            float _RayIntensity;
            float _Rotation;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 计算星形光线
            float GetStarRays(float2 uv, float rayCount, float rayWidth, float rotation)
            {
                // 将UV中心化到(0.5, 0.5)
                float2 centerUV = uv - 0.5;
                
                // 计算角度和距离
                float angle = atan2(centerUV.y, centerUV.x) + UNITY_PI;
                float dist = length(centerUV);
                
                // 应用旋转
                angle += radians(rotation);
                
                // 将角度归一化到[0, 2π]
                angle = fmod(angle + UNITY_PI * 2.0, UNITY_PI * 2.0);
                
                // 计算每个光线的角度
                float rayAngle = (UNITY_PI * 2.0) / rayCount;
                
                // 找到最近的光线角度
                float nearestRay = round(angle / rayAngle) * rayAngle;
                float angleDiff = abs(angle - nearestRay);
                
                // 确保角度差在[0, π]范围内
                if (angleDiff > UNITY_PI) angleDiff = UNITY_PI * 2.0 - angleDiff;
                
                // 计算光线强度（距离中心越远，光线越弱）
                float rayStrength = 1.0 - smoothstep(0.0, rayWidth, angleDiff);
                rayStrength *= (1.0 - smoothstep(0.3, 0.7, dist)); // 边缘衰减
                
                return rayStrength * _RayIntensity;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 将UV中心化并应用大小缩放
                float2 centerUV = (i.uv - 0.5) / _Size;
                float dist = length(centerUV);
                
                // 径向渐变（使用falloff控制衰减）
                float radialMask = 1.0 - saturate(pow(dist, _Falloff));
                
                // 颜色混合
                float midPoint = 0.4; // 中间色开始的位置
                float outerPoint = 0.7; // 外层色开始的位置
                
                float4 color;
                if (dist < midPoint)
                {
                    // 中心到中间：白色到粉红色
                    float t = dist / midPoint;
                    color = lerp(_CenterColor, _MidColor, t);
                }
                else if (dist < outerPoint)
                {
                    // 中间到外层：粉红色到紫色
                    float t = (dist - midPoint) / (outerPoint - midPoint);
                    color = lerp(_MidColor, _OuterColor, t);
                }
                else
                {
                    // 外层：紫色并衰减
                    float t = (dist - outerPoint) / (1.0 - outerPoint);
                    color = lerp(_OuterColor, float4(0, 0, 0, 0), t);
                }
                
                // 应用径向遮罩
                color *= radialMask;
                
                // 添加星形光线效果
                float rayMask = GetStarRays(i.uv, _RayCount, _RayWidth, _Rotation);
                color.rgb += rayMask * _CenterColor.rgb * 0.5; // 光线使用中心色，但稍暗
                
                // 应用整体强度
                color.rgb *= _Intensity;
                
                // 软边缘
                float edgeFade = 1.0 - smoothstep(0.7 - _Softness, 0.7, dist);
                color.a *= edgeFade * radialMask;
                
                // 确保alpha正确
                color.a = saturate(color.a);
                
                return color;
            }
            ENDCG
        }
    }
    
    FallBack "Unlit/Transparent"
}

















