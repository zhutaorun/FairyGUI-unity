Shader "FairyGUI/Text Brighter Grayed"
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

		Cull Off
		Lighting Off
		ZWrite Off
		Offset -1, -1
		Fog { Mode Off }
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
					if(texcoord.x > 1 )
					{
						texcoord.x = texcoord.x - 10;
						o.flags.x = 1;
					}
					else
						o.flags.x = 1.5;
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
