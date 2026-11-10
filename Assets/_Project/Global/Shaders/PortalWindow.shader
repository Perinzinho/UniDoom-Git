Shader "UniDoom/Portal Window"
{
    Properties
    {
        _ViewTex("Gate view", 2D) = "black" {}
        _EdgeColor("Portal rim", Color) = (0.15, 0.75, 0.9, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Name "PortalWindow"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_ViewTex); SAMPLER(sampler_ViewTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                float radius = length(p);
                clip(.99 - radius);
                half3 view = SAMPLE_TEXTURE2D(_ViewTex, sampler_ViewTex, input.uv).rgb;
                half rim = smoothstep(.91, .975, radius);
                half pulse = .85 + .15 * sin(_Time.y * 1.8 + p.y * 5);
                return half4(lerp(view, _EdgeColor.rgb * pulse, rim), 1);
            }
            ENDHLSL
        }
    }
}
