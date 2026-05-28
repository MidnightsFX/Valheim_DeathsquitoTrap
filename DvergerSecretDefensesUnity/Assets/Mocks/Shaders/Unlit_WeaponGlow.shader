Shader "JVLmock_Unlit/WeaponGlow" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_CurlTex ("Curl Noise", 3D) = "white" {}
		_CurlSize ("Curl Size", Float) = 1
		_CurlStrength ("Curl Strength", Range(0, 0.5)) = 0.05
		_VertPush ("Vertex Push", Range(0, 0.1)) = 0.05
		_ScrollSpeed ("Scroll Speed Y", Range(0, 1)) = 0.25
		_ScrollSpeedX ("Scroll Speed X", Range(0, 1)) = 0.05
		_PixelSize ("Pixel Size", Float) = 64
		_TexScale ("Texture Scale", Float) = 1
		[HDR] _Color ("Color", Vector) = (1,1,1,1)
		[MaterialToggle] _DomainWarp ("Domain Warp", Float) = 0
		_EdgeFading ("Edge Fades", Vector) = (0,1,0,1)
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}