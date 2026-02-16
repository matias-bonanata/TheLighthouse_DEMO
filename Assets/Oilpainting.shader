Shader "Custom/Oilpainting"
{
    Properties
    {
        _Radius ("Radius", Range(1, 20)) = 8
        _MainTex ("Screen Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off ZTest Always
        
        Pass
        {
            Name "OilEffect"
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment frag
            #pragma target 3.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float _Radius;
            
            // CUSTOM FULLSCREEN VERTEX - covers entire screen
            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionOS.xy * 2.0 - 1.0, 0.0, 1.0);
                output.uv = input.uv;
                return output;
            }
            
            float luminance(float3 color)
            {
                return dot(color, float3(0.3, 0.59, 0.11));
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 pixelSize = 1.0 / _ScreenParams.xy * 8.0; // OBVIOUS effect
                float4 sum = 0;
                int count = 0;
                
                // 5x5 oil painting kernel
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 offset = float2(x, y) * pixelSize;
                        float4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset);
                        float lum = luminance(sample.rgb);
                        float qLum = floor(lum * _Radius) / _Radius;
                        
                        if (abs(lum - qLum) < 0.1)
                        {
                            sum += sample;
                            count++;
                        }
                    }
                }
                
                return count > 0 ? (sum / count) : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            ENDHLSL
        }
    }
}
