Shader "Custom/UI/ImageFontLightColor"
{
    Properties
    {
        _SubTex("SubTexture", 2D) = "white" {}
        _MainTex ("Texture", 2D) = "white" {}
        _TargetColor ("TargetColor", Color) = (1,1,1,1)
        _LightColor ("LightColor", Color) = (1,1,1,1)
         _EdgeThreshold ("Edge Threshold", Range(0, 1)) = 0.15
        _EdgeSoftness ("Edge Softness", Range(0, 0.5)) = 0.1
        _BlurSize ("Blur Size", Float) = 2.0
        _GlowIntensity ("Glow Intensity", Float) = 1.0
        _BlurSamples ("Blur Samples", Range(3, 20)) = 5
        _GlowSpread("GlowSpread", Float) = 1.5
        _GlowSaturation("GlowSaturation", Float) = 1.3    
        _LightDegree("Light Degree", Range(0,5)) = 1.0
    }
    SubShader
    {
        	Tags
		{
			"Queue"="Transparent"
		
		}
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
        
            sampler2D _SubTex;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _TargetColor;
            fixed4 _LightColor;
            float _EdgeThreshold;
            float _EdgeSoftness;
            float _BlurSize;
            float _GlowIntensity;
            int _BlurSamples;
            float _DownSampling;
            float _GlowSpread;
            float _GlowSaturation;
            float _LightDegree;

                float GetColorWeight(float4 col)
            {
                float4 diff = abs(col - _TargetColor);
                float sum = diff.r + diff.g + diff.b;
                // smoothstep: edge0, edge1, x
                return 1.0 - smoothstep(_EdgeThreshold, _EdgeThreshold + _EdgeSoftness, sum);
                // return step(sum,_EdgeThreshold);
            }   


float4 ApplyRadialGlow(float2 uv)
{
    float4 glow = float4(0,0,0,0);
    float totalWeight = 0;
    
    int radialSamples = min(_BlurSamples, 16);
    int distanceSamples = min(_BlurSamples / 2, 8);
  
    for (int i = 0; i < radialSamples; ++i)
    {
        float angle = i * 6.28318 / radialSamples;
        float2 dir = float2(cos(angle), sin(angle));
        
        for (int j = 1; j <= distanceSamples; ++j)
        {
            float distance = j * _BlurSize * _MainTex_TexelSize.x * _GlowSpread;
            float2 offset = dir * distance;
            float2 sampleUV = uv + offset;
            
            float4 sampleCol = tex2D(_MainTex, sampleUV);
            float weight = GetColorWeight(sampleCol);
            
            // 距离衰减（越远离中心越淡）
            weight *= exp(-distance * 8.0);
            
            // **多彩发光核心**：保留原色并增强亮度和饱和度
            float3 enhancedColor = _LightColor*(tex2D(_SubTex,frac(_Time.y*0.005)));
            // 亮度提升
            enhancedColor *= _LightDegree;
            // 饱和度提升
            float lum = dot(enhancedColor, float3(0.299, 0.587, 0.114));
            enhancedColor = lerp(lum.xxx, enhancedColor, _GlowSaturation);
            
            glow.rgb += enhancedColor * weight;
            glow.a += weight;
            totalWeight += weight;
        }
    }
    
    if (totalWeight > 0)
    {
        glow.rgb /= totalWeight;
        glow.a /= totalWeight;
    }
    
    return glow;
}

          fixed4 frag (v2f i) : SV_Target
{
    // 采样原始颜色
    fixed4 col = tex2D(_MainTex, i.uv);
    
    // 计算匹配权重（用于颜色替换）
    float weight = GetColorWeight(col);
    
    // **功能1：颜色替换** - 将匹配区域替换为_LightColor
    fixed4 replacedColor = lerp(col, _LightColor, weight);
    
    // **功能2：多彩发光** - 基于周围匹配像素产生光晕
    float4 glow = ApplyRadialGlow(i.uv);
    
    // 使用SubTex作为艺术遮罩控制发光区域
    
    
    float glowStrength = glow.a * _GlowIntensity * (1.0 - weight);
    replacedColor.rgba += glow.rgba * glowStrength;
    clip(replacedColor.a - 0.01);
    return replacedColor;
}
            ENDCG
        }
    }
}
