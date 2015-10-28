Shader "Cooldown mask" {
	Properties 
	{
		_MaskColor ("Mask Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_MaskTex ("Mask (A)", 2D) = "white" {}
		_Progress ("Progress", Range(0,1)) = 1
	}

	Category 
	{
		SubShader 
		{
			Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType" = "Transparent"}
			
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

				sampler2D _MainTex;
				sampler2D _MaskTex;
				float4 _MainTex_ST;
				fixed4 _MaskColor;
				float _Progress;

				struct appdata
				{
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
				};
	
				v2f vert (appdata v)
				{
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
					return o;
				}

				fixed4 frag(v2f i) : COLOR
				{
					fixed4 c = tex2D(_MainTex, i.uv);
					fixed4 ca = tex2D(_MaskTex, i.uv);
					c.rgb = (ca.a > _Progress || _Progress ==0) ? c.rgb * _MaskColor : c.rgb;
					return c;
				}
				ENDCG
			}
		}
	}
	Fallback Off
}