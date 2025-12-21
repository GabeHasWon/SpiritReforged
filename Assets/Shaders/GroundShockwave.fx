sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
matrix WorldViewProjection;

texture uTexture;
sampler textureSampler = sampler_state
{
    Texture = (uTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float2 textureStretch;
float2 pixelDimensions;

float4 uColor;
float4 uColor2;
float4 uColor3;
float finalIntensityMod;
int numColors;

float2 scroll;
float progress;


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
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;

	output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

const float verticalFade = 0.1f;
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 baseCoords = input.TextureCoordinates;
    baseCoords = round(baseCoords * pixelDimensions) / pixelDimensions;
    
    float2 texCoordsA = float2(baseCoords.x + scroll.x, baseCoords.y + scroll.y) * textureStretch;
    float2 texCoordsB = float2(baseCoords.x + scroll.x, baseCoords.y + scroll.y / 2) * textureStretch / 2;
    
    //combine 2 different samplings of noise
    float strength = (tex2D(textureSampler, texCoordsA).r + tex2D(textureSampler, texCoordsB).r) / 2;
    strength = pow(strength, lerp(3, 0.33f, baseCoords.y));
    
    //fade out at horizontal edges
    strength *= pow(1 - (2 * abs(baseCoords.x - 0.5f)), 0.125f);
    
    float flipYCoord = 1 - baseCoords.y;
    float fadeThreshold = progress - verticalFade;
    if (flipYCoord > fadeThreshold)
    {
        strength *= max(1 - ((flipYCoord - fadeThreshold) / verticalFade), 0);
    }
    
    float colorStrength = pow(strength, 0.66f);
    colorStrength = round(colorStrength * numColors) / numColors;
    
    float4 finalColor = uColor;
    
    if(strength < 0.5f)
        finalColor = lerp(uColor3, uColor2, colorStrength * 2);
    else
        finalColor = lerp(uColor2, uColor, (colorStrength - 0.5f) * 2);
    
    return input.Color * finalColor * strength * finalIntensityMod;
}

technique BasicColorDrawing
{
    pass PrimitiveTextureMap
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};