#ifndef UNITY_STANDARD_META_INCLUDED
#define UNITY_STANDARD_META_INCLUDED

// define meta pass before including other files; they have conditions
// on that in some places
#define UNITY_PASS_META 1

#include "MMD4Mecanim-Standard-MMDLit-Config.cginc"
#include "MMD4Mecanim-Standard-MMDLit-BRDF.cginc"

// Functionality for Standard shader "meta" pass
// (extracts albedo/emission for lightmapper etc.)

#include "UnityCG.cginc"
#include "UnityStandardInput.cginc"
#include "UnityMetaPass.cginc"
#include "UnityStandardCore.cginc"

#ifdef TESSELLATION_ON
#include "HLSLSupport.cginc" // UNITY_CAN_COMPILE_TESSELLATION
#include "Lighting.cginc" // UnityTessellationFactors
#include "Tessellation.cginc"
#endif

struct v2f_meta
{
	float4 uv		: TEXCOORD0;
	float4 pos		: SV_POSITION;
};

v2f_meta vert_meta (VertexInput v)
{
	v2f_meta o;
	o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
	o.uv = TexCoords(v);
	return o;
}

// Albedo for lightmapping should basically be diffuse color.
// But rough metals (black diffuse) still scatter quite a lot of light around, so
// we want to take some of that into account too.
half3 UnityLightmappingAlbedo (half3 diffuse, half3 specular, half oneMinusRoughness)
{
	half roughness = 1 - oneMinusRoughness;
	half3 res = diffuse;
	res += specular * roughness * roughness * 0.5;
	return res;
}

float4 frag_meta (v2f_meta i) : SV_Target
{
	// we're interested in diffuse & specular colors,
	// and surface roughness to produce final albedo.
#ifdef _ALPHABLEND_ON
	MMDLIT_CLIP(Alpha(i.uv))
#else
	MMDLIT_CLIP_FAST(Alpha(i.uv))
#endif
	
	FragmentCommonData data = UNITY_SETUP_BRDF_INPUT (i.uv);

	UnityMetaInput o;
	UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

#ifdef _EMISSION
	o.Emission = data.diffColor.rgb * _EmissionColor.rgb;
#else
	o.Emission = half3(0.0, 0.0, 0.0);
#endif

	data.diffColor *= MMDLit_GetTempDiffuse_NoAmbient(); // Modify
	o.Albedo = UnityLightmappingAlbedo (data.diffColor, data.specColor,
#if UNITY_VERSION < 550
		data.oneMinusRoughness
#else
		data.smoothness
#endif
		);

	return UnityMetaFragment(o);
}

#ifdef TESSELLATION_ON
#ifdef UNITY_CAN_COMPILE_TESSELLATION

// tessellation vertex shader
struct InternalTessInterp_VertexInput
{
	float4 vertex	: INTERNALTESSPOS;
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
};

InternalTessInterp_VertexInput tess_vertVertexInput(VertexInput v)
{
	InternalTessInterp_VertexInput o;
	o.vertex = v.vertex;
	o.normal = v.normal;
	o.uv0 = v.uv0;
	o.uv1 = v.uv1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	o.uv2 = v.uv2;
#endif
#ifdef _TANGENT_TO_WORLD
	o.tangent = v.tangent;
#endif
	return o;
}

// tessellation hull constant shader
UnityTessellationFactors hsconst_surf(InputPatch<InternalTessInterp_VertexInput, 3> v)
{
	float4 tf = UnityEdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, _TessEdgeLength);
	UnityTessellationFactors o;
	o.edge[0] = tf.x;
	o.edge[1] = tf.y;
	o.edge[2] = tf.z;
	o.inside = tf.w;
	return o;
}

// tessellation hull shader
[UNITY_domain("tri")]
[UNITY_partitioning("fractional_odd")]
[UNITY_outputtopology("triangle_cw")]
[UNITY_patchconstantfunc("hsconst_surf")]
[UNITY_outputcontrolpoints(3)]
InternalTessInterp_VertexInput hsVertexInput(InputPatch<InternalTessInterp_VertexInput, 3> v, uint id : SV_OutputControlPointID)
{
	return v[id];
}

inline VertexInput _dsInternal(UnityTessellationFactors tessFactors, const OutputPatch<InternalTessInterp_VertexInput, 3> vi, float3 bary : SV_DomainLocation)
{
	VertexInput v;
	v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
	float3 pp[3];
	for (int i = 0; i < 3; ++i)
		pp[i] = v.vertex.xyz - vi[i].normal * (dot(v.vertex.xyz, vi[i].normal) - dot(vi[i].vertex.xyz, vi[i].normal));
	v.vertex.xyz = _TessPhongStrength * (pp[0] * bary.x + pp[1] * bary.y + pp[2] * bary.z) + (1.0f - _TessPhongStrength) * v.vertex.xyz;
	v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
	v.vertex.xyz += v.normal.xyz * _TessExtrusionAmount;
	v.uv0 = vi[0].uv0*bary.x + vi[1].uv0*bary.y + vi[2].uv0*bary.z;
	v.uv1 = vi[0].uv1*bary.x + vi[1].uv1*bary.y + vi[2].uv1*bary.z;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	v.uv2 = vi[0].uv2*bary.x + vi[1].uv2*bary.y + vi[2].uv2*bary.z;
#endif
#ifdef _TANGENT_TO_WORLD
	v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
#endif
	return v;
}

// tessellation domain shader
[UNITY_domain("tri")]
v2f_meta ds_meta(UnityTessellationFactors tessFactors, const OutputPatch<InternalTessInterp_VertexInput, 3> vi, float3 bary : SV_DomainLocation)
{
	VertexInput v = _dsInternal(tessFactors, vi, bary);
	v2f_meta o = vert_meta(v);
	return o;
}

#endif // UNITY_CAN_COMPILE_TESSELLATION
#endif // TESSELLATION_ON

#endif // UNITY_STANDARD_META_INCLUDED
