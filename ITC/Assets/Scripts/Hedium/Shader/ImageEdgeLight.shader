// This is a premultiply-alpha adaptation of the built-in Unity shader "UI/Default" in Unity 5.6.2 to allow Unity UI stencil masking.

Shader "Custom/UI/ImageEdgeLight"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
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
		_OutLineWidth ("Outline Width", Range(0,25)) = 1
		_Overall_Alpha ("_Overall_Alpha", float) = 1
		_OutlineMinAlpha ("Outline Min Alpha", Range(0,0.5)) = 0.1
        _OutlineMaxAlpha ("Outline Max Alpha", Range(0,2)) = 0.3

		[Toggle] _EnableHighLight ("Enable HighLight", float) = 1

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
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			
		
			VertexOutput vert (VertexInput IN) {
				VertexOutput OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1);
				#endif

				OUT.color = IN.color * float4(_Color.rgb * _Color.a, _Color.a); // Combine a PMA version of _Color with vertexColor.
				return OUT;
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

            float _OutlineMinAlpha, _OutlineMaxAlpha, _OutLineWidth, _GlowSpeed;
            float4 _OutlineColor_0, _OutlineColor_1;
            float _Overall_Alpha;
			float _EnableHighLight;

			fixed4 frag (VertexOutput IN) : SV_Target
			{
			float4 col = tex2D(_MainTex, IN.texcoord);
			
				#if defined(_STRAIGHT_ALPHA_INPUT)
						col.rgb *= texColor.a;
						#endif
			 col = (col + _TextureSampleAdd) * IN.color;
			if(_EnableHighLight)

			{
			            	float Alpha = col.a;
							for (int x = -10 ; x < 10; x++)
							{
								for(int y = -10; y < 10; y++)
								{
									float2 offset = (float2(x, y)*_MainTex_TexelSize.xy*_OutLineWidth )/10;
									Alpha += tex2D(_MainTex, IN.texcoord + offset).a;
								}
							}
							Alpha /= 361;
							clip(Alpha - _OutlineMinAlpha);
							Alpha = clamp(Alpha,0,_OutlineMaxAlpha);
							float3 OutLine =  lerp(_OutlineColor_0, _OutlineColor_1, (0.5 * sin(_GlowSpeed * _Time.y + 2* IN.texcoord.x) + 0.5)) * Alpha;
			
								    col.rgb += OutLine*col.a;
									col.a += Alpha;
							col.a *= pow(_Overall_Alpha,3);
			}
			
				
   				 col *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
   				 #ifdef UNITY_UI_ALPHACLIP
  					  clip(col.a - 0.001);
  				  #endif
   				 return col;
	
			}
		ENDCG
		}
	}
	
}
