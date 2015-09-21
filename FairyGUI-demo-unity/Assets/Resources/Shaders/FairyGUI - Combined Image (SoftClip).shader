Shader "FairyGUI/Combined Image (SoftClip)"
{
	Properties
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "black" {}
		_AlphaTex ("Alpha Texture", 2D) = "white"{}
	}

	SubShader
	{
		LOD 200

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Offset -1, -1
			Fog { Mode Off }
			ColorMask RGB
			AlphaTest Greater .01
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float4 _MainTex_ST;
			float4 _ClipSharpness = float4(0, 0, 0, 0);

			struct appdata_t
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 worldPos : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				o.worldPos = TRANSFORM_TEX(mul(_Object2World, v.vertex).xy, _MainTex);
				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				half4 col = tex2D(_MainTex, i.texcoord) * i.color;
		    col.a *= tex2D(_AlphaTex, i.texcoord).g;
		    
				//float2 factor = (float2(1.0, 1.0) - abs(i.worldPos)) * _ClipSharpness;
				float2 factor = float2(0,0);
				if(i.worldPos.x<0)
					factor.y = (1.0-abs(i.worldPos.x)) * _ClipSharpness.x;
				else
					factor.y = (1.0-i.worldPos.x) * _ClipSharpness.z;
				if(i.worldPos.y<0)
					factor.x = (1.0-abs(i.worldPos.y)) * _ClipSharpness.w;
				else
					factor.x = (1.0-i.worldPos.y) * _ClipSharpness.y;
				col.a *= clamp( min(factor.x, factor.y), 0.0, 1.0);
				
				return col;
			}
			ENDCG
		}
	}
}
