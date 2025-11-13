Shader "Custom/PortraitStripeWipe2D"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 入场进度 0-1
        _Progress ("Progress", Range(0,1)) = 0

        // 条纹周期（UV 空间距离，越小条纹越密）
        _Period ("Stripe Period (UV)", Float) = 0.1

        // 角度（度） 负值=顺时针倾斜
        _Angle ("Angle (deg)", Range(-180,180)) = -20

        // 条纹边缘柔和过渡宽度（UV）
        _Softness ("Edge Softness", Range(0,0.1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _Progress; // 0..1
            float _Period;   // uv units
            float _Angle;    // degrees
            float _Softness; // uv units

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // 返回条纹掩码：1=显示，0=隐藏
            float StripeMask(float2 uv)
            {
                // 以纹理中心为原点做旋转，得到沿条纹正交方向的坐标
                float2 p = uv - 0.5;
                float rad = radians(_Angle);
                float c = cos(rad);
                float s = sin(rad);
                float2x2 rot = float2x2(c, -s, s, c);
                float2 r = mul(rot, p);

                // 在旋转空间下，沿 x 方向做等距条纹
                // 周期中心：round(r.x / _Period) * _Period
                // 与最近条纹中心的距离：
                float period = max(_Period, 1e-5);
                float center = round(r.x / period) * period;
                float d = abs(r.x - center);

                // 半宽 = 周期的一半 * 进度（进度越大，条纹越“展开”）
                float halfW = 0.5 * period * saturate(_Progress);

                // 柔边：在 [halfW - softness, halfW + softness] 之间平滑过渡
                float soft = max(_Softness, 1e-6);
                // inside => d <= halfW ；用 smoothstep 反向计算：内为1，外为0
                float m = smoothstep(halfW + soft, halfW - soft, d);
                return saturate(m);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // 条纹遮罩
                float mask = StripeMask(i.uv);

                // 将条纹作用到 alpha
                col.a *= mask;
                // 预乘以 alpha，避免半透明边缘发白
                col.rgb *= col.a;

                return col;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
