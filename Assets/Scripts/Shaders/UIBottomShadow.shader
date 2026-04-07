Shader "UI/ProceduralBottomShadow"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.35)

        // Offset in pixels. For a shadow below: (0, -8)
        _ShadowOffset ("Shadow Offset (px)", Vector) = (0,-10,0,0)

        // Blur in pixels (softness)
        _Blur ("Blur (px)", Range(0,64)) = 18

        // Spread in pixels (makes the shadow wider/thicker)
        _Spread ("Spread (px)", Range(-64,64)) = 6

        // Controls how much the shadow is restricted to the bottom.
        // Higher = shadow stays lower (less visible towards top)
        _BottomFade ("Bottom Fade (px)", Range(0,128)) = 40

        // Corner radius in pixels. For a pill, set ~ (height/2).
        _Radius ("Corner Radius (px)", Range(0,128)) = 24

        // UI masking support (optional)
        [HideInInspector]_MainTex ("MainTex", 2D) = "white" {}
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _ShadowColor;
            float4 _ShadowOffset; // px
            float _Blur;
            float _Spread;
            float _BottomFade;
            float _Radius;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.texcoord; // Image UV usually 0..1
                return o;
            }

            // SDF for rounded rect (uniform radius)
            float sdRoundRect(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - (halfSize - r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Estimate rect size in pixels from UV derivatives
                float2 duv_dx = ddx(i.uv);
                float2 duv_dy = ddy(i.uv);

                float pxPerUvX = 1.0 / max(1e-5, length(duv_dx));
                float pxPerUvY = 1.0 / max(1e-5, length(duv_dy));

                float2 sizePx = float2(pxPerUvX, pxPerUvY);
                float2 halfPx = sizePx * 0.5;

                // Centered pixel coords
                float2 p = (i.uv - 0.5) * sizePx;

                // Clamp radius so it can't exceed half height/width
                float r = clamp(_Radius, 0.0, min(halfPx.x, halfPx.y));

                // Shadow shape is the rounded rect, offset/spread in px
                float2 pShadow = p - _ShadowOffset.xy;
                float2 halfShadow = max(halfPx + _Spread.xx, 0.0);

                float d = sdRoundRect(pShadow, halfShadow, r);

                // Outside distance only (no shadow inside the shape)
                float outside = max(d, 0.0);

                float blur = max(_Blur, 0.0001);

                // Soft shadow falloff: 1 at edge, fades with distance
                float shadowA = exp(-outside / blur);

                // Bottom-only mask:
                // bottom edge of rect (in pShadow space) is at y = -halfShadow.y
                // keep shadow mainly BELOW that, fade out as it goes up.
                float distAboveBottom = (pShadow.y - (-halfShadow.y)); // >0 means above bottom edge
                float bottomMask = 1.0 - smoothstep(0.0, max(_BottomFade, 0.0001), distAboveBottom);

                shadowA *= bottomMask;

                fixed4 col = fixed4(_ShadowColor.rgb, _ShadowColor.a * shadowA);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                // CanvasGroup alpha etc.
                col *= i.color;

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
