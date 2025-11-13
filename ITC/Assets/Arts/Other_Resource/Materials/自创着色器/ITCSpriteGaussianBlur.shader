Shader "Custom/ITCSpriteGaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1) // 用于SpriteRenderer的颜色
        _BlurRadius ("Blur Radius", Range(0, 1)) = 0.005 // 模糊半径
        _BlurIterations ("Blur Iterations", Int) = 3 // 模糊迭代次数 (近似采样次数)
    }
    SubShader
    {
        Tags
        {
            // --- 这是唯一的修改 ---
            // 将队列设置为 2999 (Transparent-1)，
            // 以确保它在默认UI (队列 3000) 之前渲染。
            "Queue"="Transparent-1"
            // --- 修改结束 ---

            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Sprite" // 让它在Inspector中显示为Sprite预览
        }
        LOD 100

        // 兼容SpriteRenderer的混合模式
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 使Shader Graph兼容
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // 获取SpriteRenderer的颜色
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR; // 将颜色传递给片段着色器
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity会自动提供，用于计算像素大小
            fixed4 _Color;
            float _BlurRadius;
            int _BlurIterations;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color; // 将SpriteRenderer的颜色和Shader属性颜色相乘
                return o;
            }

            // 简单的高斯模糊采样 (多次迭代近似高斯)
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0,0,0,0);
                float2 texelSize = _MainTex_TexelSize.xy; // 单个像素的UV大小

                // 根据模糊半径和迭代次数调整采样步长
                float stepSize = _BlurRadius / max(1, _BlurIterations); // 避免除以0

                // 核心模糊循环
                for (int iter = 0; iter < _BlurIterations; ++iter)
                {
                    // 沿着X轴和Y轴进行采样
                    col += tex2D(_MainTex, i.uv + float2(stepSize * iter, 0));
                    col += tex2D(_MainTex, i.uv - float2(stepSize * iter, 0));
                    col += tex2D(_MainTex, i.uv + float2(0, stepSize * iter));
                    col += tex2D(_MainTex, i.uv - float2(0, stepSize * iter));

                    // 对角线采样 (可选，增加质量但增加采样次数)
                    col += tex2D(_MainTex, i.uv + float2(stepSize * iter, stepSize * iter));
                    col += tex2D(_MainTex, i.uv - float2(stepSize * iter, stepSize * iter));
                    col += tex2D(_MainTex, i.uv + float2(stepSize * iter, -stepSize * iter));
                    col += tex2D(_MainTex, i.uv - float2(stepSize * iter, -stepSize * iter));
                }
                
                // 如果 _BlurIterations 为0，则保持原图
                if (_BlurIterations == 0) return tex2D(_MainTex, i.uv) * i.color;

                col /= (_BlurIterations * 8.0); // 平均颜色

                // 混合原始透明度，因为模糊可能改变颜色但我们想保留原始形状的透明度
                fixed4 originalPixel = tex2D(_MainTex, i.uv);
                col.a = originalPixel.a; // 保留原始图片的透明度

                return col * i.color; // 乘以SpriteRenderer的颜色
            }
            ENDCG
        }
    }
}