Shader "Hidden/SetAlphaFromTexture"
{
	Properties
	{
		_MainTex ("Texture (RGB)", 2D) = "white" {}
        _AlphaTex ("Alpha Texture (A)", 2D) = "white" {}
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;

			float4 frag(v2f i) : SV_Target
			{
				float3 rgb = tex2D(_MainTex, i.uv).rgb;

                float4 alphaSourceColor = tex2D(_AlphaTex, i.uv);

                return float4(rgb, alphaSourceColor.a);
			}
			ENDCG
		}
	}
}