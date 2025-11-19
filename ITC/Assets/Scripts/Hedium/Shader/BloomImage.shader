// This is a premultiply-alpha adaptation of the built-in Unity shader "UI/Default" in Unity 5.6.2 to allow Unity UI stencil masking.

Shader "Custom/UI/BloomImage"
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
		_Edge ("Edge", Range(0, 0.5)) = 0.1
		_EdgeColor ("EdgeColor", Color) = (1, 1, 1, 1)
		_UVScale ("UVScale", Range(0, 30)) = 0.13
		_Intensity ("Intensity", Range(0, 3)) = 1.86








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
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 vertex : SV_POSITION;
				float4 objVertex : TEXCOORD0;
				fixed2 texcoord : TEXCOORD1;
				fixed4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			
		
 
			fixed _Edge;
			fixed4 _EdgeColor;
			sampler2D _MainTex;
			float _UVScale;
			float _Intensity;
			float _Test;
			float4 _MainTex_TexelSize;
			float _EnableHighLight;
		
			
			VertexOutput vert (VertexInput IN) {
				VertexOutput OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.objVertex = IN.vertex;
				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1);
				#endif

				OUT.color = IN.color * float4(_Color.rgb * _Color.a, _Color.a); // Combine a PMA version of _Color with vertexColor.
				return OUT;
			}

		
			fixed4 frag (VertexOutput IN) : SV_Target
			{
				float4 col = tex2D(_MainTex, IN.texcoord);
			
				#if defined(_STRAIGHT_ALPHA_INPUT)
						col.rgb *= texColor.a;
						#endif
			 col = (col + _TextureSampleAdd) * IN.color;
			if(_EnableHighLight)

			{
			    fixed x = IN.texcoord.x ;
				fixed y = IN.texcoord.y;
 
 
				float2 leftUp = float2(_Edge,1-_Edge);
 
				float2 leftDown = float2(_Edge,_Edge);
 
				float2 RightUp = float2(1-_Edge,1-_Edge);
 
				float2 RightDown = float2(1-_Edge,_Edge);
 
			
 
 
				float leftUpD = distance(leftUp,IN.texcoord); 
 
				float2 leftDownD = distance(leftDown,IN.texcoord); 
 
				float2 RightUpD = distance(RightUp,IN.texcoord); 
 
				float2 RightDownD =  distance(RightDown,IN.texcoord); 
 
				
				float alpha =0;
 
 
				if(x<_Edge && (1-y)<_Edge)
				    alpha=  pow((_Edge-leftUpD)/_Edge,_Intensity);
				else if(x<_Edge && y<_Edge)
				    alpha=  pow((_Edge-leftDownD)/_Edge,_Intensity);
				else if((1-x)<_Edge && y<_Edge)
				    alpha=  pow((_Edge-RightDownD)/_Edge,_Intensity);
				else if((1-x)<_Edge && (1-y)<_Edge)
				    alpha=  pow((_Edge-RightUpD)/_Edge,_Intensity);
				else if((x < _Edge))
				    alpha = pow(x/_Edge,_Intensity);
				else if(1 - x < _Edge)
				    alpha = pow((1-x)/_Edge,_Intensity);
				else if(1 - y < _Edge)
					alpha = pow((1-y)/_Edge,_Intensity);
				else if(y < _Edge)    
					alpha =pow(y/_Edge,_Intensity);
				else 
				{
				      float4 addUV = float4(-_UVScale,-_UVScale,1+_UVScale*2,1+_UVScale*2);
					 fixed4 col = tex2D(_MainTex, IN.texcoord*addUV.zw+addUV.xy);
					 alpha=1;
					 _EdgeColor.xyz =col.xyz;
				}
 
			  return fixed4(_EdgeColor.xyz,alpha);


			}
			
				
   				 col *= UnityGet2DClipping(IN.objVertex.xy, _ClipRect);
   				 #ifdef UNITY_UI_ALPHACLIP
  					  clip(col.a - 0.001);
  				  #endif
   				 return col;
	
			}
		ENDCG
		}
	}
	
}
