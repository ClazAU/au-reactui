Shader "ReactUI/Image"
{
    Properties
    {
        _MainTex ("Image Texture", 2D) = "white" {}
        _RectSize ("Rect Size", Vector) = (100, 100, 0, 0)
        _Radii ("Border Radii (TL TR BR BL)", Vector) = (0, 0, 0, 0)
        _Opacity ("Opacity", Float) = 1.0
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
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
            float4 _Tint;

            // SDF for a rounded rectangle centered at origin
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
                // Sample the image texture
                float4 texColor = tex2D(_MainTex, i.uv);
                texColor *= _Tint;
                texColor *= i.color;

                // Apply rounded rectangle mask via SDF
                float2 halfSize = _RectSize.xy * 0.5;
                float2 p = i.localPos - halfSize;

                float r = selectRadius(p, _Radii);
                float maxR = min(halfSize.x, halfSize.y);
                r = min(r, maxR);

                float dist = roundedRectSDF(p, halfSize, r);
                float aaWidth = fwidth(dist);
                float mask = 1.0 - smoothstep(-aaWidth, aaWidth, dist);

                texColor.a *= mask * _Opacity;

                return texColor;
            }
            ENDCG
        }
    }

    FallBack Off
}
