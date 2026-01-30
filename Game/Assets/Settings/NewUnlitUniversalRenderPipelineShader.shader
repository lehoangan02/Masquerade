Shader "Custom/URP_BackgroundSample_Simple"
{
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
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

            // 1. Declare the Texture (We still need this)
            TEXTURE2D(_CameraSortingLayerTexture);
            
            // ERROR FIXED: Removed "SAMPLER(sampler_LinearClamp);" 
            // It is already included in Core.hlsl automatically.

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

            float _DistortionStrength;

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
                // 1. Calculate proper screen UV with perspective divide
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                
                // 2. Handle platform differences (some platforms flip Y)
                #if UNITY_UV_STARTS_AT_TOP
                    // screenUV.y = 1.0 - screenUV.y; // Uncomment if image is flipped
                #endif

                // 3. Apply Distortion
                screenUV.x += _DistortionStrength;

                // 4. Sample using the built-in LinearClamp sampler
                half4 background = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_LinearClamp, screenUV);

                return background;
            }
            ENDHLSL
        }
    }
}