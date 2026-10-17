Shader "UniDoom/Characters/Zombie Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Original sprite sheet", 2D) = "white" {}
        [NoScaleOffset] _SilhouetteTex("Imported silhouette mask", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite On
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_SilhouetteTex); SAMPLER(sampler_SilhouetteTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float2 uv:TEXCOORD0;
                half4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float2 uv:TEXCOORD0;
                half4 color:COLOR;
                half fog:TEXCOORD1;
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
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // A separate import mask preserves dark details inside the original JPEG.
                clip(SAMPLE_TEXTURE2D(_SilhouetteTex, sampler_SilhouetteTex, i.uv).r - .5);
                color *= i.color;
                color.rgb = MixFog(color.rgb, i.fog);
                return color;
            }
            ENDHLSL
        }
    }
}
