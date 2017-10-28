
Shader "Hidden/MosicEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _PreTex;
			sampler2D _EffectTex;

			float2 _EffectScale;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
#if UNITY_UV_STARTS_AT_TOP
//			i.uv.y = 1 - i.uv.y;
#endif
				float2 fl = floor(i.uv * _EffectScale);
				float dp = tex2D(_PreTex, (fl + float2(0.5, 0.5)) / _EffectScale);
				
				float4 led = tex2D(_EffectTex, i.uv * _EffectScale - fl);
				
				col.rgb += led.rgb*dp;

				return col;
			}
			ENDCG
		}
	}
}
