Shader "Custom/CardGlow"
{
    Properties
    {
        [Header(Shape)]
        _GlowColor ("Glow Color", Color) = (1.0, 0.85, 0.2, 1.0)
        _CoreColor ("Core Color", Color) = (1.0, 1.0, 0.8, 1.0)
        _Intensity ("Intensity", Range(0, 5)) = 2.0
        _CoreSize ("Core Size", Range(0, 1)) = 0.3
        _FalloffPower ("Falloff Power", Range(0.5, 8)) = 2.5
        _AspectRatio ("Aspect Ratio (W/H)", Range(0.3, 3)) = 0.7

        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.15
        _PulseMin ("Pulse Min Intensity", Range(0, 1)) = 0.85

        [Header(Edge)]
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.5
        _OuterRadius ("Outer Radius", Range(0.1, 1.5)) = 0.95

        [Header(Noise Optional)]
        _NoiseScale ("Noise Scale", Range(0, 20)) = 5.0
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.08
        _NoiseSpeed ("Noise Speed", Range(0, 3)) = 0.8

        [Header(Sprite)]
        _MainTex ("Sprite Texture (optional)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // --- KEY: Additive blending so the glow naturally composites ---
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Properties
            fixed4 _GlowColor;
            fixed4 _CoreColor;
            half _Intensity;
            half _CoreSize;
            half _FalloffPower;
            half _AspectRatio;

            half _PulseSpeed;
            half _PulseAmount;
            half _PulseMin;

            half _EdgeSoftness;
            half _OuterRadius;

            half _NoiseScale;
            half _NoiseStrength;
            half _NoiseSpeed;

            sampler2D _MainTex;

            // Simple hash-based noise (no texture dependency)
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Center UV so (0,0) is the middle of the quad
                float2 centeredUV = i.uv - 0.5;

                // Apply aspect ratio to make elliptical glow (cards are taller than wide)
                centeredUV.x *= (1.0 / _AspectRatio);

                // Distance from center
                float dist = length(centeredUV);

                // --- Pulse animation ---
                float pulse = lerp(_PulseMin, 1.0,
                    (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * _PulseAmount
                    + (1.0 - _PulseAmount));

                // --- Optional noise for organic feel ---
                float2 noiseUV = centeredUV * _NoiseScale + _Time.y * _NoiseSpeed;
                float noise = valueNoise(noiseUV) * _NoiseStrength;

                // --- Glow falloff ---
                // Outer edge: smooth fade to zero
                float outerMask = 1.0 - smoothstep(_OuterRadius - _EdgeSoftness, _OuterRadius, dist + noise);

                // Core-to-edge gradient
                float gradient = 1.0 - saturate(dist / _OuterRadius);
                gradient = pow(gradient, _FalloffPower);

                // Bright core
                float coreMask = 1.0 - smoothstep(0.0, _CoreSize, dist);
                coreMask = pow(coreMask, 1.5);

                // --- Color: blend from glow color (outer) to core color (center) ---
                fixed4 glowCol = lerp(_GlowColor, _CoreColor, coreMask);

                // --- Final composite ---
                float alpha = (gradient + coreMask * 0.5) * outerMask * pulse * _Intensity;

                // Multiply by vertex color (useful for SpriteRenderer tinting)
                fixed4 result = glowCol * alpha * i.color;

                // Optional: multiply by sprite texture alpha if using a shaped mask
                fixed4 texCol = tex2D(_MainTex, i.uv);
                result *= texCol.a;

                return result;
            }
            ENDCG
        }
    }

    FallBack Off
}
