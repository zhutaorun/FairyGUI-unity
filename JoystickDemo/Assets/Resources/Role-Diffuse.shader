Shader "Game/Role-Diffuse" {
	Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		
		_LightDir_0 ("Light0 direction", Vector) = (0.6, -0.8, 0.2, 1.0)
		_LightColor_0 ("Light0 color", Color) = (0.6196,0.5255,0.46275,1)
		_LightIntensity_0 ("Light0 intensity", Range(0,8)) = 0.8
		
		_LightDir_1 ("Light1 direction", Vector) = (-0.9, 0.5, 0.1, 1.0)
		_LightColor_1 ("Light1 color", Color) = (0.2196,0.498,0.61176,1)
		_LightIntensity_1 ("Light1 intensity", Range(0,8)) = 0.6
		
		_RimColor ("Rim color", Color) = (0.4, 0.4, 0.4, 1)
		_RimWidth ("Rim width", Range(0,1)) = 0.7
		
		_Direction ("Direction", Range(-1,1)) = 1
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
	}

	SubShader {
		Pass {
			Tags {"Queue"="Opaque+1" "IgnoreProjector"="True"}
			Lighting Off
			Fog {Mode Off}
			Offset 0,-1
			Cull Back
			ZWrite On
			LOD 200
			
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma exclude_renderers flash xbox360 ps3
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			
			sampler2D _MainTex;
			fixed3 _LightDir_0;
			fixed4 _LightColor_0;
			fixed _LightIntensity_0;
			fixed3 _LightDir_1;
			fixed4 _LightColor_1;
			fixed _LightIntensity_1;
			fixed4 _RimColor;
			fixed _RimWidth;
			fixed _Direction;
			fixed _Cutoff;
			
			struct appdata {
				fixed4 vertex : POSITION;
				fixed3 normal : NORMAL;
				fixed2 texcoord : TEXCOORD0;
			};
			
			struct v2f {
				fixed4 pos : POSITION;
				fixed2 texcoord : TEXCOORD0;
				fixed4 color : TEXCOORD1;
			};
			
			fixed4 _MainTex_ST;
			
			v2f vert(appdata v) {
				v2f o;
				float4x4 modelMatrix = _Object2World;
				fixed4 posWorld = mul(modelMatrix, v.vertex);
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				
				fixed3 normalDirection = normalize(mul(modelMatrix, fixed4(v.normal, 0.0)).xyz);
				fixed3 viewDirection = normalize(_WorldSpaceCameraPos - posWorld.xyz);
				
				fixed3 cDir = fixed3(_Direction,1,1);
				
				fixed3 lightDirection0 = normalize(-_LightDir_0 * cDir);
				fixed3 NDotL0 = max(0.0, dot(normalDirection, lightDirection0));
				fixed4 diffuseReflection0 = fixed4(_LightIntensity_0 * _LightColor_0.rgb * NDotL0, 1.0);
				
				fixed3 lightDirection1 = normalize(-_LightDir_1 * cDir);
				fixed3 NDotL1 = max(0.0, dot(normalDirection, lightDirection1));
				fixed4 diffuseReflection1 = fixed4(_LightIntensity_1 * _LightColor_1.rgb * NDotL1, 1.0);
				
				fixed dotProduct = 1 - dot(normalDirection, viewDirection);
				fixed4 rim = smoothstep(1 - _RimWidth, 1.0, dotProduct) * _RimColor;
				
				o.color = ((UNITY_LIGHTMODEL_AMBIENT + diffuseReflection0 + diffuseReflection1) * 2 + rim);
				
				return o;
			}
			
			fixed4 frag(v2f i) :COLOR {
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				fixed4 c = i.color * tex;
				c.a = tex.a;
				clip(c.a - _Cutoff);
				return c;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}