// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/MosicLed"
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
			sampler2D _LedTex;

			float2 _LedScale;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
#if UNITY_UV_STARTS_AT_TOP
//			i.uv.y = 1 - i.uv.y;
#endif
				float2 fl = floor(i.uv * _LedScale);
				float dp = tex2D(_PreTex, (fl + float2(0.5, 0.5)) / _LedScale);
				
				float4 led = tex2D(_LedTex, i.uv * _LedScale - fl);
				
				col.rgb += led.rgb*dp;

				return col;
			}
			ENDCG
		}
	}
}
