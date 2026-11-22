Shader "UniDoom/UI/MenuSignFlicker"
{
    Properties
    {
        [PerRendererData] _MainTex ("Campus photo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LeftLight ("Left sign light", Range(0,1)) = 1
        _RightLight ("Right sign light", Range(0,1)) = 1
        _PostLights ("First four post lights", Vector) = (1,1,1,1)
        _FifthPostLight ("Fifth post light", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _LeftLight;
            float _RightLight;
            float4 _PostLights;
            float _FifthPostLight;

            float PostMask(float2 pixel, float2 center, float2 radius)
            {
                return 1 - smoothstep(1, 1.3, length((pixel - center) / radius));
            }

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 photo = tex2D(_MainTex, input.uv) * input.color;
                if (_LeftLight >= .999 && _RightLight >= .999 &&
                    _PostLights.x >= .999 && _PostLights.y >= .999 &&
                    _PostLights.z >= .999 && _PostLights.w >= .999 &&
                    _FifthPostLight >= .999) return photo;

                float outage = 0;
                if (input.uv.x > .716 && input.uv.x < .874 &&
                    input.uv.y > .562 && input.uv.y < .672 &&
                    (_LeftLight < .999 || _RightLight < .999))
                {
                    // The two spotlights illuminate only the entrance sign.
                    float2 left = (input.uv - float2(.767, .62)) / float2(.077, .087);
                    float2 right = (input.uv - float2(.836, .62)) / float2(.071, .084);
                    float leftCone = 1 - smoothstep(.25, 1, length(left));
                    float rightCone = 1 - smoothstep(.25, 1, length(right));
                    float signArea = smoothstep(.716, .73, input.uv.x) *
                        (1 - smoothstep(.86, .874, input.uv.x)) *
                        smoothstep(.562, .574, input.uv.y) *
                        (1 - smoothstep(.655, .672, input.uv.y));
                    outage = max((1 - _LeftLight) * leftCone,
                        (1 - _RightLight) * rightCone) * signArea;
                }

                if (input.uv.x > .56 && input.uv.x < .95 &&
                    input.uv.y > .54 && input.uv.y < .73)
                {
                    // Globe centers and radii are measured in the 1920 x 1080 campus photo.
                    float2 pixel = float2(input.uv.x * 1920, (1 - input.uv.y) * 1080);
                    if (_PostLights.x < .999)
                        outage = max(outage, (1 - _PostLights.x) * PostMask(pixel, float2(1126, 342), float2(22, 27)));
                    if (_PostLights.y < .999)
                        outage = max(outage, (1 - _PostLights.y) * PostMask(pixel, float2(1314, 436), float2(12, 14)));
                    if (_PostLights.z < .999)
                        outage = max(outage, (1 - _PostLights.z) * PostMask(pixel, float2(1382, 469), float2(9, 10)));
                    if (_PostLights.w < .999)
                        outage = max(outage, (1 - _PostLights.w) * PostMask(pixel, float2(1686, 459), float2(11, 11)));
                    if (_FifthPostLight < .999)
                        outage = max(outage, (1 - _FifthPostLight) * PostMask(pixel, float2(1783, 410), float2(18, 18)));
                }

                // Keep dark lettering, poles and the rest of the photograph untouched.
                float litSurface = smoothstep(.025, .20, dot(photo.rgb, float3(.2126, .7152, .0722)));
                photo.rgb *= 1 - .94 * outage * litSurface;
                return photo;
            }
            ENDCG
        }
    }
}
