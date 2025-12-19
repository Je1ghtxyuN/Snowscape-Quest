#ifndef MMDLIT_UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
#define MMDLIT_UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED

#include "MMD4Mecanim-Standard-MMDLit-Core.cginc"
#include "UnityStandardCoreForwardSimple.cginc"

VertexOutputBaseSimple MMDLit_vertForwardBaseSimple (VertexInput v)
{
	return vertForwardBaseSimple( v );
}

half4 MMDLit_fragForwardBaseSimpleInternal (VertexOutputBaseSimple i)
{
	FragmentCommonData s = FragmentSetupSimple(i);
#ifdef _ALPHABLEND_ON
	MMDLIT_CLIP(s.alpha)
#else
	MMDLIT_CLIP_FAST(s.alpha)
#endif

	UnityLight mainLight = MainLightSimple(i, s);
	
	half shadowAtten = _UNITY_SHADOW_ATTENUATION(i, s.posWorld);

#ifdef _TOON
	half giAtten = 1.0; // Skip shadowAtten on toon rendering.
#else
	half giAtten = shadowAtten;
#endif

	half3 ambientRate = MMDLit_GetAmbientRate();

	half occlusion = Occlusion(i.tex.xy);
	half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), LightDirForSpecular(i, mainLight));
	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, giAtten, mainLight);

	half nl;
	MMDLit_BRDF_NdotL(normal, mainLight, nl);

	half3 c = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, PerVertexGrazingTerm(i, s), PerVertexFresnelTerm(i)) * ambientRate;
#ifdef _TOON
	half toonNdotL = dot(s.normalWorld, mainLight.dir);
	half3 ramp = MMDLit_GetRamp(toonNdotL, shadowAtten);
	half3 diffDirect = s.diffColor * MMDLit_GetTempDiffuse(gi.indirect.diffuse);
#if SPECULAR_HIGHLIGHTS
#if UNITY_VERSION < 550
	half3 specDirect = MMDLit_BRDF3_Specular(s.specColor, Pow4(rl), 1.0 - s.oneMinusRoughness);
#else
	half3 specDirect = MMDLit_BRDF3_Specular(s.specColor, Pow4(rl), SmoothnessToPerceptualRoughness(s.smoothness));
#endif
	c += (diffDirect * ramp + specDirect * nl) * gi.light.color;
#else // SPECULAR_HIGHLIGHTS
	c += (diffDirect * ramp) * gi.light.color;
#endif // SPECULAR_HIGHLIGHTS
#else // _TOON
	half3 attenuatedLightColor = gi.light.color * nl;
	c += BRDF3DirectSimple(s.diffColor, s.specColor,
#if UNITY_VERSION < 550
		s.oneMinusRoughness,
#else
		s.smoothness,
#endif
		rl) * attenuatedLightColor;
#endif // _TOON
	c += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity,
#if UNITY_VERSION < 550
		s.oneMinusRoughness,
#else
		s.smoothness,
#endif
		s.normalWorld, -s.eyeVec, occlusion, gi);
	c += Emission(i.tex.xy);
#ifdef SPECULAR_ON // Legacy specular
	c += (half3)_Specular * (half3)gi.light.color * MMDLit_SpecularRefl(s.normalWorld, mainLight.dir, -s.eyeVec, _Shininess);
#endif // SPECULAR_ON

	UNITY_APPLY_FOG(i.fogCoord, c);
	
	return OutputForward (half4(c, 1), s.alpha);
}

half4 MMDLit_fragForwardBaseSimple (VertexOutputBaseSimple i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return MMDLit_fragForwardBaseSimpleInternal(i);
}

VertexOutputForwardAddSimple MMDLit_vertForwardAddSimple (VertexInput v)
{
	return vertForwardAddSimple( v );
}

half4 MMDLit_fragForwardAddSimpleInternal (VertexOutputForwardAddSimple i)
{
	FragmentCommonData s = FragmentSetupSimpleAdd(i);
#ifdef _ALPHABLEND_ON
	MMDLIT_CLIP(s.alpha)
#else
	MMDLIT_CLIP_FAST(s.alpha)
#endif

	half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), i.lightDir);
	half atten = LIGHT_ATTENUATION(i);
	half3 normal = LightSpaceNormal(i, s);
	half nl = LambertTerm(normal, i.lightDir);

#ifdef _TOON
	half toonNdotL = dot(s.normalWorld, i.lightDir);
	half toonRefl = MMDLit_GetToolRefl(toonNdotL);
	half toonShadow = MMDLit_GetToonShadow(toonRefl);
	half3 ramp = MMDLit_GetRamp_Add(toonRefl, toonShadow);
	half3 diffDirect = s.diffColor * MMDLit_GetTempDiffuse_NoAmbient();
#if SPECULAR_HIGHLIGHTS
#if UNITY_VERSION < 550
	half3 specDirect = MMDLit_BRDF3_Specular(s.specColor, Pow4(rl), 1.0 - s.oneMinusRoughness);
#else
	half3 specDirect = MMDLit_BRDF3_Specular(s.specColor, Pow4(rl), SmoothnessToPerceptualRoughness(s.smoothness));
#endif
	half3 c = (diffDirect * ramp + specDirect * nl);
#else // SPECULAR_HIGHLIGHTS
	half3 c = (diffDirect * ramp);
#endif // SPECULAR_HIGHLIGHTS
	c *= _LightColor0.rgb * atten * MMDLit_GetForwardAddStr(toonRefl);
#else // _TOON
	half3 c = BRDF3DirectSimple(s.diffColor, s.specColor,
#if UNITY_VERSION < 550
		s.oneMinusRoughness,
#else
		s.smoothness,
#endif
		rl);
	c *= _LightColor0.rgb * atten * nl;
#endif // _TOON
	
	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	return OutputForward (half4(c, 1), s.alpha);
}

half4 MMDLit_fragForwardAddSimple (VertexOutputForwardAddSimple i) : SV_Target	// backward compatibility (this used to be the fragment entry function)
{
	return MMDLit_fragForwardAddSimpleInternal(i);
}

#endif // MMDLIT_UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
