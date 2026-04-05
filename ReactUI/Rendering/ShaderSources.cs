namespace ReactUI.Rendering;

/// <summary>
/// Embedded ShaderLab source for the SDFRect shader.
/// Used by ShaderCache for runtime shader compilation (editor only).
/// In IL2CPP builds, shaders must be pre-compiled in an AssetBundle.
/// </summary>
internal static class SdfRectShaderSource
{
    internal static readonly string Source = @"
Shader ""ReactUI/SDFRect""
{
    Properties
    {
        _RectSize (""Rect Size"", Vector) = (100, 100, 0, 0)
        _RectPos (""Rect Position"", Vector) = (0, 0, 0, 0)
        _Radii (""Border Radii (TL TR BR BL)"", Vector) = (0, 0, 0, 0)
        _BgColor (""Background Color"", Color) = (1, 1, 1, 1)
        _BorderColor (""Border Color"", Color) = (0, 0, 0, 1)
        _BorderWidth (""Border Width"", Float) = 0
        _GradientEnabled (""Gradient Enabled"", Float) = 0
        _GradientAngle (""Gradient Angle (radians)"", Float) = 0
        _GradientColorA (""Gradient Color A"", Color) = (1, 1, 1, 1)
        _GradientColorB (""Gradient Color B"", Color) = (0, 0, 0, 1)
        _GradientRadial (""Gradient Radial"", Float) = 0
        _ShadowEnabled (""Shadow Enabled"", Float) = 0
        _ShadowOffset (""Shadow Offset"", Vector) = (0, 0, 0, 0)
        _ShadowBlur (""Shadow Blur"", Float) = 0
        _ShadowSpread (""Shadow Spread"", Float) = 0
        _ShadowColor (""Shadow Color"", Color) = (0, 0, 0, 0.5)
        _ShadowInset (""Shadow Inset"", Float) = 0
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""IgnoreProjector""=""True"" }
        ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 localPos : TEXCOORD1; float4 color : COLOR; };
            float4 _RectSize, _RectPos, _Radii, _BgColor, _BorderColor;
            float _BorderWidth, _GradientEnabled, _GradientAngle, _GradientRadial;
            float4 _GradientColorA, _GradientColorB;
            float _ShadowEnabled; float4 _ShadowOffset, _ShadowColor;
            float _ShadowBlur, _ShadowSpread, _ShadowInset;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; o.localPos = v.uv * _RectSize.xy; return o; }
            float roundedRectSDF(float2 p, float2 h, float r) { float2 q = abs(p) - h + float2(r,r); return length(max(q,0.0)) + min(max(q.x,q.y),0.0) - r; }
            float selR(float2 p, float4 r) { if(p.x<0&&p.y>0) return r.x; if(p.x>=0&&p.y>0) return r.y; if(p.x>=0&&p.y<=0) return r.z; return r.w; }
            float gaussF(float d, float s) { if(s<=0) return d<=0?1:0; return 1.0-smoothstep(0,s,d); }
            float4 frag(v2f i) : SV_Target {
                float2 hs = _RectSize.xy*0.5; float2 p = i.localPos-hs;
                float r = selR(p,_Radii); float mx = min(hs.x,hs.y); r = min(r,mx);
                float d = roundedRectSDF(p,hs,r); float aa = fwidth(d);
                float4 fc = float4(0,0,0,0);
                if(_ShadowEnabled>0.5&&_ShadowInset<0.5) { float2 sp=p-_ShadowOffset.xy; float sr=selR(sp,_Radii); sr=min(sr,mx); float2 sh=hs+float2(_ShadowSpread,_ShadowSpread); float sd=roundedRectSDF(sp,sh,sr+_ShadowSpread); float sa=gaussF(sd,_ShadowBlur); sa*=1.0-smoothstep(-aa,aa,-d); float4 sc=_ShadowColor; sc.a*=sa; fc=sc; }
                float fa = 1.0-smoothstep(-aa,aa,d); float4 bg=_BgColor;
                if(_GradientEnabled>0.5) { float2 np=i.uv-float2(0.5,0.5); float t; if(_GradientRadial>0.5){t=saturate(length(np)*2);}else{t=saturate(dot(np,float2(cos(_GradientAngle),sin(_GradientAngle)))+0.5);} bg=lerp(_GradientColorA,_GradientColorB,t); }
                bg.a*=fa; fc=float4(lerp(fc.rgb,bg.rgb,bg.a),fc.a*(1-bg.a)+bg.a);
                if(_BorderWidth>0) { float id=roundedRectSDF(p,hs-float2(_BorderWidth,_BorderWidth),max(r-_BorderWidth,0)); float ba=(1-smoothstep(-aa,aa,d))*smoothstep(-aa,aa,id); float4 bc=_BorderColor; bc.a*=ba; fc=float4(lerp(fc.rgb,bc.rgb,bc.a),fc.a*(1-bc.a)+bc.a); }
                if(_ShadowEnabled>0.5&&_ShadowInset>0.5) { float2 ip=p-_ShadowOffset.xy; float ir=selR(ip,_Radii); ir=min(ir,mx); float2 ih=max(hs-float2(_ShadowSpread,_ShadowSpread),float2(0,0)); float id2=roundedRectSDF(ip,ih,max(ir-_ShadowSpread,0)); float ia=gaussF(-id2,_ShadowBlur)*fa; float4 ic=_ShadowColor; ic.a*=ia; fc=float4(lerp(fc.rgb,ic.rgb,ic.a),max(fc.a,ic.a)); }
                return fc;
            }
            ENDCG
        }
    }
    FallBack Off
}";
}

/// <summary>
/// Embedded ShaderLab source for the SDFText shader.
/// </summary>
internal static class SdfTextShaderSource
{
    internal static readonly string Source = @"
Shader ""ReactUI/SDFText""
{
    Properties
    {
        _MainTex (""Font Atlas"", 2D) = ""white"" {}
        _Opacity (""Opacity"", Float) = 1.0
        _SdfThreshold (""SDF Threshold"", Float) = 0.5
        _SdfSoftness (""SDF Softness"", Float) = 0.1
        _OutlineEnabled (""Outline Enabled"", Float) = 0
        _OutlineColor (""Outline Color"", Color) = (0, 0, 0, 1)
        _OutlineWidth (""Outline Width"", Float) = 0.0
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent+1"" ""RenderType""=""Transparent"" ""IgnoreProjector""=""True"" }
        ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float _Opacity, _SdfThreshold, _SdfSoftness, _OutlineEnabled, _OutlineWidth;
            float4 _OutlineColor;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            float4 frag(v2f i) : SV_Target {
                float4 ts = tex2D(_MainTex, i.uv); float sv = ts.a;
                float aa = fwidth(sv); float sf = max(_SdfSoftness, aa);
                float fa = smoothstep(_SdfThreshold-sf, _SdfThreshold+sf, sv);
                float4 fc = i.color; fc.a *= fa * _Opacity;
                if(_OutlineEnabled>0.5&&_OutlineWidth>0) { float ot=_SdfThreshold-_OutlineWidth; float oa=smoothstep(ot-sf,ot+sf,sv); float4 oc=_OutlineColor; oc.a*=oa*_Opacity; float4 r; r.rgb=lerp(oc.rgb,fc.rgb,fa); r.a=oc.a*(1-fc.a)+fc.a; return r; }
                return fc;
            }
            ENDCG
        }
    }
    FallBack Off
}";
}

/// <summary>
/// Embedded ShaderLab source for the Image shader.
/// </summary>
internal static class ImageShaderSource
{
    internal static readonly string Source = @"
Shader ""ReactUI/Image""
{
    Properties
    {
        _MainTex (""Image Texture"", 2D) = ""white"" {}
        _RectSize (""Rect Size"", Vector) = (100, 100, 0, 0)
        _Radii (""Border Radii (TL TR BR BL)"", Vector) = (0, 0, 0, 0)
        _Opacity (""Opacity"", Float) = 1.0
        _Tint (""Tint Color"", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent+1"" ""RenderType""=""Transparent"" ""IgnoreProjector""=""True"" }
        ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 localPos : TEXCOORD1; float4 color : COLOR; };
            sampler2D _MainTex; float4 _RectSize, _Radii, _Tint; float _Opacity;
            float roundedRectSDF(float2 p, float2 h, float r) { float2 q=abs(p)-h+float2(r,r); return length(max(q,0))+min(max(q.x,q.y),0)-r; }
            float selR(float2 p, float4 r) { if(p.x<0&&p.y>0) return r.x; if(p.x>=0&&p.y>0) return r.y; if(p.x>=0&&p.y<=0) return r.z; return r.w; }
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; o.localPos=v.uv*_RectSize.xy; return o; }
            float4 frag(v2f i) : SV_Target {
                float4 tc=tex2D(_MainTex,i.uv)*_Tint*i.color;
                float2 hs=_RectSize.xy*0.5; float2 p=i.localPos-hs;
                float r=selR(p,_Radii); r=min(r,min(hs.x,hs.y));
                float d=roundedRectSDF(p,hs,r); float aa=fwidth(d);
                tc.a *= (1-smoothstep(-aa,aa,d)) * _Opacity;
                return tc;
            }
            ENDCG
        }
    }
    FallBack Off
}";
}

/// <summary>
/// Embedded ShaderLab source for the KawaseBlur shader.
/// </summary>
internal static class KawaseBlurShaderSource
{
    internal static readonly string Source = @"
Shader ""ReactUI/KawaseBlur""
{
    Properties
    {
        _MainTex (""Source Texture"", 2D) = ""white"" {}
        _BlurOffset (""Blur Offset"", Float) = 1.0
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""IgnoreProjector""=""True"" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name ""KAWASE_BLUR""
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _BlurOffset;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float4 frag(v2f i) : SV_Target {
                float2 t=_MainTex_TexelSize.xy*_BlurOffset;
                float4 c=tex2D(_MainTex,i.uv)*4;
                c+=tex2D(_MainTex,i.uv+float2(-t.x,-t.y));
                c+=tex2D(_MainTex,i.uv+float2(t.x,-t.y));
                c+=tex2D(_MainTex,i.uv+float2(-t.x,t.y));
                c+=tex2D(_MainTex,i.uv+float2(t.x,t.y));
                return c/8;
            }
            ENDCG
        }
        Pass
        {
            Name ""KAWASE_COMPOSITE""
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 localPos : TEXCOORD1; float4 color : COLOR; };
            sampler2D _MainTex; float4 _RectSize, _Radii; float _Opacity;
            float roundedRectSDF(float2 p, float2 h, float r) { float2 q=abs(p)-h+float2(r,r); return length(max(q,0))+min(max(q.x,q.y),0)-r; }
            float selR(float2 p, float4 r) { if(p.x<0&&p.y>0) return r.x; if(p.x>=0&&p.y>0) return r.y; if(p.x>=0&&p.y<=0) return r.z; return r.w; }
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; o.localPos=v.uv*_RectSize.xy; return o; }
            float4 frag(v2f i) : SV_Target {
                float4 bc=tex2D(_MainTex,i.uv);
                float2 hs=_RectSize.xy*0.5; float2 p=i.localPos-hs;
                float r=selR(p,_Radii); r=min(r,min(hs.x,hs.y));
                float d=roundedRectSDF(p,hs,r); float aa=fwidth(d);
                bc.a=(1-smoothstep(-aa,aa,d))*_Opacity;
                return bc;
            }
            ENDCG
        }
    }
    FallBack Off
}";
}
