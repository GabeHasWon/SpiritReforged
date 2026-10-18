sampler uImage0 : register(s0);

texture distortionTexture;
sampler distortionSampler = sampler_state
{
    Texture = (distortionTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float2 dimensions;
float4 gradientSource;
float4 outlineColor;
float2 displacement;
float2 distortionStrength;
float fadeStrength;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

float4 GhostShader(VertexShaderOutput input) : COLOR0
{
    float4 warpColor = tex2D(distortionSampler, input.TextureCoordinates + displacement);
    float2 coords = input.TextureCoordinates + (distortionStrength * warpColor.gr);
    float4 color = tex2D(uImage0, coords);

    float2 frameUv = (coords * dimensions - gradientSource.xy) / gradientSource.zw;
    
    frameUv.x -= frameUv.x % (1 / dimensions.x);
    frameUv.y -= frameUv.y % (1 / dimensions.y);
    
    float gradientOpacity = 1.0 - (frameUv.y * fadeStrength);
    float2 pixelCoords = float2(2.0 / dimensions.x, 2.0 / dimensions.y);
    float4 outline = outlineColor * color.a * gradientOpacity;

    if (tex2D(uImage0, coords + float2(pixelCoords.x, 0)).a == 0.0)
        return outline;
    if (tex2D(uImage0, coords - float2(pixelCoords.x, 0)).a == 0.0)
        return outline;
    if (tex2D(uImage0, coords + float2(0, pixelCoords.y)).a == 0.0)
        return outline;
    if (tex2D(uImage0, coords - float2(0, pixelCoords.y)).a == 0.0)
        return outline;

    color.rgb = (color.r + color.g + color.b) / 3;
    return color * input.Color * gradientOpacity;
}

technique BasicColorDrawing
{
    pass GhostShaderPass
    {
        PixelShader = compile ps_2_0 GhostShader();
    }
};