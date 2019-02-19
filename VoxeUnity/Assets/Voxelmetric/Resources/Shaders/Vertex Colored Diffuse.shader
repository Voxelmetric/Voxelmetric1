Shader "Custom/Vertex Colored Diffuse"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}

	SubShader
	{
		Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		LOD 150

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert alphatest:_Cutoff

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input
		{
				float2 uv_MainTex;
				float3 vertColor;
		};

		void vert(inout appdata_full v, out Input o)
		{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.vertColor = v.color;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb * IN.vertColor;
				o.Alpha = c.a;
		}
		ENDCG
	}
		
	Fallback "Diffuse"
}