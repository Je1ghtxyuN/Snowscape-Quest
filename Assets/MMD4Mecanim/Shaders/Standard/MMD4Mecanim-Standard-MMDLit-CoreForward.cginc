#ifndef UNITY_STANDARD_CORE_FORWARD_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_INCLUDED

#include "MMD4Mecanim-Standard-MMDLit-Config.cginc"
#include "MMD4Mecanim-Standard-MMDLit-BRDF.cginc"

#if defined(UNITY_NO_FULL_STANDARD_SHADER)
#	define UNITY_STANDARD_SIMPLE 1
#endif

#if SHADER_TARGET < 30
#undef UNITY_STANDARD_SIMPLE
#define UNITY_STANDARD_SIMPLE 1
#endif

#include "UnityStandardConfig.cginc"

#if UNITY_STANDARD_SIMPLE // Simple supported 5.3 or later.
	#include "MMD4Mecanim-Standard-MMDLit-CoreForwardSimple.cginc"
	VertexOutputBaseSimple vertBase (VertexInput v) { return MMDLit_vertForwardBaseSimple(v); }
	VertexOutputForwardAddSimple vertAdd (VertexInput v) { return MMDLit_vertForwardAddSimple(v); }
	half4 fragBase (VertexOutputBaseSimple i) : SV_Target { return MMDLit_fragForwardBaseSimpleInternal(i); }
	half4 fragAdd (VertexOutputForwardAddSimple i) : SV_Target { return MMDLit_fragForwardAddSimpleInternal(i); }
#else
	#include "MMD4Mecanim-Standard-MMDLit-Core.cginc"
	VertexOutputForwardBase vertBase (VertexInput v) { return MMDLit_vertForwardBase(v); }
	VertexOutputForwardAdd vertAdd (VertexInput v) { return MMDLit_vertForwardAdd(v); }
	half4 fragBase (VertexOutputForwardBase i) : SV_Target { return MMDLit_fragForwardBaseInternal(i); }
	half4 fragAdd (VertexOutputForwardAdd i) : SV_Target { return MMDLit_fragForwardAddInternal(i); }
#endif

#endif // UNITY_STANDARD_CORE_FORWARD_INCLUDED
