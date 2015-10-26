Shader "FairyGUI/BMFont (SoftClip)" 
{
	Properties
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "black" {}
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
			Fog { Mode Off }
			Offset -1, -1
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
				if(texcoord.x >= 40 )
				{
					texcoord.x = texcoord.x - 40;
					o.flags.x = 3;
				}
				else if(texcoord.x >= 30 )
				{
					texcoord.x = texcoord.x - 30;
					o.flags.x = 2;
				}
				else if(texcoord.x >= 20 )
				{
					texcoord.x = texcoord.x - 20;
					o.flags.x = 1;
				}
				else if(texcoord.x >= 10 )
				{
					texcoord.x = texcoord.x - 10;
					o.flags.x = 0;
				}
				else
					o.flags.x = 3;
						
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
				half4 tcol = tex2D(_MainTex, i.texcoord);
				col.a *= ((i.flags.x==0 || i.flags.x==4)?tcol.a:(i.flags.x==1?tcol.r:(i.flags.x==2?tcol.g:tcol.b)));

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
