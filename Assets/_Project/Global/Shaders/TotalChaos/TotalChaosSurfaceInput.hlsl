#ifndef TOTAL_CHAOS_SURFACE_INPUT_INCLUDED
#define TOTAL_CHAOS_SURFACE_INPUT_INCLUDED

#define InitializeStandardLitSurfaceData InitializeTotalChaosNativeLitSurface
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#undef InitializeStandardLitSurfaceData

TEXTURE2D(_DirtMap);
SAMPLER(sampler_DirtMap);

// URP 17.5 LitInput equivalent for materials whose metallic, occlusion and dirt
// slots reference the SAME texture. Keep native precision, UVs and feature math.
// Derived from URP LitInput.hlsl; Unity Companion License, see LICENSE-Unity.md.
#if defined(_TC_SHARED_MASK) && defined(_METALLICSPECGLOSSMAP) && !defined(_SPECULAR_SETUP)
inline void InitializeTotalChaosSharedMaskSurface(float2 uv, float4 mask, out SurfaceData surface)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    surface.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    surface.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    surface.albedo = AlphaModulate(surface.albedo, surface.alpha);
    half4 specGloss = half4(mask);
    surface.metallic = specGloss.r;
    surface.specular = half3(0.0h, 0.0h, 0.0h);
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    surface.smoothness = albedoAlpha.a * _Smoothness;
#else
    surface.smoothness = specGloss.a * _Smoothness;
#endif
    surface.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
#if defined(_OCCLUSIONMAP)
    half occlusion = mask.g;
    surface.occlusion = LerpWhiteTo(occlusion, _OcclusionStrength);
#else
    surface.occlusion = half(1.0);
#endif
    surface.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    surface.clearCoatMask = clearCoat.r;
    surface.clearCoatSmoothness = clearCoat.g;
#else
    surface.clearCoatMask = half(0.0);
    surface.clearCoatSmoothness = half(0.0);
#endif
#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    surface.albedo = ApplyDetailAlbedo(detailUv, surface.albedo, detailMask);
    surface.normalTS = ApplyDetailNormal(detailUv, surface.normalTS, detailMask);
#endif
}
#endif

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData surface)
{
#if defined(_TC_SHARED_MASK) && defined(_METALLICSPECGLOSSMAP) && !defined(_SPECULAR_SETUP)
    float4 mask = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    InitializeTotalChaosSharedMaskSurface(uv, mask, surface);
    half cavity = 1.0h - mask.g;
#else
    InitializeTotalChaosNativeLitSurface(uv, surface);
    half cavity = 1.0h - SAMPLE_TEXTURE2D(_DirtMap, sampler_DirtMap, uv).g;
#endif
    // Reuse the unused metallic-workflow specular vector to keep every native
    // pass's UnityPerMaterial layout identical and preserve SRP batching.
    half4 controls = _SpecColor; // cavity dirt, cavity wetness, saturation, contrast
    half dirt = saturate(cavity * controls.r);
    half wet = saturate(cavity * controls.g);
    surface.albedo *= lerp(1.0h, 0.68h, dirt);
    surface.albedo *= lerp(1.0h, 0.78h, wet);
    half luminance = dot(surface.albedo, half3(0.2126h, 0.7152h, 0.0722h));
    surface.albedo = lerp(luminance.xxx, surface.albedo, controls.b);
    surface.albedo = saturate((surface.albedo - 0.18h) * controls.a + 0.18h);
    surface.smoothness = lerp(surface.smoothness, max(surface.smoothness, 0.72h), wet);
}
#endif
