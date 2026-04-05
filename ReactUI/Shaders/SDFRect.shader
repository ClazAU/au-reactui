Shader "ReactUI/SDFRect"
{
    Properties
    {
        _RectSize ("Rect Size", Vector) = (100, 100, 0, 0)
        _RectPos ("Rect Position", Vector) = (0, 0, 0, 0)
        _Radii ("Border Radii (TL TR BR BL)", Vector) = (0, 0, 0, 0)
        _BgColor ("Background Color", Color) = (1, 1, 1, 1)
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        _BorderWidth ("Border Width", Float) = 0

        _GradientEnabled ("Gradient Enabled", Float) = 0
        _GradientAngle ("Gradient Angle (radians)", Float) = 0
        _GradientColorA ("Gradient Color A", Color) = (1, 1, 1, 1)
        _GradientColorB ("Gradient Color B", Color) = (0, 0, 0, 1)
        _GradientRadial ("Gradient Radial", Float) = 0

        _ShadowEnabled ("Shadow Enabled", Float) = 0
        _ShadowOffset ("Shadow Offset", Vector) = (0, 0, 0, 0)
        _ShadowBlur ("Shadow Blur", Float) = 0
        _ShadowSpread ("Shadow Spread", Float) = 0
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowInset ("Shadow Inset", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

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

            float4 _RectSize;
            float4 _RectPos;
            float4 _Radii;
            float4 _BgColor;
            float4 _BorderColor;
            float _BorderWidth;

            float _GradientEnabled;
            float _GradientAngle;
            float4 _GradientColorA;
            float4 _GradientColorB;
            float _GradientRadial;

            float _ShadowEnabled;
            float4 _ShadowOffset;
            float _ShadowBlur;
            float _ShadowSpread;
            float4 _ShadowColor;
            float _ShadowInset;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                // Map UV to local pixel coordinates within the rect
                o.localPos = v.uv * _RectSize.xy;
                return o;
            }

            // SDF for a rounded rectangle centered at origin
            // p: point relative to rect center
            // halfSize: half the rect dimensions
            // r: corner radius (selected per-quadrant)
            float roundedRectSDF(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - halfSize + float2(r, r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // Select the correct corner radius based on the quadrant
            float selectRadius(float2 p, float4 radii)
            {
                // radii = (TL, TR, BR, BL)
                if (p.x < 0.0 && p.y > 0.0) return radii.x; // Top-left
                if (p.x >= 0.0 && p.y > 0.0) return radii.y; // Top-right
                if (p.x >= 0.0 && p.y <= 0.0) return radii.z; // Bottom-right
                return radii.w;                                 // Bottom-left
            }

            // Approximate gaussian for shadow falloff
            float gaussianFalloff(float d, float sigma)
            {
                if (sigma <= 0.0) return d <= 0.0 ? 1.0 : 0.0;
                return 1.0 - smoothstep(0.0, sigma, d);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 rectSize = _RectSize.xy;
                float2 halfSize = rectSize * 0.5;

                // Local position relative to rect center (UV 0-1 mapped to rect pixels)
                float2 p = i.localPos - halfSize;

                // Select corner radius based on quadrant
                float r = selectRadius(p, _Radii);
                // Clamp radius to half the smallest dimension
                float maxR = min(halfSize.x, halfSize.y);
                r = min(r, maxR);

                // Base SDF distance
                float dist = roundedRectSDF(p, halfSize, r);

                // Anti-aliasing width based on screen-space derivatives
                float aaWidth = fwidth(dist);

                float4 finalColor = float4(0, 0, 0, 0);

                // --- Box shadow (outer) ---
                if (_ShadowEnabled > 0.5 && _ShadowInset < 0.5)
                {
                    float2 shadowP = p - _ShadowOffset.xy;
                    float shadowR = selectRadius(shadowP, _Radii);
                    shadowR = min(shadowR, maxR);

                    // Expand rect for spread
                    float2 shadowHalf = halfSize + float2(_ShadowSpread, _ShadowSpread);
                    float shadowDist = roundedRectSDF(shadowP, shadowHalf, shadowR + _ShadowSpread);

                    float shadowAlpha = gaussianFalloff(shadowDist, _ShadowBlur);
                    // Don't draw shadow inside the rect
                    float insideMask = 1.0 - smoothstep(-aaWidth, aaWidth, -dist);
                    shadowAlpha *= insideMask;

                    float4 shadow = _ShadowColor;
                    shadow.a *= shadowAlpha;
                    finalColor = shadow;
                }

                // --- Background fill ---
                float fillAlpha = 1.0 - smoothstep(-aaWidth, aaWidth, dist);

                float4 bgColor = _BgColor;

                // Gradient
                if (_GradientEnabled > 0.5)
                {
                    float2 nPos = i.uv - float2(0.5, 0.5);
                    float t;

                    if (_GradientRadial > 0.5)
                    {
                        t = length(nPos) * 2.0;
                        t = saturate(t);
                    }
                    else
                    {
                        float2 dir = float2(cos(_GradientAngle), sin(_GradientAngle));
                        t = dot(nPos, dir) + 0.5;
                        t = saturate(t);
                    }

                    bgColor = lerp(_GradientColorA, _GradientColorB, t);
                }

                bgColor.a *= fillAlpha;

                // Composite background over shadow
                finalColor = float4(
                    lerp(finalColor.rgb, bgColor.rgb, bgColor.a),
                    finalColor.a * (1.0 - bgColor.a) + bgColor.a
                );

                // --- Border ---
                if (_BorderWidth > 0.0)
                {
                    float innerDist = roundedRectSDF(p, halfSize - float2(_BorderWidth, _BorderWidth),
                                                      max(r - _BorderWidth, 0.0));
                    float borderAlpha = smoothstep(-aaWidth, aaWidth, -dist)  // outside edge: 0
                                      * (1.0 - smoothstep(-aaWidth, aaWidth, -innerDist)); // inside edge of border

                    // Correct: border is where dist < 0 (inside outer) and innerDist > 0 (outside inner)
                    borderAlpha = (1.0 - smoothstep(-aaWidth, aaWidth, dist))
                                * smoothstep(-aaWidth, aaWidth, innerDist);

                    float4 border = _BorderColor;
                    border.a *= borderAlpha;

                    finalColor = float4(
                        lerp(finalColor.rgb, border.rgb, border.a),
                        finalColor.a * (1.0 - border.a) + border.a
                    );
                }

                // --- Inset shadow ---
                if (_ShadowEnabled > 0.5 && _ShadowInset > 0.5)
                {
                    float2 insetP = p - _ShadowOffset.xy;
                    float insetR = selectRadius(insetP, _Radii);
                    insetR = min(insetR, maxR);

                    float2 insetHalf = halfSize - float2(_ShadowSpread, _ShadowSpread);
                    insetHalf = max(insetHalf, float2(0, 0));
                    float insetDist = roundedRectSDF(insetP, insetHalf, max(insetR - _ShadowSpread, 0.0));

                    float insetAlpha = gaussianFalloff(-insetDist, _ShadowBlur);
                    // Only inside the rect
                    insetAlpha *= fillAlpha;

                    float4 insetColor = _ShadowColor;
                    insetColor.a *= insetAlpha;

                    finalColor = float4(
                        lerp(finalColor.rgb, insetColor.rgb, insetColor.a),
                        max(finalColor.a, insetColor.a)
                    );
                }

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack Off
}
