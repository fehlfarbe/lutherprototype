Shader "Custom/blendTexture" {
Properties {
	_Blend ("Blend", Range (0, 1) ) = 0.5
	_Blend2 ("Alpha Blend", Range (0, 1) ) = 0.0
	_Color ("Main Color", Color) = (1,1,1,0)
	_MainTex ("Texture 1 Color (RGB) Alpha (A)", 2D) = "white" {}
	_Texture2 ("Texture 2 Color (RGB) Alpha (A)", 2D) = ""
	_Texture3 ("Blank Color (RGB) Alpha (A)", 2D) = ""
	//_BumpMap ("Normalmap", 2D) = "bump" {}
}

SubShader {
	Blend SrcAlpha OneMinusSrcAlpha
	Tags { "Queue"="Transparent" "RenderType"="Transparent" }
	LOD 300
	Pass {
		SetTexture[_MainTex]
		SetTexture[_Texture2] { 
			ConstantColor (0,0,0, [_Blend])
			Combine texture Lerp(constant) previous
		}
		SetTexture[_Texture3] { 
			ConstantColor (0,0,0, [_Blend2])
			Combine texture Lerp(constant) previous
		}
	}

	CGPROGRAM
	#pragma surface surf Lambert alpha
	
	sampler2D _MainTex;
	//sampler2D _BumpMap;
	fixed4 _Color;
	sampler2D _Texture2;
	sampler2D _Texture3;
	float _Blend;
	float _Blend2;
	
	struct Input {
		float2 uv_MainTex;
		//float2 uv_BumpMap;
		float2 uv_Texture2;
		float2 uv_Texture3;
		
	};
	
	void surf (Input IN, inout SurfaceOutput o) {
		fixed4 t1 = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		fixed4 t2 = tex2D (_Texture2, IN.uv_MainTex) * _Color;
		
		fixed4 a1 = tex2D(_MainTex, IN.uv_MainTex).a;
		fixed4 a2 = tex2D(_Texture2, IN.uv_MainTex).a;
		fixed4 a3 = tex2D(_Texture3, IN.uv_MainTex).a;
				
		o.Albedo = lerp(t1, t2, _Blend);
		o.Alpha = lerp(lerp(a1, a2, _Blend), a3, _Blend2);
		//o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	}
	ENDCG  
	}
	
	FallBack "Diffuse"
}