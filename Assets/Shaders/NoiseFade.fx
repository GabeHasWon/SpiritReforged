sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

texture noiseTexture;
sampler noise
{
    Texture = (noiseTexture);
};

float power;
float2 size;
float2 offset;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

float4 MainPS(VertexShaderInput input) : COLOR0
{
    float4 texColor = tex2D(uImage0, input.TextureCoordinates);

    float4 noiseColor = tex2D(noise, float2((input.TextureCoordinates.x * size.x + offset.x) % 1, (input.TextureCoordinates.y * size.y + offset.y) % 1));
    float4 noiseStrength = noiseColor.r;
    noiseStrength = pow(noiseStrength, power);
    
    if (noiseStrength.r > 0 || texColor.r == 0)
        return float4(0, 0, 0, 0);

    return input.Color * (1 - noiseStrength);
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
};