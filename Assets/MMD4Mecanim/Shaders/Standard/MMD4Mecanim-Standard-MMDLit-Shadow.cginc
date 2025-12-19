#ifndef UNITY_STANDARD_SHADOW_INCLUDED
#define UNITY_STANDARD_SHADOW_INCLUDED

#include "MMD4Mecanim-Standard-MMDLit-Config.cginc"

// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"

#ifdef TESSELLATION_ON
#include "HLSLSupport.cginc" // UNITY_CAN_COMPILE_TESSELLATION
#include "Lighting.cginc" // UnityTessellationFactors
#include "Tessellation.cginc"
#endif

// Do dithering for alpha blended shadows on SM3+/desktop;
// on lesser systems do simple alpha-tested shadows
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
	#if !((SHADER_TARGET < 30) || defined (SHADER_API_MOBILE) || defined(SHADER_API_D3D11_9X) || defined (SHADER_API_PSP2) || defined (SHADER_API_PSM))
	#define UNITY_STANDARD_USE_DITHER_MASK 1
	#endif
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif


half4		_Color;
half		_Cutoff;
sampler2D	_MainTex;
float4		_MainTex_ST;
#ifdef UNITY_STANDARD_USE_DITHER_MASK
sampler3D	_DitherMaskLOD;
#endif
		
struct VertexInput
{
	float4 vertex	: POSITION;
	float3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
};

#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
	V2F_SHADOW_CASTER_NOPOS
	#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		float2 tex : TEXCOORD1;
	#endif
};
#endif


// We have to do these dances of outputting SV_POSITION separately from the vertex shader,
// and inputting VPOS in the pixel shader, since they both map to "POSITION" semantic on
// some platforms, and then things don't go well.


void vertShadowCaster (VertexInput v,
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	out VertexOutputShadowCaster o,
	#endif
	out float4 opos : SV_POSITION)
{
	TRANSFER_SHADOW_CASTER_NOPOS(o,opos)
	#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		o.tex = TRANSFORM_TEX(v.uv0, _MainTex);
	#endif
}

half4 fragShadowCaster (
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	VertexOutputShadowCaster i
	#endif
	#ifdef UNITY_STANDARD_USE_DITHER_MASK
	, UNITY_VPOS_TYPE vpos : VPOS
	#endif
	) : SV_Target
{
	#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		#ifdef _ALPHABLEND_ON
			half alpha = tex2D(_MainTex, i.tex).a * _Color.a;
			MMDLIT_CLIP (alpha);
		#else
			MMDLIT_CLIP_FAST(1.0);
		#endif
	#endif // #if defined(UNITY_STANDARD_USE_SHADOW_UVS)

/*
	#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
		half alpha = tex2D(_MainTex, i.tex).a * _Color.a;
		#if defined(_ALPHATEST_ON)
			clip (alpha - _Cutoff);
		#endif
		#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
			#if defined(UNITY_STANDARD_USE_DITHER_MASK)
				// Use dither mask for alpha blended shadows, based on pixel position xy
				// and alpha level. Our dither texture is 4x4x16.
				half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
				clip (alphaRef - 0.01);
			#else
				clip (alpha - _Cutoff);
			#endif
		#endif
	#endif // #if defined(UNITY_STANDARD_USE_SHADOW_UVS)
*/
	SHADOW_CASTER_FRAGMENT(i)
}			

#ifdef TESSELLATION_ON
#ifdef UNITY_CAN_COMPILE_TESSELLATION

float _TessPhongStrength;
float _TessEdgeLength;
float _TessExtrusionAmount;

// tessellation vertex shader
struct InternalTessInterp_VertexInput
{
	float4 vertex	: INTERNALTESSPOS;
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
};

InternalTessInterp_VertexInput tess_vertVertexInput(VertexInput v)
{
	InternalTessInterp_VertexInput o;
	o.vertex = v.vertex;
	o.normal = v.normal;
	o.uv0 = v.uv0;
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
	return v;
}

// tessellation domain shader
[UNITY_domain("tri")]
void dsShadowCaster(UnityTessellationFactors tessFactors, const OutputPatch<InternalTessInterp_VertexInput, 3> vi, float3 bary : SV_DomainLocation,
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	out VertexOutputShadowCaster o,
#endif
	out float4 opos : SV_POSITION)
{
	VertexInput v = _dsInternal(tessFactors, vi, bary);
	vertShadowCaster(v,
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		o,
#endif
		opos);
}

#endif // UNITY_CAN_COMPILE_TESSELLATION
#endif // TESSELLATION_ON

#endif // UNITY_STANDARD_SHADOW_INCLUDED
