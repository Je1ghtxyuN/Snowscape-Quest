Shader "MMD4Mecanim/Standard/MMDLit-Tess-NoShadowCasting-BothFaces-Transparent"
{
	Properties
	{
		//_Color("Diffuse", Color) = (1,1,1,1)
		_Specular("Specular", Color) = (1,1,1) // Memo: Postfix from material.(Revision>=0)
		_Ambient("Ambient", Color) = (1,1,1)
		_Shininess("Shininess", Float) = 0
		_ShadowLum("ShadowLum", Range(0,10)) = 1.5
		_AmbientToDiffuse("AmbientToDiffuse", Float) = 5
		_EdgeColor("EdgeColor", Color) = (0,0,0,1)
		_EdgeScale("EdgeScale", Range(0,2)) = 0 // Memo: Postfix from material.(Revision>=0)
		_EdgeSize("EdgeSize", float) = 0 // Memo: Postfix from material.(Revision>=0)
		//_MainTex("MainTex", 2D) = "white" {}
		_ToonTex("ToonTex", 2D) = "white" {}

		_SphereCube("SphereCube", Cube) = "white" {} // Memo: Postfix from material.(Revision>=0)

		_Emissive("Emissive", Color) = (0,0,0,0)
		_ALPower("ALPower", Float) = 0

		_AddLightToonCen("AddLightToonCen", Float) = -0.1
		_AddLightToonMin("AddLightToonMin", Float) = 0.5

		_ToonTone("ToonTone", Vector) = (1.0, 0.5, 0.5, 0.0) // ToonTone, ToonTone / 2, ToonToneAdd, Unused

		_NoShadowCasting("__NoShadowCasting", Float) = 1.0

		_TessEdgeLength("Tess Edge length", Range(2,50)) = 5
		_TessPhongStrength("Tess Phong Strengh", Range(0,1)) = 0.5
		_TessExtrusionAmount("TessExtrusionAmount", Float) = 0.0

		_Revision("Revision",Float) = -1.0 // Memo: Shader setting trigger.(Reset to 0<=)



		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0

		_OffsetFactor("__offset_factor", Float) = 0.0
		_OffsetUnits("__offset_units", Float) = 0.0

		[HideInInspector] _Cull("__cull", Float) = 0.0

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	SubShader
	{
		Tags { "Queue" = "Geometry+2" "RenderType"="Opaque" "PerformanceChecks"="False" "ForceNoShadowCasting" = "True" }
		LOD 300
	

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull Front

			CGPROGRAM
			#pragma target 5.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _BRDF_SPECULAR
			#pragma shader_feature _ADD_SPECULAR
			#pragma shader_feature _TOON
			#pragma shader_feature AMB2DIFF_ON
			#pragma shader_feature SPECULAR_ON
			#pragma shader_feature SELFSHADOW_ON
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex tess_vertVertexInput
			#pragma fragment fragBase
			#pragma hull hsVertexInput
			#pragma domain dsBase
			#define TESSELLATION_ON
			#include "MMD4Mecanim-Standard-MMDLit-CoreForward.cginc"

			ENDCG
		}

		Pass
		{
			Name "FORWARD2" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull Back
			Offset [_OffsetFactor], [_OffsetUnits]

			CGPROGRAM
			#pragma target 5.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _BRDF_SPECULAR
			#pragma shader_feature _ADD_SPECULAR
			#pragma shader_feature _TOON
			#pragma shader_feature AMB2DIFF_ON
			#pragma shader_feature SPECULAR_ON
			#pragma shader_feature SELFSHADOW_ON
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertBase
			#pragma fragment fragBase
			#define TESSELLATION_ON
			#include "MMD4Mecanim-Standard-MMDLit-CoreForward.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			Cull Front

			CGPROGRAM
			#pragma target 5.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _BRDF_SPECULAR
			#pragma shader_feature _ADD_SPECULAR
			#pragma shader_feature _TOON
			#pragma shader_feature SPECULAR_ON
			#pragma shader_feature SELFSHADOW_ON
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			#pragma vertex tess_vertVertexInput
			#pragma fragment fragAdd
			#pragma hull hsVertexInput
			#pragma domain dsAdd
			#define UNITY_PASS_FORWARDADD
			#define TESSELLATION_ON
			#include "MMD4Mecanim-Standard-MMDLit-CoreForward.cginc"

			ENDCG
		}

		Pass
		{
			Name "FORWARD_DELTA2"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			Cull Back
			Offset [_OffsetFactor], [_OffsetUnits]

			CGPROGRAM
			#pragma target 5.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _BRDF_SPECULAR
			#pragma shader_feature _ADD_SPECULAR
			#pragma shader_feature _TOON
			#pragma shader_feature SPECULAR_ON
			#pragma shader_feature SELFSHADOW_ON
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			#pragma vertex tess_vertVertexInput
			#pragma fragment fragAdd
			#pragma hull hsVertexInput
			#pragma domain dsAdd
			#define UNITY_PASS_FORWARDADD
			#define TESSELLATION_ON
			#include "MMD4Mecanim-Standard-MMDLit-CoreForward.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "SHADOW_CASTER"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual
			Cull Off

			CGPROGRAM
			#pragma target 5.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------


			//#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster
			#pragma hull hsVertexInput
			#pragma domain dsShadowCaster
			#define TESSELLATION_ON
			#include "MMD4Mecanim-Standard-MMDLit-Shadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta
			#pragma hull hsVertexInput
			#pragma domain ds_meta
			#pragma target 5.0

			#pragma shader_feature _BRDF_SPECULAR
			#pragma shader_feature _ADD_SPECULAR
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#define TESSELLATION_ON
			#include "MMD4Mecanim-Standard-MMDLit-Meta.cginc"
			ENDCG
		}
	}

	FallBack "MMD4Mecanim/Standard/MMDLit-NoShadowCasting-BothFaces-Transparent"
	CustomEditor "MMD4MecanimStandardMaterialInspector"
}
