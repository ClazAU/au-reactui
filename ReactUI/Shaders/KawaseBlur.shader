Shader "ReactUI/KawaseBlur"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _BlurOffset ("Blur Offset", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        ZWrite Off
        ZTest Always
        Cull Off

        // Pass 0: Kawase downsample/blur pass
        // Apply this pass iteratively with increasing _BlurOffset for stronger blur
        Pass
        {
            Name "KAWASE_BLUR"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurOffset;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _BlurOffset;

                // Kawase blur: sample the center and four diagonal neighbors
                float4 color = tex2D(_MainTex, i.uv) * 4.0;
                color += tex2D(_MainTex, i.uv + float2(-texel.x, -texel.y));
                color += tex2D(_MainTex, i.uv + float2( texel.x, -texel.y));
                color += tex2D(_MainTex, i.uv + float2(-texel.x,  texel.y));
                color += tex2D(_MainTex, i.uv + float2( texel.x,  texel.y));
                color /= 8.0;

                return color;
            }
            ENDCG
        }

        // Pass 1: Composite pass - draws the blurred texture with alpha blending
        // Used to composite the blurred backdrop behind a UI element
        Pass
        {
            Name "KAWASE_COMPOSITE"

            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _RectSize;
            float4 _Radii;
            float _Opacity;

            float roundedRectSDF(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - halfSize + float2(r, r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            float selectRadius(float2 p, float4 radii)
            {
                if (p.x < 0.0 && p.y > 0.0) return radii.x;
                if (p.x >= 0.0 && p.y > 0.0) return radii.y;
                if (p.x >= 0.0 && p.y <= 0.0) return radii.z;
                return radii.w;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.localPos = v.uv * _RectSize.xy;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 blurColor = tex2D(_MainTex, i.uv);

                // Apply rounded rect mask
                float2 halfSize = _RectSize.xy * 0.5;
                float2 p = i.localPos - halfSize;
                float r = selectRadius(p, _Radii);
                float maxR = min(halfSize.x, halfSize.y);
                r = min(r, maxR);

                float dist = roundedRectSDF(p, halfSize, r);
                float aaWidth = fwidth(dist);
                float mask = 1.0 - smoothstep(-aaWidth, aaWidth, dist);

                blurColor.a = mask * _Opacity;
                return blurColor;
            }
            ENDCG
        }
    }

    FallBack Off
}
