Shader "UI/FrostedGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,0.65)

        _FrostTex ("Frost Texture", 2D) = "white" {}
        _FrostIntensity ("Frost Intensity", Range(0,1)) = 0.6
        _Distortion ("Distortion", Range(0,1)) = 0.25
        _BlurStrength ("Blur Strength", Range(0,3)) = 1.5

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "FROSTED_UI"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _FrostTex;
            float4 _FrostTex_ST;
            half _FrostIntensity;
            half _Distortion;
            half _BlurStrength;

            fixed4 _Color;

            sampler2D _GrabBlurTexture_0;
            sampler2D _GrabBlurTexture_1;
            sampler2D _GrabBlurTexture_2;
            sampler2D _GrabBlurTexture_3;

            float4 _ClipRect;
            float _UseUIAlphaClip;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvFrost : TEXCOORD1;
                fixed4 color : COLOR;
                float4 worldPosition : TEXCOORD2;
                float4 grabPos : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.uvFrost = TRANSFORM_TEX(v.texcoord, _FrostTex);
                o.color = v.color;
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.position);
                return o;
            }

            half4 SampleBlurChain(float4 grabPos, half surfSmooth)
            {
                half4 ref00 = tex2Dproj(_GrabBlurTexture_0, grabPos);
                half4 ref01 = tex2Dproj(_GrabBlurTexture_1, grabPos);
                half4 ref02 = tex2Dproj(_GrabBlurTexture_2, grabPos);
                half4 ref03 = tex2Dproj(_GrabBlurTexture_3, grabPos);

                half step00 = smoothstep(0.75, 1.00, surfSmooth);
                half step01 = smoothstep(0.5, 0.75, surfSmooth);
                half step02 = smoothstep(0.05, 0.5, surfSmooth);
                half step03 = smoothstep(0.00, 0.05, surfSmooth);

                half4 refraction = lerp(ref03, lerp( lerp( lerp(ref03, ref02, step02), ref01, step01), ref00, step00), step03);
                return refraction;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half4 maskSample = tex2D(_MainTex, i.uv) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                maskSample.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                if (_UseUIAlphaClip > 0.5f)
                {
                    clip(maskSample.a - 0.001);
                }

                float4 frostSample = tex2D(_FrostTex, i.uvFrost);
                float2 frostOffset = frostSample.rg * 2.0 - 1.0;
                float frostMask = frostSample.r;

                float surfSmooth = saturate(1.0 - frostMask * _FrostIntensity);
                float strength01 = saturate(_BlurStrength / 3.0);
                surfSmooth = saturate(lerp(surfSmooth, 0.0, strength01));

                float4 grabPos = i.grabPos;
                grabPos.xy += frostOffset * (_Distortion * grabPos.w);

                half4 blurred = SampleBlurChain(grabPos, surfSmooth);

                half3 tintedRgb = blurred.rgb * _Color.rgb * maskSample.rgb;
                half4 outputColor = half4(tintedRgb, 1.0);
                outputColor.a = maskSample.a * _Color.a;

                outputColor.rgb *= outputColor.a;

                return outputColor;
            }
            ENDCG
        }
    }

    Fallback Off
}

