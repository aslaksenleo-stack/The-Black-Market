// Simple stylised glass for jars, lantern panes, orbs and display cases.
// Mostly transparent, brighter at glancing angles (rim) with one soft fake highlight, so glass reads
// clearly without real reflections. Vertex colour tints it (e.g. green-tinted bottle glass).
Shader "BlackMarket/Glass"
{
    Properties
    {
        _Color ("Tint (alpha = opacity)", Color) = (0.75,0.88,0.92,0.18)
        _RimColor ("Rim (alpha = strength)", Color) = (1,0.95,0.85,0.55)
        _RimPower ("Rim Power", Range(0.5,8)) = 2.5
        _Highlight ("Highlight", Range(0,1)) = 0.55
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _RimColor;
            half _RimPower;
            half _Highlight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = UnityWorldSpaceViewDir(worldPos);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);
                half rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimColor.a;
                float3 h = normalize(normalize(float3(-0.35, 1.0, 0.25)) + v);
                half spec = smoothstep(0.92, 0.97, saturate(dot(n, h))) * _Highlight;

                fixed4 vc = i.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                vc.rgb = GammaToLinearSpace(vc.rgb);
                #endif
                fixed4 c = _Color * vc;
                c.rgb += _RimColor.rgb * rim + spec;
                c.a = saturate(c.a + rim * 0.6 + spec);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }

    FallBack Off
}
