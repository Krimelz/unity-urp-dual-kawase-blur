Shader "Hidden/Blur"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurOffset;
        ENDHLSL
        
        Pass
        {
            Name "Downsample"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_TARGET
            {
                float4 offset = _BlitTexture_TexelSize.xyxy * float4(-_BlurOffset, -_BlurOffset, _BlurOffset, _BlurOffset);
                
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord) * 4.0;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.xy);
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.xw);
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.zy);
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.zw);

                return color * 0.125;
            }
            
            ENDHLSL
        }
        
        Pass
        {
            Name "Upsample"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_TARGET
            {
                float4 offset = _BlitTexture_TexelSize.xyxy * float4(-_BlurOffset, -_BlurOffset, _BlurOffset, _BlurOffset);
                
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(offset.x, 0));
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(offset.z, 0));
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(0, offset.y));
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(0, offset.w));
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.xy * 0.5) * 2.0;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.xw * 0.5) * 2.0;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.zy * 0.5) * 2.0;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset.zw * 0.5) * 2.0;
                
                return color * 0.0833;
            }
            
            ENDHLSL
        }
    }
}