/*
* Window Shader, lerps between day and night.
*/

Shader "Kappa/Window"
{
    Properties
    {
        _MainTex("Albedo (RGBA)", 2D) = "white" {}
        _SecTex("Sec Albedo (RGB)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Blend("Blend", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        CGPROGRAM
        #pragma surface surf NoLighting noambient

        sampler2D _MainTex;
        sampler2D _SecTex;

        float4 _Color;
        half _Blend;

        struct Input
        {
            float2 uv_MainTex;
        };


        //N64 ain't got time for no lighting
        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            return fixed4(s.Albedo, s.Alpha);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 t1 = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 t2 = tex2D(_SecTex, IN.uv_MainTex) * _Color;

            fixed4 c = lerp(t1, t2, _Blend);

            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }

        ENDCG
    }
}