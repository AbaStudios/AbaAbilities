sampler uImage0 : register(s0); // Screen Texture
sampler uImage1 : register(s1); // Distortion Map (The RenderTarget with trails)

float intensity;
float2 uScreenSize;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 distortionColor = tex2D(uImage1, coords);
    
    // Use the red channel (or alpha) of the distortion map to determine offset magnitude
    float distortionValue = distortionColor.r * distortionColor.a;
    
    if (distortionValue <= 0.001)
    {
        return tex2D(uImage0, coords);
    }
    
    // Calculate offset
    // We assume the distortion map encodes direction or just simple radial/noise magnitude
    // For simple trails, we can just offset by a small amount based on value
    
    // A better approach for trails might be to encode direction in RG, but for now let's do a simple displacement
    // taking neighboring pixels to calculate a 'normal' like effect or just simple noise-based displacement if encoded
    
    // Simplified Logic from AirDistortEffect:
    float2 offset = float2(distortionValue, distortionValue) * intensity * 0.01;
    
    // Sample screen with offset
    return tex2D(uImage0, coords + offset);
}

technique Technique1
{
    pass P0
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
