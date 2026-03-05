sampler uImage0 : register(s0);

matrix WorldViewProjection;
float totalHeight;

texture normalTexture;
sampler normal
{
    Texture = (normalTexture);
};

texture tileTexture;
sampler tile
{
    Texture = (tileTexture);
};

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

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;

    output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 mapColor = tex2D(normal, coords);
    float4 tileColor = tex2D(tile, coords);
    
    mapColor *= tileColor.a; //Strictly adhere to tile bounds

    float reflectionY = (1 - ((mapColor.r + mapColor.g + mapColor.b) / 3)) / totalHeight;
    float4 reflectedColor = tex2D(uImage0, coords - float2(0, reflectionY));

    return reflectedColor * input.Color * mapColor.w;
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
};