Shader "UniDoom/Environment/Worn Sign Unlit"
{
    Properties
    {
        _BaseMap("Printed sign",2D)="white"{}
        _BaseColor("Faded paint tint",Color)=(.72,.70,.65,1)
        _WearStrength("Ink fading",Range(0,1))=.75
        _Seed("Wear variation",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _WearStrength;
        float _Seed;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float3 normalOS:NORMAL; };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half fog:TEXCOORD1; half3 normalWS:TEXCOORD2; };
        Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o; }
        float hash(float2 p) { float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z); }
        float noise(float2 p) { float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
        half4 frag(Varyings i):SV_Target
        {
            half3 printed=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
            half3 background=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(.02,.5)).rgb;
            half ink=saturate((max(printed.r,max(printed.g,printed.b))-max(background.r,max(background.g,background.b)))*3);
            half wear=noise(i.uv*float2(22,7)+_Seed);
            half wornPatch=smoothstep(.63,.78,noise(i.uv*float2(10,4)+_Seed*3));
            half fade=saturate(_WearStrength*(.55+.40*wear)+wornPatch*.65);
            half3 color=lerp(printed,background,ink*fade)*_BaseColor.rgb;
            return half4(MixFog(color,i.fog),1);
        }
        ENDHLSL
        Pass { Name "Forward" Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass { Name "DepthOnly" Tags { "LightMode"="DepthOnly" } ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depth
            half4 depth(Varyings i):SV_Target { return 0; }
            ENDHLSL
        }
        Pass { Name "DepthNormals" Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment normals
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 normals(Varyings i):SV_Target
            {
                float3 normal=normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(normal);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                return half4(normal,0);
                #endif
            }
            ENDHLSL
        }
    }
}
