Shader "UniDoom/Campus/LocalizedWeathering"
{
    Properties
    {
        _BaseMap("Weather mask (dark areas are dirt)",2D)="white"{}
        _BaseColor("Deposited dirt colour",Color)=(.12,.14,.10,1)
        _Opacity("Deposit strength",Range(0,1))=.55
        _Smoothness("Dampness",Range(0,1))=.15
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Pass
        {
            Name "Weathering"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Opacity;
                half _Smoothness;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=p.positionCS;output.positionWS=p.positionWS;
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=input.uv;output.fog=ComputeFogFactor(p.positionCS.z);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 mask=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv*_BaseMap_ST.xy+_BaseMap_ST.zw).rgb;
                half edge=smoothstep(0,.08,input.uv.x)*smoothstep(0,.08,1-input.uv.x)*smoothstep(0,.12,input.uv.y)*smoothstep(0,.18,1-input.uv.y);
                half alpha=saturate((1-dot(mask,half3(.2126,.7152,.0722)))*2.2h)*_Opacity*edge;
                clip(alpha-.015h);
                InputData data=(InputData)0;
                data.positionWS=input.positionWS;data.normalWS=NormalizeNormalPerPixel(input.normalWS);data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);data.bakedGI=SampleSH(data.normalWS);data.fogCoord=input.fog;
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=_BaseColor.rgb;surface.alpha=alpha;surface.smoothness=_Smoothness;surface.occlusion=1;surface.normalTS=half3(0,0,1);
                half4 color=UniversalFragmentPBR(data,surface);color.rgb=MixFog(color.rgb,input.fog);color.a=alpha;return color;
            }
            ENDHLSL
        }
    }
}
