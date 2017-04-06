Shader "Custom/Vertex Colored"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}

	SubShader
	{
		Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		LOD 150

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert alphatest:_Cutoff

		fixed4 _Color;

		struct Input
		{
				float3 vertColor;
		};

		void vert(inout appdata_full v, out Input o)
		{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.vertColor = v.color;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
				fixed4 c = _Color;
				o.Albedo = c.rgb * IN.vertColor;
				o.Alpha = c.a;
		}
		ENDCG
	}
	
	Fallback "Diffuse"
}