sampler uImage0 : register(s0);
float2 uImageSize;
float uPixelSize;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float2 gridSize = uImageSize / uPixelSize;
    
    float2 pixelatedCoords = floor(coords * gridSize) / gridSize;
    float4 finalColor = tex2D(uImage0, pixelatedCoords);

    return finalColor * color;
}

technique Technique1
{
    pass P0
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}