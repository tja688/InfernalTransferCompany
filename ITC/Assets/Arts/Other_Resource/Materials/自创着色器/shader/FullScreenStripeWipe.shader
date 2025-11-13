Shader "Custom/FullScreenStripeWipe"
{
    Properties
    {
        _Color     ("Tint", Color) = (1,1,1,1)
        _Progress  ("Progress", Range(0,1)) = 0
        _Angle     ("Angle (deg)", Range(-180,180)) = -20
        _Period    ("Stripe Period (UV)", Float) = 0.06
        _Softness  ("Edge Softness (UV)", Range(0,0.2)) = 0.02
        _Direction ("Direction (+/-1)", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4 _Color;
            float  _Progress, _Angle, _Period, _Softness, _Direction;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // 返回条纹推进的覆盖 alpha（1=遮罩可见）
            float StripeCover(float2 uv)
            {
                // 旋转坐标
                float2 p = uv - 0.5;
                float rad = radians(_Angle);
                float c = cos(rad), s = sin(rad);
                float2x2 rot = float2x2(c,-s,s,c);
                float2 r = mul(rot, p);

                // 推进轴：沿 y 方向推进；_Progress 控制推进前沿的位置
                float y = saturate( (r.y * 0.8) + 0.5 );
                float edge = _Progress;

                // 在边界上叠条纹（沿 x 方向）
                float period = max(_Period, 1e-4);
                // 用 floor(x/period + 0.5)*period 代替 round 以提高兼容性
                float cx = floor(r.x/period + 0.5) * period;
                float dstripe = abs(r.x - cx);

                float halfW = 0.5 * period * saturate(_Progress);  // 展开幅度随进度增大
                float mStripe = smoothstep(halfW + _Softness, halfW - _Softness, dstripe);

                // 基础推进（无条纹时的硬推进）
                float baseMask = step(y, edge);

                // 在推进边界附近引入条纹过渡
                float band = smoothstep(edge - _Softness*4, edge + _Softness*4, y); // 0=已覆盖, 1=未覆盖
                float stripeEdge = lerp(1.0, mStripe, 1.0 - band);

                // 方向翻转
                float cover = (_Direction >= 0.0) ? (baseMask * stripeEdge)
                                                  : ((1.0 - baseMask) * stripeEdge);

                return saturate(cover);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float a = StripeCover(i.uv);
                return fixed4(_Color.rgb, a);
            }
            ENDCG
        }
    }
}
