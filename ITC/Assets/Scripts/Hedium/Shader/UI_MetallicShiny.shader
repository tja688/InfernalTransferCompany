// This is a premultiply-alpha adaptation of the built-in Unity shader "UI/Default" in Unity 5.6.2 to allow Unity UI stencil masking.

Shader "Custom/UI/MetallicScan"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[Toggle(_CANVAS_GROUP_COMPATIBLE)] _CanvasGroupCompatible("CanvasGroup Compatible", Int) = 0
		_Color ("Tint", Color) = (1,1,1,1)

		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector][Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255

		[HideInInspector] _ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		// Outline properties are drawn via custom editor.
		[HideInInspector] _OutlineWidth("Outline Width", Range(0,8)) = 3.0
		[HideInInspector] _OutlineColor("Outline Color", Color) = (1,1,0,1)
		[HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Int) = 1024
		[HideInInspector] _ThresholdEnd("Outline Threshold", Range(0,1)) = 0.25
		[HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0,1)) = 1.0
		[HideInInspector][MaterialToggle(_USE8NEIGHBOURHOOD_ON)] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
		[HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0,3)) = 0
		_SubTex ("SubTex", 2D) = "white" {}
		_ScanSpeed ("ScanSpeed", Float) = 0.1
        _ScanPosXYMIN ("ScanPosXYMIN", Vector) = (-9.7799, -4.43, 0, 0)
        _ScanPosXYMAX ("ScanPosXYMAX", Vector) = (-6.499928, -1.429978, 0, 0)
		_TransparentDegree("TransparentDegree",Float) = 0.4
		 _UseScan("Use Scan", Float) = 1.0 
		 _GoldColor ("Gold Color", Color) = (1, 0.84, 0, 1)
		 _ColorThreshold ("ColorThreshold", float) = 0.5
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
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Fog { Mode Off }
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Normal"

		CGPROGRAM
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma shader_feature _ _CANVAS_GROUP_COMPATIBLE
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_ALPHACLIP

			struct VertexInput {
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 objPosition : TEXCOORD1;
				float2 worldPosition : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float2 _ScanPosXYMIN; 
            float2 _ScanPosXYMAX;
			float _TransparentDegree;
			float _UseScan;
			float _ColorThreshold;
			VertexOutput vert (VertexInput IN) {
				VertexOutput OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				
				OUT.objPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.objPosition);
				OUT.texcoord = IN.texcoord;


				float4 WorldPosition =mul(UNITY_MATRIX_M,IN.vertex);
			
				float2 vertexSize = _ScanPosXYMAX - _ScanPosXYMIN;
                OUT.worldPosition = (WorldPosition.xy - _ScanPosXYMIN) / vertexSize;




				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1);
				#endif



				OUT.color = IN.color * float4(_Color.rgb * _Color.a, _Color.a); // Combine a PMA version of _Color with vertexColor.
				return OUT;
			}

			sampler2D _MainTex;
			float _ScanSpeed;
			sampler2D _SubTex;
			float4 _GoldColor;
fixed4 frag (VertexOutput IN) : SV_Target
{
    half4 texColor = tex2D(_MainTex, IN.texcoord);

    #if defined(_STRAIGHT_ALPHA_INPUT)
        texColor.rgb *= texColor.a;
    #endif



	texColor = (texColor + _TextureSampleAdd) * IN.color;
	float lineWidth = 0.1;
	float2 pos = IN.worldPosition.xy;


	



	float2 runDis =frac(_Time.y * _ScanSpeed); 
	pos.y+=runDis;
	pos.y=frac(pos.y);
	half4 subCol = tex2D(_SubTex,pos);

	


	
    float3 targetColor = _GoldColor.rgb; 
    float3 pixColor = texColor.rgb;

    float diffSum = abs(pixColor.r - targetColor.r) + abs(pixColor.g - targetColor.g);

  float diff = (diffSum > (1-_ColorThreshold)) ? 1 : diffSum;

  
   

    float goldFactor = saturate(1.0 - diff);   
    goldFactor*=goldFactor;
	


	float mask = texColor.a * subCol.a * goldFactor  * _UseScan*_TransparentDegree;

	half4 outCol  =texColor+subCol*mask;

	// float2 normPos = pos * 0.5 + 0.5;
	// float offset = frac(_Time.y * _ScanSpeed); 
	// float line1 = abs(normPos.y - ( offset-normPos.x)); 
	// float mask = smoothstep(lineWidth, 0.0, line1); 
	// half4 lineColor = fixed4(1,0,0,1);
	// half4 outCol = lerp(texColor, lineColor, mask);

    #ifdef _CANVAS_GROUP_COMPATIBLE
        outCol.rgb *= IN.color.a;
    #endif
	
    outCol *= UnityGet2DClipping(IN.objPosition.xy, _ClipRect);

    #ifdef UNITY_UI_ALPHACLIP
        clip(outCol.a - 0.001);
    #endif

    return outCol;
}
		ENDCG
		}
	}

}
