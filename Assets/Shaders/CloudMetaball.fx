sampler2D uImage0 : register(s0);

float4 primaryColor;
float4 secondaryColor;
int numColors;

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

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 imageColor = tex2D(uImage0, coords);
    float4 finalColor = lerp(secondaryColor, primaryColor, round(imageColor.a * numColors) / numColors);
    
    if (imageColor.a == 0)
        finalColor *= 0;
    
    return finalColor;
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}