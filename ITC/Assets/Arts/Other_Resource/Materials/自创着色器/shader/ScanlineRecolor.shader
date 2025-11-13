Shader "UI/ScanlineRecolor"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        // 颜色替换相关
        _SrcColor("Source Color (pick)", Color) = (1,1,1,1)
        _TargetColor("Target Color", Color) = (1,0,0,1)
        _Tolerance("Match Tolerance", Range(0,1)) = 0.08
        _ReplaceAmount("Replace Amount", Range(0,1)) = 1.0

        // 扫描线相关
        _AngleDeg("Scan Angle (deg)", Range(-180,180)) = 0 // 0=左->右; -90=上->下; 90=下->上; 180或-180=右->左
        _Progress("Progress (0-1)", Range(0,1)) = 0.0
        _LineSoftness("Line Softness", Range(0,0.25)) = 0.02

        // 自动播放 & 缓动
        [Toggle]_AutoPlay("Auto Play (use _Speed)", Float) = 0
        _Speed("Speed (cycles/sec)", Float) = 0.5
        [Toggle]_UseEase("Ease In-Out", Float) = 1

        // ---- UI 内置遮罩/模板支持（与 UI/Default 对齐）----
        [HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
        _ClipRect("Clip Rect", Vector) = ( -32767, -32767, 32767, 32767 )
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _SrcColor;
            fixed4 _TargetColor;
            float  _Tolerance;
            float  _ReplaceAmount;

            float  _AngleDeg;
            float  _Progress;
            float  _LineSoftness;

            float  _AutoPlay;
            float  _Speed;
            float  _UseEase;

            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float2 worldPosXY    : TEXCOORD1; // for UI clip rect
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPosXY = worldPos.xy;
                return o;
            }

            // 将角度映射为单位方向
            float2 AngleToDir(float deg)
            {
                float rad = radians(deg);
                return normalize(float2(cos(rad), sin(rad)));
            }

            // 将沿方向的投影 t 归一化到 [0,1]，使扫描线在任意角度覆盖完整矩形
            float NormalizeProj(float2 uv, float2 dir)
            {
                float t = dot(uv, dir);
                // 四个角的投影，找 min/max
                float t00 = 0;
                float t10 = dir.x;
                float t01 = dir.y;
                float t11 = dir.x + dir.y;
                float tMin = min(0, min(min(t10, t01), t11));
                float tMax = max(0, max(max(t10, t01), t11));
                return saturate((t - tMin) / max(1e-5, (tMax - tMin)));
            }

            // 简单的 S 曲线缓动
            float Ease01(float x, float useEase)
            {
                return lerp(x, smoothstep(0.0, 1.0, x), step(0.5, useEase));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 取源纹理颜色（已乘上顶点/Graphic 颜色）
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // UI 裁剪
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosXY, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                if (col.a <= 0.001) discard;
                #endif

                // 自动播放进度（可被脚本/材质覆盖）
                float progress = _Progress;
                if (_AutoPlay > 0.5)
                {
                    // _Time.y 为 t/20（Unity 内置），这里使用 frac 让它循环
                    float p = frac(_Time.y * (_Speed * 20.0));
                    progress = Ease01(p, _UseEase);
                }

                // 扫描方向 & 归一化投影
                float2 dir = AngleToDir(_AngleDeg);
                float t01 = NormalizeProj(i.texcoord, dir);

                // 扫描线遮罩（线后=1，线前=0），支持软边
                float soft = _LineSoftness;
                float scanMask = smoothstep(progress - soft, progress, t01);

                // 颜色匹配（靠近 _SrcColor 才会被替换）
                float dist = length(col.rgb - _SrcColor.rgb);         // RGB 距离
                float match = 1.0 - smoothstep(_Tolerance, _Tolerance + 0.01, dist);

                // 综合权重：匹配 * 扫描线 * 全局替换强度
                float w = saturate(match * scanMask * _ReplaceAmount);

                // 输出：在 w 权重下用目标色替换（保持 alpha 原样）
                fixed3 outRGB = lerp(col.rgb, _TargetColor.rgb, w);
                return fixed4(outRGB, col.a);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
