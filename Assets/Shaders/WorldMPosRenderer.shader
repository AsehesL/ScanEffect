// 该shader用于渲染世界空间的扫描效果

Shader "Hidden/WorldMPosRenderer"
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
				float2 uv_depth : TEXCOORD1;
				float3 interpolatedRay : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D_float _CameraDepthTexture;

			//float4x4 internalCameraToWorld;
			float4x4 _FrustumCorners;

			float4 internalCentPos;
			float4 internalArg;

			float4 internalFade;

			v2f vert (appdata v)
			{
				v2f o;
				half index = v.vertex.z;
				v.vertex.z = 0.1;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				o.uv_depth = v.uv.xy;
#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
#endif
				o.interpolatedRay = _FrustumCorners[(int)index].xyz;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));
			

				fixed depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth)));

				fixed4 worldPos = fixed4(depth*i.interpolatedRay, 1);

				worldPos.xyz += _WorldSpaceCameraPos;

				fixed dis = length(internalCentPos.xyz - worldPos.xyz);

				fixed a = 1 - saturate((abs(dis - internalArg.x) - internalArg.y) / internalArg.z);
				a = a * internalFade.x + c * internalFade.y;
				return fixed4(a, a, a, a);
			}
			ENDCG
		}
	}
}
