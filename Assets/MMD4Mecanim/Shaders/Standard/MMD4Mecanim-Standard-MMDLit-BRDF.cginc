#ifndef MMD4MECANIM_MMDLIT_STANDARD_BRDF_INCLUDED
#define MMD4MECANIM_MMDLIT_STANDARD_BRDF_INCLUDED

#include "MMD4Mecanim-Standard-MMDLit-Input.cginc" // Override UnityStandardInput.cginc

#include "UnityStandardBRDF.cginc"

#define MMD4MECANIM_STANDARD
#include "AutoLight.cginc" // Not MMD4Mecanim-MMDLit-AutoLight.cginc
#include "../MMD4Mecanim-MMDLit-Lighting.cginc"
#include "../MMD4Mecanim-MMDLit-Surface-Lighting.cginc"

inline void _MMDLit_BRDF_NdotL(half3 normal, UnityLight light, out half ndotl, out half ndotl_uc)
{
	ndotl_uc = dot(normal, light.dir);
	ndotl = saturate(ndotl_uc);
}

inline void MMDLit_BRDF_NdotL(half3 normal, UnityLight light, out half ndotl, out half ndotl_uc)
{
	ndotl_uc = dot(normal, light.dir);
#if UNITY_VERSION >= 550
	ndotl = saturate(ndotl_uc);
#else
	ndotl = light.ndotl;
#endif
}

inline void MMDLit_BRDF_NdotL(half3 normal, UnityLight light, out half ndotl)
{
#if UNITY_VERSION >= 550
	half ndotl_uc = dot(normal, light.dir);
	ndotl = saturate(ndotl_uc);
#else
	ndotl = light.ndotl;
#endif
}

// Main Physically Based BRDF
// Derived from Disney work and based on Torrance-Sparrow micro-facet model
//
//   BRDF = kD / pi + kS * (D * V * F) / 4
//   I = BRDF * NdotL
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) Normalized BlinnPhong
//  b) GGX
// * Smith for Visiblity term
// * Schlick approximation for Fresnel
half4 MMDLit_BRDF1_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity,
#if UNITY_VERSION < 550
	half oneMinusRoughness,
#else
	half smoothness,
#endif
	half3 normal, half3 viewDir,
	UnityLight light, UnityIndirect gi, half shadowAtten)
{
#if UNITY_VERSION < 550
	half roughness = 1.0 - oneMinusRoughness;
	half specularPower = RoughnessToSpecPower(roughness);
#else
	half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	half specularPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
#endif
	half3 halfDir = Unity_SafeNormalize(light.dir + viewDir);

	// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
	// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
	// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
	// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
	// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
	// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0 

	half nl, nl_uc;
#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
	// The amount we shift the normal toward the view vector is defined by the dot product.
	// This correction is only applied with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
	half shiftAmount = dot(normal, viewDir);
	normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
	// A re-normalization should be apply here but as the shift is small we don't do it to save ALU.
	//normal = normalize(normal);

	// As we have modify the normal we need to recalculate the dot product nl. 
	// Note that  light.ndotl is a clamped cosine and only the ForwardSimple mode use a specific ndotL with BRDF3
	_MMDLit_BRDF_NdotL(normal, light, nl, nl_uc);
#else
	MMDLit_BRDF_NdotL(normal, light, nl, nl_uc);
#endif

	half nh = BlinnTerm(normal, halfDir);
	half nv = DotClamped(normal, viewDir);

	half lv = DotClamped(light.dir, viewDir);
	half lh = DotClamped(light.dir, halfDir);

#if UNITY_BRDF_GGX
	half V = SmithJointGGXVisibilityTerm(nl, nv, roughness);
	half D = GGXTerm(nh, roughness);
#else
	half V = SmithBeckmannVisibilityTerm(nl, nv, roughness);
	half D = NDFBlinnPhongNormalizedTerm(nh, specularPower);
#endif

	half nlPow5 = Pow5(1 - nl);
	half nvPow5 = Pow5(1 - nv);
#if UNITY_VERSION < 550
	half Fd90 = 0.5 + 2 * lh * lh * roughness;
#else
	half Fd90 = 0.5 + 2 * lh * lh * perceptualRoughness;
#endif
	half disneyDiffuse = (1 + (Fd90 - 1) * nlPow5) * (1 + (Fd90 - 1) * nvPow5);

	// HACK: theoretically we should divide by Pi diffuseTerm and not multiply specularTerm!
	// BUT 1) that will make shader look significantly darker than Legacy ones
	// and 2) on engine side "Non-important" lights have to be divided by Pi to in cases when they are injected into ambient SH
	// NOTE: multiplication by Pi is part of single constant together with 1/4 now
	half specularTerm = (V * D) * (UNITY_PI / 4); // Torrance-Sparrow model, Fresnel is applied later (for optimization reasons)
	if (IsGammaSpace())
		specularTerm = sqrt(max(1e-4h, specularTerm));
	specularTerm = max(0, specularTerm * nl);


#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm = 0.0;
#endif

#ifdef _TOON
	half toonNdotL = clamp(disneyDiffuse * nl_uc, -1.0, 1.0);
#ifdef UNITY_PASS_FORWARDADD
	half toonRefl = MMDLit_GetToolRefl(toonNdotL);
	half toonShadow = MMDLit_GetToonShadow(toonRefl);
	half3 diffuseTerm = MMDLit_GetRamp_Add(toonRefl, toonShadow);
#else // UNITY_PASS_FORWARDADD
	half3 diffuseTerm = MMDLit_GetRamp(toonNdotL, shadowAtten);
#endif // UNITY_PASS_FORWARDADD
#else // _TOON
	half3 diffuseTerm = disneyDiffuse * nl; // Warning: half to half3
#endif // _TOON

#ifdef UNITY_PASS_FORWARDADD
	half3 tempDiffuse = MMDLit_GetTempDiffuse_NoAmbient();
#else
	half3 tempDiffuse = MMDLit_GetTempDiffuse(gi.diffuse);
#endif

#if UNITY_VERSION < 550
	// surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(realRoughness^2+1)
	half realRoughness = roughness*roughness;		// need to square perceptual roughness
	half surfaceReduction;
	if (IsGammaSpace()) surfaceReduction = 1.0 - 0.28*realRoughness*roughness;		// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
	else surfaceReduction = 1.0 / (realRoughness*realRoughness + 1.0);			// fade \in [0.5;1]
#else
	// surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
	half surfaceReduction;
	if (IsGammaSpace()) surfaceReduction = 1.0 - 0.28*roughness*perceptualRoughness;		// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
	else surfaceReduction = 1.0 / (roughness*roughness + 1.0);			// fade \in [0.5;1]
#endif

#ifdef UNITY_PASS_FORWARDADD
#else // UNITY_PASS_FORWARDADD
	half3 ambientRate = MMDLit_GetAmbientRate();
#endif

#if UNITY_VERSION < 550
	half grazingTerm = saturate(oneMinusRoughness + (1 - oneMinusReflectivity));
#else
	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
#endif
	half3 color = diffColor * (
#ifdef UNITY_PASS_FORWARDADD
#else // UNITY_PASS_FORWARDADD
		gi.diffuse * ambientRate +
#endif
		tempDiffuse * light.color * diffuseTerm)
		+ specularTerm * light.color * FresnelTerm(specColor, lh)
#ifdef UNITY_PASS_FORWARDADD
		;
#else // UNITY_PASS_FORWARDADD
		+ surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv) * ambientRate;
#endif

#ifdef SPECULAR_ON // Legacy specular
	color += (half3)_Specular * (half3)light.color * MMDLit_SpecularRefl(normal, light.dir, viewDir, _Shininess);
#endif

#ifdef _TOON
#ifdef UNITY_PASS_FORWARDADD
	color *= MMDLit_GetForwardAddStr(toonRefl);
#endif // UNITY_PASS_FORWARDADD
#endif // _TOON

#ifdef UNITY_PASS_FORWARDADD
#else
#ifdef _EMISSION
	color.rgb += diffColor * _EmissionColor.rgb; // Added: Multiply with diffColor.
#endif
#endif

	return half4(color, 1);
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * BlinnPhong as NDF
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half4 MMDLit_BRDF2_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity,
#if UNITY_VERSION < 550
	half oneMinusRoughness,
#else
	half smoothness,
#endif
	half3 normal, half3 viewDir,
	UnityLight light, UnityIndirect gi, half shadowAtten)
{
	half3 halfDir = Unity_SafeNormalize(light.dir + viewDir);

	half nl, nl_uc;
	MMDLit_BRDF_NdotL(normal, light, nl, nl_uc);

	half nh = BlinnTerm(normal, halfDir);
	half nv = DotClamped(normal, viewDir);
	half lh = DotClamped(light.dir, halfDir);

#if UNITY_VERSION < 550
	half roughness = 1.0 - oneMinusRoughness;
	half specularPower = RoughnessToSpecPower(roughness);
#else
	half perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	half specularPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
	half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#endif
	// Modified with approximate Visibility function that takes roughness into account
	// Original ((n+1)*N.H^n) / (8*Pi * L.H^3) didn't take into account roughness 
	// and produced extremely bright specular at grazing angles

	// HACK: theoretically we should divide by Pi diffuseTerm and not multiply specularTerm!
	// BUT 1) that will make shader look significantly darker than Legacy ones
	// and 2) on engine side "Non-important" lights have to be divided by Pi to in cases when they are injected into ambient SH
	// NOTE: multiplication by Pi is cancelled with Pi in denominator

#if UNITY_VERSION < 550
	half invV = lh * lh * oneMinusRoughness + roughness * roughness; // approx ModifiedKelemenVisibilityTerm(lh, 1-oneMinusRoughness);
#else
	half invV = lh * lh * smoothness + perceptualRoughness * perceptualRoughness; // approx ModifiedKelemenVisibilityTerm(lh, perceptualRoughness);
#endif
	half invF = lh;
	half specular = ((specularPower + 1) * pow(nh, specularPower)) / (8 * invV * invF + 1e-4h);
	if (IsGammaSpace())
		specular = sqrt(max(1e-4h, specular));

#if UNITY_VERSION < 550
	// surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(realRoughness^2+1)
	half realRoughness = roughness*roughness;		// need to square perceptual roughness
													// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
													// 1-x^3*(0.6-0.08*x)   approximation for 1/(x^4+1)
	half surfaceReduction = IsGammaSpace() ? 0.28 : (0.6 - 0.08*roughness);
	surfaceReduction = 1.0 - realRoughness*roughness*surfaceReduction;
#else
	// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
	// 1-x^3*(0.6-0.08*x)   approximation for 1/(x^4+1)
	half surfaceReduction = IsGammaSpace() ? 0.28 : (0.6 - 0.08*perceptualRoughness);
	surfaceReduction = 1.0 - roughness*perceptualRoughness*surfaceReduction;
#endif

	// Prevent FP16 overflow on mobiles
#if SHADER_API_GLES || SHADER_API_GLES3
	specular = clamp(specular, 0.0, 100.0);
#endif

#if defined(_SPECULARHIGHLIGHTS_OFF)
	specular = 0.0;
#endif

	half3 specLight = light.color * nl;
#ifdef _TOON
	half toonNdotL = nl_uc;
#ifdef UNITY_PASS_FORWARDADD
	half toonRefl = MMDLit_GetToolRefl(toonNdotL);
	half toonShadow = MMDLit_GetToonShadow(toonRefl);
	half3 ramp = MMDLit_GetRamp_Add(toonRefl, toonShadow);
#else // UNITY_PASS_FORWARDADD
	half3 ramp = MMDLit_GetRamp(toonNdotL, shadowAtten);
#endif // UNITY_PASS_FORWARDADD
	half3 diffLight = light.color * ramp;
#else // _TOON
	half3 diffLight = specLight;
#endif // _TOON

#ifdef UNITY_PASS_FORWARDADD
	half3 tempDiffuse = MMDLit_GetTempDiffuse_NoAmbient();
#else
	half3 tempDiffuse = MMDLit_GetTempDiffuse(gi.diffuse);
#endif

#ifdef UNITY_PASS_FORWARDADD
#else // UNITY_PASS_FORWARDADD
	half3 ambientRate = MMDLit_GetAmbientRate();
#endif

	half3 color = diffColor * (
#ifdef UNITY_PASS_FORWARDADD
#else // UNITY_PASS_FORWARDADD
		gi.diffuse * ambientRate +
#endif
		tempDiffuse * diffLight)
		+ specular * specColor * specLight;

#ifdef UNITY_PASS_FORWARDADD
#else // UNITY_PASS_FORWARDADD
#if UNITY_VERSION < 550
	half grazingTerm = saturate(oneMinusRoughness + (1 - oneMinusReflectivity));
#else
	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
#endif
	color += surfaceReduction * gi.specular * FresnelLerpFast(specColor, grazingTerm, nv) * ambientRate;
#endif // UNITY_PASS_FORWARDADD

#ifdef SPECULAR_ON // Legacy specular
	color += (half3)_Specular * (half3)light.color * MMDLit_SpecularRefl(normal, light.dir, viewDir, _Shininess);
#endif

#ifdef _TOON
#ifdef UNITY_PASS_FORWARDADD
	color *= MMDLit_GetForwardAddStr(toonRefl);
#endif // UNITY_PASS_FORWARDADD
#endif // _TOON

#ifdef UNITY_PASS_FORWARDADD
#else
#ifdef _EMISSION
	color.rgb += diffColor * _EmissionColor.rgb; // Added: Multiply with diffColor.
#endif
#endif

	return half4(color, 1);
}

half3 MMDLit_BRDF3_Specular(half3 specColor, half rlPow4, half roughness)
{
#if defined(_SPECULARHIGHLIGHTS_OFF)
	return 0.0;
#else
	half LUT_RANGE = 16.0; // must match range in NHxRoughness() function in GeneratedTextures.cpp
						   // Lookup texture to save instructions
	half specular = tex2D(unity_NHxRoughness, half2(rlPow4, roughness)).UNITY_ATTEN_CHANNEL * LUT_RANGE;
	return specular * specColor;
#endif
}

// Old school, not microfacet based Modified Normalized Blinn-Phong BRDF
// Implementation uses Lookup texture for performance
//
// * Normalized BlinnPhong in RDF form
// * Implicit Visibility term
// * No Fresnel term
//
// TODO: specular is too weak in Linear rendering mode
half4 MMDLit_BRDF3_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity,
#if UNITY_VERSION < 550
	half oneMinusRoughness,
#else
	half smoothness,
#endif
	half3 normal, half3 viewDir,
	UnityLight light, UnityIndirect gi, half shadowAtten)
{
	half3 reflDir = reflect(viewDir, normal);

	half nl, nl_uc;
	MMDLit_BRDF_NdotL(normal, light, nl, nl_uc);

	half nv = DotClamped(normal, viewDir);

	// Vectorize Pow4 to save instructions
	half2 rlPow4AndFresnelTerm = Pow4(half2(dot(reflDir, light.dir), 1 - nv));  // use R.L instead of N.H to save couple of instructions
	half rlPow4 = rlPow4AndFresnelTerm.x; // power exponent must match kHorizontalWarpExp in NHxRoughness() function in GeneratedTextures.cpp
	half fresnelTerm = rlPow4AndFresnelTerm.y;

#ifdef UNITY_PASS_FORWARDADD
	half3 tempDiffuse = MMDLit_GetTempDiffuse_NoAmbient();
#else
	half3 tempDiffuse = MMDLit_GetTempDiffuse(gi.diffuse);
#endif

	half3 diffDirect = diffColor * tempDiffuse;
#if UNITY_VERSION < 550
	half3 specDirect = MMDLit_BRDF3_Specular(specColor, rlPow4, 1.0 - oneMinusRoughness);
#else
	half3 specDirect = MMDLit_BRDF3_Specular(specColor, rlPow4, SmoothnessToPerceptualRoughness(smoothness));
#endif

#ifdef _TOON
	half toonNdotL = nl_uc;
#ifdef UNITY_PASS_FORWARDADD
	half toonRefl = MMDLit_GetToolRefl(toonNdotL);
	half toonShadow = MMDLit_GetToonShadow(toonRefl);
	half3 ramp = MMDLit_GetRamp_Add(toonRefl, toonShadow);
#else // UNITY_PASS_FORWARDADD
	half3 ramp = MMDLit_GetRamp(toonNdotL, shadowAtten);
#endif // UNITY_PASS_FORWARDADD
	half3 color = (diffDirect * ramp + specDirect * nl) * light.color;
#else // _TOON
	half3 color = (diffDirect + specDirect) * light.color * nl;
#endif // _TOON

#ifdef UNITY_PASS_FORWARDADD
#else // UNITY_PASS_FORWARDADD
	half3 ambientRate = MMDLit_GetAmbientRate();
#if UNITY_VERSION < 550
	half grazingTerm = saturate(oneMinusRoughness + (1.0 - oneMinusReflectivity));
#else
	half grazingTerm = saturate(smoothness + (1.0 - oneMinusReflectivity));
#endif
	color += BRDF3_Indirect(diffColor, specColor, gi, grazingTerm, fresnelTerm) * ambientRate;
#endif // UNITY_PASS_FORWARDADD

#ifdef SPECULAR_ON // Legacy specular
	color += (half3)_Specular * (half3)light.color * MMDLit_SpecularRefl(normal, light.dir, viewDir, _Shininess);
#endif // SPECULAR_ON

#ifdef _TOON
#ifdef UNITY_PASS_FORWARDADD
	color *= MMDLit_GetForwardAddStr(toonRefl);
#endif // UNITY_PASS_FORWARDADD
#endif // _TOON

#ifdef UNITY_PASS_FORWARDADD
#else
#ifdef _EMISSION
	color.rgb += diffColor * _EmissionColor.rgb; // Added: Multiply with diffColor.
#endif
#endif

	return half4(color, 1);
}

#endif // MMD4MECANIM_MMDLIT_STANDARD_BRDF_INCLUDED
