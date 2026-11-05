Shader "UniDoom/Characters/Zombie Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Original sprite sheet", 2D) = "white" {}
        [NoScaleOffset] _SilhouetteTex("Imported silhouette mask", 2D) = "white" {}
        [Toggle] _UseSilhouetteMask("Use imported silhouette mask", Float) = 1
        [Toggle] _UseGreenScreen("Remove green background", Float) = 0
        [Toggle] _UseSourceAlpha("Clip original sprite alpha", Float) = 0
        _BlackThreshold("Black background threshold", Range(0, 0.1)) = 0.012
        _Color("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite On
        ZTest LEqual
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_SilhouetteTex); SAMPLER(sampler_SilhouetteTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _UseSilhouetteMask;
                half _UseGreenScreen;
                half _UseSourceAlpha;
                half _BlackThreshold;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float2 uv:TEXCOORD0;
                float3 normalOS:NORMAL;
                half4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float2 uv:TEXCOORD0;
                half4 color:COLOR;
                half fog:TEXCOORD1;
                half3 normalWS:TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SetUpSpriteInstanceProperties();
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                v.positionOS.xyz = UnityFlipSprite(v.positionOS.xyz, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color * _Color * unity_SpriteColor;
                o.fog = ComputeFogFactor(o.positionCS.z);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 SampleClippedSprite(Varyings i)
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(lerp(1.0h, color.a - .5h, step(.5h, _UseSourceAlpha)));
                // A separate import mask preserves dark details inside the original JPEG.
                half importedMask = SAMPLE_TEXTURE2D(_SilhouetteTex, sampler_SilhouetteTex, i.uv).r;
                half blackKeyMask = max(color.r, max(color.g, color.b)) - _BlackThreshold;
                half mask = lerp(blackKeyMask, importedMask - .5, step(.5, _UseSilhouetteMask));
                half greenKeyMask = .15686h - (color.g - max(color.r, color.b));
                clip(lerp(mask, greenKeyMask, step(.5, _UseGreenScreen)));
                color *= i.color;
                clip(color.a - .001h);
                return color;
            }
            half4 frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 color = SampleClippedSprite(i);
                color.rgb = MixFog(color.rgb, i.fog);
                return color;
            }
            half4 fragDepth(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                SampleClippedSprite(i);
                return i.positionCS.z;
            }
            half4 fragNormals(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                SampleClippedSprite(i);
                half3 normalWS = normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(normalWS);
                    return half4(PackFloat2To888(saturate(oct * .5 + .5)), 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            ENDHLSL
        }
        // Use the same silhouette in every pass, so transparent backgrounds never
        // occlude the scene and opaque characters participate in URP depth prepasses.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragNormals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
}
