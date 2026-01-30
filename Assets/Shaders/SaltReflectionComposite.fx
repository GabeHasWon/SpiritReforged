sampler uImage0 : register(s0);

matrix WorldViewProjection;
float totalHeight;
float backgroundSeethroughOpacity;

texture normalTexture;
sampler normal
{
    Texture = (normalTexture);
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = clamp;
    AddressV = clamp;
};

texture backgroundComposite;
sampler bgComposite
{
    Texture = (backgroundComposite);
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = clamp;
    AddressV = clamp;
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

float4 alphaBlend(float4 bottomLayer, float4 topLayer)
{
    float4 returnColor = bottomLayer * (1 - topLayer.a) + topLayer * topLayer.a;
    returnColor.a = saturate(bottomLayer.a + topLayer.a);
    return returnColor;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 mapColor = tex2D(normal, coords);
    float4 tileMask = tex2D(tile, coords);
    mapColor *= tileMask.a; //Strictly adhere to tile bounds

    float reflectionY = (1 - ((mapColor.r + mapColor.g + mapColor.b) / 3)) / totalHeight;
    float4 reflectedColor = tex2D(uImage0, coords - float2(0, reflectionY));
    float4 seethroughBackgroundColor = tex2D(bgComposite, coords);
    
    reflectedColor = alphaBlend(seethroughBackgroundColor, reflectedColor);
    
    return reflectedColor * input.Color * mapColor.a;
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};