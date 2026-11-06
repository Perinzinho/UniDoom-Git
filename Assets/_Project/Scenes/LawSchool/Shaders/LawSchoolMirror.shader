Shader "UniDoom/LawSchool/Mirror"
{
    Properties{_ReflectionTex("Environment reflection",2D)="grey"{} _Tint("Aged silver",Color)=(0.84,0.86,0.84,1)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "Mirror"
            Tags{"LightMode"="UniversalForward"}
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_ReflectionTex);SAMPLER(sampler_ReflectionTex);
            CBUFFER_START(UnityPerMaterial)
                float4x4 _ReflectionVP;
                half4 _Tint;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float4 reflected:TEXCOORD0;float2 uv:TEXCOORD1;};
            Varyings Vert(Attributes input){Varyings o;float3 world=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(world);o.reflected=mul(_ReflectionVP,float4(world,1));o.uv=input.uv;return o;}
            half4 Frag(Varyings input):SV_Target{float2 uv=input.reflected.xy/max(input.reflected.w,.0001)*.5+.5;
            #if UNITY_UV_STARTS_AT_TOP
                uv.y=1-uv.y;
            #endif
                half3 reflected=SAMPLE_TEXTURE2D(_ReflectionTex,sampler_ReflectionTex,uv).rgb;float edge=min(min(input.uv.x,1-input.uv.x),min(input.uv.y,1-input.uv.y));half patina=1-smoothstep(0,.025,edge);return half4(lerp(reflected*_Tint.rgb,half3(.13,.15,.14),patina*.45),1);}
            ENDHLSL
        }
    }
}
