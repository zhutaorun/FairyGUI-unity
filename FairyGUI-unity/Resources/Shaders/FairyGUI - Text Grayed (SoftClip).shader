Shader "FairyGUI/Text Grayed (SoftClip)" 
{
	Properties
	{
		_MainTex ("Alpha (A)", 2D) = "white" {}
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
			AlphaTest Greater .01
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
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
				float4 flags : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;

				float2 texcoord = v.texcoord;
				if(texcoord.y >1)
				{
					texcoord.y = texcoord.y - 10;
					o.flags.y = 1;
				}
				else
					o.flags.y = 0;

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color;
				o.texcoord = texcoord;
				o.flags.zw = TRANSFORM_TEX(mul(_Object2World, v.vertex).xy, _MainTex);
				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				half4 col = i.color;
				if(i.flags.y==1)
				{
					float grey = dot(col.rgb, float3(0.299, 0.587, 0.114));
					col.rgb = float3(grey, grey, grey);  
				}
				else
					col.rgb = float3(0.8, 0.8, 0.8);  
			    col.a *= tex2D(_MainTex, i.texcoord).a;
				
				//float2 factor = (float2(1.0, 1.0) - abs(i.flags.zw)) * _ClipSharpness;
				float2 factor = float2(0,0);
				if(i.flags.z<0)
					factor.y = (1.0-abs(i.flags.z)) * _ClipSharpness.x;
				else
					factor.y = (1.0-i.flags.z) * _ClipSharpness.z;
				if(i.flags.w<0)
					factor.x = (1.0-abs(i.flags.w)) * _ClipSharpness.w;
				else
					factor.x = (1.0-i.flags.w) * _ClipSharpness.y;
				col.a *= clamp( min(factor.x, factor.y), 0.0, 1.0);
				
				return col;
			}
			ENDCG
		}
	}
}
