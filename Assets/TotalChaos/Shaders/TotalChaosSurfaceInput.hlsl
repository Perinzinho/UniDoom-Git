#ifndef TOTAL_CHAOS_SURFACE_INPUT_INCLUDED
#define TOTAL_CHAOS_SURFACE_INPUT_INCLUDED

#define InitializeStandardLitSurfaceData InitializeTotalChaosNativeLitSurface
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#undef InitializeStandardLitSurfaceData

TEXTURE2D(_DirtMap);
SAMPLER(sampler_DirtMap);

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData surface)
{
    InitializeTotalChaosNativeLitSurface(uv, surface);
    // Reuse the unused metallic-workflow specular vector to keep every native
    // pass's UnityPerMaterial layout identical and preserve SRP batching.
    half4 controls = _SpecColor; // cavity dirt, cavity wetness, saturation, contrast
    half cavity = 1.0h - SAMPLE_TEXTURE2D(_DirtMap, sampler_DirtMap, uv).g;
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
