Shader "Custom/FogOfWarSimple"
{
    Properties
    {
        _PlayerPos ("Player Position", Vector) = (0, 0, 0, 0)
        _LightRadius ("Light Radius", Float) = 5.0
        _DarknessColor ("Darkness Color", Color) = (0,0,0,0.9)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };
            
            float2 _PlayerPos;
            float _LightRadius;
            half4 _DarknessColor;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Calculate distance from player
                float dist = distance(input.positionWS.xy, _PlayerPos);
                
                // Smooth falloff
                float alpha = smoothstep(_LightRadius * 0.3, _LightRadius, dist);
                
                half4 color = _DarknessColor;
                color.a *= alpha;
                
                return color;
            }
            ENDHLSL
        }
    }
}