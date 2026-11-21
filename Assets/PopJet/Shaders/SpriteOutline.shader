// Viền mực chung cho bóng và vật tương tác (GDD §9.6 điều 3): viền #2B2B3A dày khoảng 3 px ở màn rộng 1080 px.
// Độ dày tính theo pixel màn hình (dùng fwidth của UV), nên vật to hay nhỏ, ảnh 512 hay 256 px đều có viền như nhau.
// Viền lấy mẫu alpha 12 hướng quanh mỗi điểm; ảnh cần lề trong suốt (art AI có lề 3%) và mesh Full Rect.
// Nằm trong atlas thì padding atlas phải lớn hơn độ dày viền tính theo texel.
Shader "PopJet/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _OutlineColor ("Màu viền", Color) = (0.169, 0.169, 0.227, 1)
        _OutlinePixels ("Độ dày viền (px ở màn rộng 1080)", Range(0, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlinePixels;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                // UV đi được trong 1 pixel màn hình, đổi về màn tham chiếu rộng 1080 px.
                float2 step = fwidth(i.uv) * _OutlinePixels * (_ScreenParams.x / 1080.0);
                half a = 0;
                [unroll]
                for (int k = 0; k < 12; k++)
                {
                    float ang = k * 0.52359878;
                    a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(cos(ang), sin(ang)) * step).a);
                }
                half oa = a * _OutlineColor.a * i.color.a;
                // Hình nằm trên viền (alpha nhân sẵn).
                half3 rgb = c.rgb * c.a + _OutlineColor.rgb * oa * (1 - c.a);
                half alpha = c.a + oa * (1 - c.a);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
