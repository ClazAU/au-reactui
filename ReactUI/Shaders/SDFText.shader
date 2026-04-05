Shader "ReactUI/SDFText"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Opacity ("Opacity", Float) = 1.0
        _SdfThreshold ("SDF Threshold", Float) = 0.5
        _SdfSoftness ("SDF Softness", Float) = 0.1
        _OutlineEnabled ("Outline Enabled", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.0
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
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Opacity;
            float _SdfThreshold;
            float _SdfSoftness;
            float _OutlineEnabled;
            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Sample the font atlas
                float4 texSample = tex2D(_MainTex, i.uv);

                // For standard Unity font textures, the alpha channel contains the glyph shape.
                // For true SDF fonts, a single channel (typically R or A) contains the distance field.
                float sdfValue = texSample.a;

                // Compute anti-aliased alpha using screen-space derivatives
                float aaWidth = fwidth(sdfValue);

                // Use softness parameter, falling back to derivative-based AA
                float softness = max(_SdfSoftness, aaWidth);

                // Glyph fill: smooth threshold
                float fillAlpha = smoothstep(_SdfThreshold - softness, _SdfThreshold + softness, sdfValue);

                float4 fillColor = i.color;
                fillColor.a *= fillAlpha * _Opacity;

                // Optional outline
                if (_OutlineEnabled > 0.5 && _OutlineWidth > 0.0)
                {
                    float outlineThreshold = _SdfThreshold - _OutlineWidth;
                    float outlineAlpha = smoothstep(outlineThreshold - softness, outlineThreshold + softness, sdfValue);

                    float4 outlineCol = _OutlineColor;
                    outlineCol.a *= outlineAlpha * _Opacity;

                    // Composite: fill over outline
                    float4 result;
                    result.rgb = lerp(outlineCol.rgb, fillColor.rgb, fillAlpha);
                    result.a = outlineCol.a * (1.0 - fillColor.a) + fillColor.a;
                    return result;
                }

                return fillColor;
            }
            ENDCG
        }
    }

    FallBack Off
}
