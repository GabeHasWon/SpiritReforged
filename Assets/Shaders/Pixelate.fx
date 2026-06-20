sampler uImage0 : register(s0);

texture uImage1; // Distortion noise texture
sampler2D uImage1Sampler = sampler_state
{
    texture = <uImage1>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float2 uImageSize;
float uPixelSize;
float uTime;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float2 gridSize = uImageSize / uPixelSize;
    float2 pixelatedCoords = floor(coords * gridSize) / gridSize;

    float4 target = tex2D(uImage0, pixelatedCoords);
    float smoothBrightness = max(target.r, max(target.g, target.b));
    float strength = lerp(0.005, 0.0005, smoothBrightness);

    float2 noiseCoords = pixelatedCoords + float2(uTime * 1.5, uTime * -0.8);
    float4 noise = tex2D(uImage1Sampler, noiseCoords);
    float2 offset = (noise.rg - 0.5) * 2.0;
    float2 distortionCoords = pixelatedCoords + (offset * strength);

    float4 finalTex = tex2D(uImage0, distortionCoords);
    float finalBrightness = max(finalTex.r, max(finalTex.g, finalTex.b));
    
    float blurRadius = 4.0;
    float2 texel = (uPixelSize / 4) / uImageSize;
    
    float4 bloom = tex2D(uImage0, distortionCoords + float2(texel.x * blurRadius, 0)) +
                   tex2D(uImage0, distortionCoords - float2(texel.x * blurRadius, 0)) +
                   tex2D(uImage0, distortionCoords + float2(0, texel.y * blurRadius)) +
                   tex2D(uImage0, distortionCoords - float2(0, texel.y * blurRadius));
                   
    bloom = (bloom * 0.1) * 0.4;
    
    if (finalBrightness <= 0.01 || finalBrightness <= noise.r * 0.5)
    {
        if (max(bloom.r, max(bloom.g, bloom.b)) > 0.01)
            return bloom;
        
        return float4(0, 0, 0, 0);  
    }
        
    finalTex.a = 1;

    return finalTex * 1.25 + bloom;
}

technique Technique1
{
    pass P0
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}