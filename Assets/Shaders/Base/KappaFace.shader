Shader "Kappa/Face"
{
    Properties
    {
        _MainTex("Albedo (RGBA)", 2D) = "white" {}
		_Cutoff  ("Cutoff", Range(0, 1)) = 0.5
		_Color("Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2

    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Cull[_Cull]
        Lighting Off

        CGPROGRAM
        #pragma surface surf NoLighting noambient

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
		float _Cutoff;
		float4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        // based on https://www.shadertoy.com/view/wdy3RW
        // with proper support for mip maps and textures that aren't using point filtering
        fixed4 N64Filtering(sampler2D tex, float2 uv, float4 texelSize)
        {
            // texel coordinates
            float2 texels = uv * texelSize.zw;

            // calculate mip level
            float2 dx = ddx(texels);
            float2 dy = ddy(texels);
            float delta_max_sqr = max(dot(dx, dx), dot(dy, dy));
            float mip = max(0.0, 0.5 * log2(delta_max_sqr));

            // scale texel sizes and texel coordinates to handle mip levels properly
            float scale = pow(2,floor(mip));
            texelSize.xy *= scale;
            texelSize.zw /= scale;
            texels = texels / scale - 0.5;

            // calculate blend for the three points of the tri-filter
            float2 fracTexels = frac(texels);
            float3 blend = float3(
                abs(fracTexels.x + fracTexels.y - 1),
                min(abs(fracTexels.xx - float2(0,1)), abs(fracTexels.yy - float2(1,0)))
            );

            // calculate equivalents of point filtered uvs for the three points
            float2 uvA = (floor(texels + fracTexels.yx) + 0.5) * texelSize.xy;
            float2 uvB = (floor(texels) + float2(1.5, 0.5)) * texelSize.xy;
            float2 uvC = (floor(texels) + float2(0.5, 1.5)) * texelSize.xy;

            // sample points
            fixed4 A = tex2Dlod(tex, float4(uvA, 0, mip));
            fixed4 B = tex2Dlod(tex, float4(uvB, 0, mip));
            fixed4 C = tex2Dlod(tex, float4(uvC, 0, mip));

            // blend and return
            return A * blend.x + B * blend.y + C * blend.z;
        }

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) 
        {
            return fixed4(s.Albedo, s.Alpha);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = N64Filtering(_MainTex, IN.uv_MainTex, _MainTex_TexelSize);
            clip(c.a - _Cutoff);
			o.Albedo = c.rgb * _Color.rgb;
        }
        ENDCG
    }
}