// This is a premultiply-alpha adaptation of the built-in Unity shader "UI/Default" in Unity 5.6.2 to allow Unity UI stencil masking.

Shader "Custom/UI/BloodAuraSpread"
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
		[HDR]_OutlineColor_0 ("Outline Color_0", Color) = (1,1,1,1)
		[HDR]_OutlineColor_1 ("Outline Color_1", Color) = (1,1,1,1)
		_GlowSpeed ("Glow Speed", Range(0,10)) = 2
		_WaveAuraWidth ("WaveAura Width", Range(0,1)) = 1
        _DirRatio("LR & UD Ratio (X=左右, Y=上下)", Vector) = (0.5, 0.5, 0, 0) 
		[Toggle] _EnableAura ("Enable Aura", float) = 1
		 _AuraColor ("Aura Color", Color) = (1, 0.84, 0, 1)
		 _ScanPosXYMIN ("ScanPosXYMIN", Vector) = (-9.7799, -4.43, 0, 0)
         _ScanPosXYMAX ("ScanPosXYMAX", Vector) = (-6.499928, -1.429978, 0, 0)
		 _SubTex ("SubTex", 2D) = "white" {}



		_NoiseScale ("NoiseScale", Float) = 10.0
        _NoiseIntensity ("NoiseIntensity", Float) = 0.8 
        _NoiseTimeSpeed ("NoiseTimeSpeed", Float) = 0.5 
		_BurrLength ("BurrLength", Float) = 0.05
        _BurrEdgeRange ("BurrEdgeRange", Float) = 0.03
		_YRad("YRad",Float) = 0.7
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
				float2 worldPosition : TEXCOORD2;
				float4 objPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float2 _ScanPosXYMIN;
			float2 _ScanPosXYMAX;
		


			float _BurrLength;
            float _BurrEdgeRange;
			float4 _SubTex_ST; 
            float _NoiseScale;
			float _NoiseIntensity;
            float _NoiseTimeSpeed;
			float _YRad;
			VertexOutput vert (VertexInput IN) {
				VertexOutput OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.objPosition = IN.vertex;
				float4 WorldPosition =mul(UNITY_MATRIX_M,IN.vertex);
				float2 vertexSize = _ScanPosXYMAX - _ScanPosXYMIN;
                OUT.worldPosition = (WorldPosition.xy - _ScanPosXYMIN) / vertexSize;


				OUT.vertex = UnityObjectToClipPos(OUT.objPosition);
				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1);
				#endif

				OUT.color = IN.color * float4(_Color.rgb * _Color.a, _Color.a); // Combine a PMA version of _Color with vertexColor.
				return OUT;
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float2 _DirRatio;
			float4 _AuraColor;
			float _WaveAuraWidth;
			float _EnableAura;
			float _GlowSpeed;
			sampler2D _SubTex;
			fixed4 frag (VertexOutput IN) : SV_Target
			{
				float2 uvTemp=IN.texcoord;
				float4 sunCol  = float4(0,0,0,0);
			if (_EnableAura)
			{
					// float2 distance = distance * float2(canvas_size.x / canvas_size.y, 1.0);
					float2 vec  = _DirRatio - IN.worldPosition.xy;
					vec.y/=_YRad;
					float dis = sqrt(vec.x*vec.x+vec.y*vec.y);
					float radius = 0.3;
					float2 vertexSize = _ScanPosXYMAX - _ScanPosXYMIN;
					
				
					
					float4 dis_t =radius- abs(dis - frac(_Time.y*_GlowSpeed/10));
					float4 dis_factor = saturate(dis_t);
					


					float burrMask = smoothstep(0, radius/4, dis_factor.x) + smoothstep(1, 1 - radius/4, dis_factor.x);
                    burrMask = saturate(burrMask);
			
					float2 noiseUV = IN.worldPosition.xy * _NoiseScale;
                    noiseUV += _Time.y * _NoiseTimeSpeed; 
                    noiseUV = TRANSFORM_TEX(noiseUV, _SubTex);
					float noiseValue = tex2D(_SubTex, noiseUV).r;
					float random = noiseValue * 2.0 - 1.0; 
                    random *= _NoiseIntensity;
					             float2 burrDir = normalize(vec + 1e-6); 
                    float2 burrOffset = burrDir * random * _BurrLength * burrMask; 

                    float timeVal = 1;
                    uvTemp = IN.texcoord + dis_factor.x * timeVal * burrDir + burrOffset;

                   
                    sunCol = _AuraColor * dis_factor.x * 3.0 * _WaveAuraWidth*20;
                    sunCol.rgb = saturate(sunCol.rgb);




					// sunCol = _AuraColor*dis_factor.x*7;
					// float timeVal = sin(_Time.y*100*dis)*_WaveAuraWidth;
					// uvTemp = dis_factor.xy*timeVal*normalize(vec)+IN.texcoord;
					


					
			}
			float4 col = tex2D(_MainTex,uvTemp);
			col=sunCol+col*(1-sunCol.a);
						#if defined(_STRAIGHT_ALPHA_INPUT)
						col.rgb *= texColor.a;
						#endif
				 col = (col + _TextureSampleAdd) * IN.color;


	
   				 col *= UnityGet2DClipping(IN.objPosition.xy, _ClipRect);
   				 #ifdef UNITY_UI_ALPHACLIP
  					  clip(col.a - 0.001);
  				  #endif
   				 return col;
	
			}
		ENDCG
		}
	}
	
}
