sampler uImage0 : register(s0);

texture tileTexture;
sampler tile
{
    Texture = (tileTexture);
};

float2 offset;

float4 MainPS(float2 coords : TEXCOORD0, float4 ocolor : COLOR0) : COLOR0
{
    float2 coordinates = coords + offset;
    float4 finalColor = tex2D(uImage0, coords);
    float4 tileColor = tex2D(tile, coordinates);

    return finalColor *= tileColor.a;
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
};