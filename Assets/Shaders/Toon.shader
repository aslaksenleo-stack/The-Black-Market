// The game's main surface shader: stylised "toon" lighting for the built-in render pipeline.
//
// - Light falls off in a soft band instead of a smooth gradient, giving the painted, cartoon look.
// - The shadowed side keeps a tinted share of the light (_ShadowColor) so forms read softly, never pitch black.
// - Vertex colour RGB tints the surface, so one material can colour a whole character or prop.
// - Vertex colour ALPHA marks glowing parts: alpha 1 = normal, alpha 0 = fully self-lit (lantern flames, gems).
// - Vertex colours are authored in sRGB and converted when the project uses Linear colour space.
// - Optional rim light (_RimColor alpha > 0) helps characters stand out from the background.
Shader "BlackMarket/Toon"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _TexStrength ("Texture Strength", Range(0,1)) = 1
        _ShadowColor ("Shadow Color", Color) = (0.42,0.33,0.40,1)
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.48
        _RampSmooth ("Ramp Smoothness", Range(0.01,0.5)) = 0.09
        _RimColor ("Rim Color (alpha = strength)", Color) = (1,0.82,0.6,0)
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _EmissionBoost ("Glow Strength", Range(0,4)) = 1.4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _TexStrength;
        fixed4 _ShadowColor;
        half _RampThreshold;
        half _RampSmooth;
        fixed4 _RimColor;
        half _RimPower;
        half _EmissionBoost;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float4 color : COLOR;
        };

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir) * 0.5 + 0.5;
            half ramp = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndl);
            half3 shade = lerp(_ShadowColor.rgb, half3(1,1,1), ramp);
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * shade * atten;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed3 texColor = lerp(fixed3(1,1,1), tex.rgb, _TexStrength);

            // Vertex colours are authored in sRGB; Unity passes them through unconverted.
            half3 vertexColor = IN.color.rgb;
            #ifndef UNITY_COLORSPACE_GAMMA
            vertexColor = GammaToLinearSpace(vertexColor);
            #endif

            fixed3 albedo = texColor * _Color.rgb * vertexColor;

            half glow = saturate(1.0 - IN.color.a);
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _RimPower);

            o.Albedo = albedo * (1.0 - glow * 0.5);
            o.Emission = albedo * glow * _EmissionBoost + _RimColor.rgb * (_RimColor.a * rim);
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
