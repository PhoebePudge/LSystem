Shader "BumpedSpecularVtxCol" {
    Properties{
        _Color("Main Color", Color) = (1,1,1,1)
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess("Shininess", Range(0.03, 1)) = 0.078125
        _Emission("Emission", float) = 0
        _MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}

        _NormalMap("Normal map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 1)) = 1
    }
    
        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 400

        CGPROGRAM
        #pragma surface surf BlinnPhong


        sampler2D _MainTex;
        sampler2D _NormalMap;
        fixed4 _Color;
        float _Emission;
        half _Shininess;
        float _Amount;

        struct Input {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            half4 color : COLOR0;
			float3 vertexNormal;
        }; 
        void surf(Input IN, inout SurfaceOutput o) {
            //fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
             
            //o.Albedo = tex.rgb * _Color.rgb * IN.color.rgb;

            //o.Gloss = tex.a;

            //o.Alpha = tex.a * _Color.a;

            //o.Specular = _Shininess;

            //o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));

            //o.Emission = (tex.rgb * _Color.rgb * IN.color.rgb) * _Emission;

			o.Albedo = float3(1.0, 0, 0);
        }
        ENDCG
    }

        //FallBack "Specular"
}
