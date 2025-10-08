#ifndef MMD4MECANIM_MMDLIT_STANDARD_CONFIG_INCLUDED
#define MMD4MECANIM_MMDLIT_STANDARD_CONFIG_INCLUDED

#ifdef _BRDF_SPECULAR
	#define UNITY_SETUP_BRDF_INPUT SpecularSetup
#else
	#define UNITY_SETUP_BRDF_INPUT MetallicSetup
#endif

#if !defined (MMDLit_UNITY_BRDF_PBS) // allow to explicitly override BRDF in custom shader
	// still add safe net for low shader models, otherwise we might end up with shaders failing to compile
	// the only exception is WebGL in 5.3 - it will be built with shader target 2.0 but we want it to get rid of constraints, as it is effectively desktop
	#if SHADER_TARGET < 30 && !UNITY_53_SPECIFIC_TARGET_WEBGL
		#define MMDLit_UNITY_BRDF_PBS MMDLit_BRDF3_Unity_PBS
	#elif UNITY_PBS_USE_BRDF3
		#define MMDLit_UNITY_BRDF_PBS MMDLit_BRDF3_Unity_PBS
	#elif UNITY_PBS_USE_BRDF2
		#define MMDLit_UNITY_BRDF_PBS MMDLit_BRDF2_Unity_PBS
	#elif UNITY_PBS_USE_BRDF1
		#define MMDLit_UNITY_BRDF_PBS MMDLit_BRDF1_Unity_PBS
	#elif defined(SHADER_TARGET_SURFACE_ANALYSIS)
		// we do preprocess pass during shader analysis and we dont actually care about brdf as we need only inputs/outputs
		#define MMDLit_UNITY_BRDF_PBS MMDLit_BRDF1_Unity_PBS
	#else
		#error something broke in auto-choosing BRDF
	#endif
#endif

#endif
