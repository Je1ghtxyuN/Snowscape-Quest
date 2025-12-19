Shader "Custom/VRCondensation"
{
    Properties
    {
        _MainTex ("Droplet Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _TintColor ("Tint Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _AlphaMultiplier ("Alpha Multiplier", Range(0.1, 2.0)) = 0.5 // 新增：控制透明度倍率
    }
    
    SubShader
    {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        LOD 100

        // 关键设置：关闭深度写入，始终渲染
        ZWrite Off
        ZTest Always
        // 混合模式：标准透明混合
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 开启 GPU Instancing 支持 (VR Single Pass 必须)
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // 1. 添加 Instancing ID
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                // 2. 添加 Stereo 输出
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;
            float4 _TintColor;
            float _AlphaMultiplier;

            // 简单噪声
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // 3. 初始化 Instancing 和 Stereo (修复右眼不显示的核心)
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 4. 片元阶段也要初始化 Stereo (部分平台需要)
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                if (_Intensity < 0.001) {
                    return fixed4(0,0,0,0);
                }

                // 计算动态水滴 UV
                float2 dropletUV = i.uv * 5.0 + _Time.x * 0.05; 
                float dropletNoise = noise(dropletUV);
                
                // 采样贴图
                fixed4 texCol = tex2D(_MainTex, i.uv);
                
                // 混合遮罩：结合贴图红色通道和计算噪声
                float mask = texCol.r * 0.6 + dropletNoise * 0.4;

                // 边缘虚化 (Vignette) - 让中心更清晰，看路更清楚
                float2 centerUV = i.uv - 0.5;
                float dist = length(centerUV);
                // 只有边缘 (距离中心 > 0.3) 才开始变白
                float vignette = smoothstep(0.2, 0.8, dist); 
                
                // 计算最终透明度
                // 基础值非常低 + 边缘增强 + 纹理细节
                float alpha = 0.0;
                
                // 只有边缘和有水滴的地方才显示，中间尽量透明
                alpha += vignette * _Intensity * 0.6; 
                alpha += mask * _Intensity * 0.3;

                // 全局透明度控制 (解决太模糊/太白的问题)
                alpha *= _AlphaMultiplier;

                return fixed4(_TintColor.rgb, saturate(alpha));
            }
            ENDCG
        }
    }
}