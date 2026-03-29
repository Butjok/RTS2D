Shader "Unlit/UnlitWithCircularProgressBarOverlay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TintColor ("Tint Color", Color) = (1,1,1,1)
		_Progress ("Progress", Range(0,1)) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 

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
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _TintColor;
			float4 _MainTex_ST;
			float _Progress;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv2 = v.uv2;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 textureColor = tex2D(_MainTex, i.uv);

				i.uv2 -= .5;
				i.uv2 *= 2;

				float currentAngle = atan2(i.uv2.y, i.uv2.x);
				if (currentAngle < 0)
					currentAngle += 2 * UNITY_PI;
				float progress = _Progress;
				float progressAngle = progress * 2 * UNITY_PI;
				float alpha = smoothstep(progressAngle, progressAngle + .1, currentAngle);
				float4 tint = lerp(float4(1,1,1,1), _TintColor, alpha);
				
				return textureColor * tint;
			}
			ENDCG
		}
	}
}