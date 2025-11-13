Shader "Custom/UniversalElasticDeformation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        
        // --- 自定义变形参数 ---
        _DeformationAngle ("Deformation Angle (Degrees)", Range(0, 360)) = 0
        _Strength ("Deformation Strength", Float) = 0
        _CurveFreq ("Deformation Width (Frequency)", Float) = 3
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

        Cull Off
        Lighting Off
        ZWrite Off
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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            // 声明我们的参数
            float _DeformationAngle;
            float _Strength;
            float _CurveFreq;

            // 旋转函数的辅助逻辑
            float2 Rotate(float2 position, float degrees)
            {
                float rad = radians(degrees);
                float s = sin(rad);
                float c = cos(rad);
                
                // 2D旋转矩阵公式
                return float2(
                    position.x * c - position.y * s,
                    position.x * s + position.y * c
                );
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // 1. 获取原始顶点位置 (Object Space)
                float3 pos = v.vertex.xyz;

                // 2. 【关键步骤】先将坐标旋转到指定的变形角度
                // 这样我们就只需要处理 X 轴的变形，就能适应所有方向
                float2 rotatedPos = Rotate(pos.xy, _DeformationAngle);

                // 3. 【计算变形】
                // 使用 Cosine 函数根据 Y 轴 (高度) 算出 X 轴 (宽度) 的偏移量
                // rotatedPos.y * _CurveFreq: 控制波形的密度
                // cos(...) * _Strength: 控制变形的幅度
                float offset = cos(rotatedPos.y * _CurveFreq) * _Strength;

                // 应用偏移到旋转后的 X 轴
                rotatedPos.x += offset;

                // 4. 【还原坐标】将坐标反向旋转回去 (-_DeformationAngle)
                pos.xy = Rotate(rotatedPos, -_DeformationAngle);

                // 5. 转换到裁剪空间 (常规流程)
                o.vertex = UnityObjectToClipPos(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color; // 支持 Sprite Renderer 的颜色修改

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 简单的纹理采样
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}