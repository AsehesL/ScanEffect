// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/WorldMosic"
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
			float4 _MainTex_TexelSize;
			sampler2D_float _CameraDepthTexture;

			float4x4 internalCameraToWorld;

			float4 internalCentPos;
			float4 internalArg;

			float4 internalFade;

			fixed4 frag (v2f i) : SV_Target
			{
				
#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
				i.uv.y = 1 - i.uv.y;

#endif
			fixed c = tex2D(_MainTex, i.uv);

				fixed depth = tex2D(_CameraDepthTexture, i.uv).r;
				fixed4 projPos = fixed4(i.uv.x * 2 - 1, i.uv.y * 2 - 1, -depth * 2 + 1, 1);
				fixed4 worldPos = mul(unity_CameraInvProjection, projPos);
				worldPos = mul(internalCameraToWorld, worldPos);
				worldPos /= worldPos.w;

				fixed dis = length(internalCentPos.xyz - worldPos.xyz);

				fixed a = 1 - saturate((abs(dis - internalArg.x) - internalArg.y) / internalArg.z);
				a = a * internalFade.x + c * internalFade.y;
				return fixed4(a, a, a, a);
			}
			ENDCG
		}
	}
}
