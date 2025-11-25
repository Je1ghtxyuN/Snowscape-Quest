Shader "Custom/VRCondensation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _BlurAmount ("Blur Amount", Range(0,0.1)) = 0.02
        _TintColor ("Tint Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _Distortion ("Distortion", Range(0,0.1)) = 0.05
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;
            float _BlurAmount;
            float4 _TintColor;
            float _Distortion;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 简单的噪声函数（替代纹理）
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 基础纹理采样
                fixed4 originalCol = tex2D(_MainTex, i.uv);
                
                if (_Intensity < 0.01) {
                    return originalCol; // 强度为0时直接返回原图
                }
                
                float currentBlur = _BlurAmount * _Intensity;
                float currentDistortion = _Distortion * _Intensity;
                
                // 扭曲效果模拟水波
                float2 distortedUV = i.uv;
                distortedUV.x += sin(i.uv.y * 20 + _Time.y) * currentDistortion * 0.5;
                distortedUV.y += cos(i.uv.x * 15 + _Time.y) * currentDistortion * 0.5;
                
                // 简单模糊效果
                fixed4 blurCol = fixed4(0,0,0,0);
                int sampleCount = 4;
                float2 blurOffsets[4] = {
                    float2(currentBlur, currentBlur),
                    float2(-currentBlur, currentBlur),
                    float2(currentBlur, -currentBlur),
                    float2(-currentBlur, -currentBlur)
                };
                
                for(int j = 0; j < sampleCount; j++) {
                    blurCol += tex2D(_MainTex, distortedUV + blurOffsets[j]);
                }
                blurCol /= sampleCount;
                
                // 使用噪声函数模拟水滴图案（避免外部纹理依赖）
                float2 dropletUV = i.uv * 8.0 + _Time.x;
                float dropletNoise = noise(dropletUV);
                float dropletMask = dropletNoise * _Intensity;
                
                // 边缘冷凝效果
                float2 centerUV = i.uv - 0.5;
                float edgeFactor = 1.0 - length(centerUV) * 1.8;
                edgeFactor = saturate(edgeFactor);
                float edgeIntensity = _Intensity * edgeFactor;
                
                // 最终颜色混合
                fixed4 finalCol = lerp(originalCol, blurCol, _Intensity * 0.7);
                finalCol.rgb = lerp(finalCol.rgb, _TintColor.rgb, dropletMask * 0.2);
                finalCol.rgb *= (1.0 - edgeIntensity * 0.1);
                
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}