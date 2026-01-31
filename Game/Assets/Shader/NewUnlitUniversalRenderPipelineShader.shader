Shader "Custom/URP_InvertBackground"
{
    Properties
    {
        _BackgroundTex ("Background Render Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(-1, 1)) = -0.3
        _Contrast ("Contrast", Range(0, 3)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_BackgroundTex);
            SAMPLER(sampler_BackgroundTex);
            float4 _BackgroundTex_TexelSize;
            float _Brightness;
            float _Contrast;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION; 
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                half4 background = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, screenUV);
                
                // Invert the colors
                background.rgb = 1.0 - background.rgb;
                
                // Apply contrast (around midpoint 0.5)
                background.rgb = ((background.rgb - 0.5) * _Contrast) + 0.5;
                
                // Apply brightness
                background.rgb += _Brightness;
                
                // Clamp to valid range
                background.rgb = saturate(background.rgb);
                
                return background;
            }
            ENDHLSL
        }
    }
}