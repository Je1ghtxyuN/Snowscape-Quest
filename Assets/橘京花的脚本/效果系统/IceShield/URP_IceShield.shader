Shader "Custom/URP_IceShield"
{
    Properties
    {
        [Header(Visuals)]
        _BaseColor ("Ice Color", Color) = (0.0, 0.5, 1.0, 0.3)
        _RimColor ("Rim Color (Edge Glow)", Color) = (0.5, 0.8, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 10.0)) = 3.0
        
        [Header(Shape)]
        _ExpandAmount ("Expansion Size (Meters)", Range(0.0, 0.1)) = 0.005
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // ⭐ 核心修复 1: 开启实例化支持
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                // ⭐ 核心修复 2: 输入结构体添加实例ID
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                // ⭐ 核心修复 3: 输出结构体添加立体渲染坐标
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _ExpandAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // ⭐ 核心修复 4: 初始化实例和立体渲染
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 顶点外推逻辑
                float3 expandedPos = input.positionOS.xyz + (input.normalOS * _ExpandAmount);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(expandedPos);
                output.positionCS = vertexInput.positionCS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ⭐ 核心修复 5: 片元着色器也要声明立体渲染
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                float NdotV = saturate(dot(normal, viewDir));
                float rim = pow(1.0 - NdotV, _RimPower);

                half4 finalColor = _BaseColor + (_RimColor * rim);
                finalColor.a = _BaseColor.a + (rim * _RimColor.a);
                
                return saturate(finalColor);
            }
            ENDHLSL
        }
    }
}