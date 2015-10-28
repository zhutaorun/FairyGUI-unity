Shader "FairyGUI/BMFont Grayed"
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

		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		Offset -1, -1
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

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
					fixed2 flags : TEXCOORD1;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;

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
					o.texcoord = TRANSFORM_TEX(texcoord, _MainTex);
					o.color = v.color;
					return o;
				}

				half4 frag (v2f i) : COLOR
				{
					half4 col = i.color;
					half4 tcol = tex2D(_MainTex, i.texcoord);
					col.a *= ((i.flags.x==0 || i.flags.x==4)?tcol.a:(i.flags.x==1?tcol.r:(i.flags.x==2?tcol.g:tcol.b)));
					
					if(i.flags.y==1)
					{
						float grey = dot(col.rgb, float3(0.299, 0.587, 0.114));
						col.rgb = float3(grey, grey, grey);  
					}
					else
						col.rgb = float3(0.8, 0.8, 0.8);  
					col.a *= tex2D(_MainTex, i.texcoord).a * i.flags.x;
					return col;
				}
			ENDCG
		}
	}
}
