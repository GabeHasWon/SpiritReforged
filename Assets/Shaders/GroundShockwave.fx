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

texture uTexture2;
sampler textureSampler2 = sampler_state
{
    Texture = (uTexture2);
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
float direction;


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

const float verticalFade = 0.3f;
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 baseCoords = input.TextureCoordinates;
    baseCoords = round(baseCoords * pixelDimensions) / pixelDimensions;
    
    float2 texCoordsA = float2(baseCoords.x + scroll.x, baseCoords.y + scroll.y) * textureStretch;
    float2 texCoordsB = float2(baseCoords.x + scroll.x, baseCoords.y + scroll.y) * textureStretch;
    
    //combine 2 different samplings of noise
    float strength = (tex2D(textureSampler, texCoordsA).r + tex2D(textureSampler2, texCoordsB).r) / 2;
    float flipYCoord = (1 - baseCoords.y);
    float fadeXCoords = (direction > 0) ? baseCoords.x : (1 - baseCoords.x);
    
    float fadeThreshold = pow(progress, lerp(0.25f, 3, fadeXCoords));
    fadeThreshold *= lerp(0.2f, 1, fadeXCoords);
    float fadeDist = verticalFade * progress;
    
    strength = pow(strength, lerp(3, 0.33f, 1 - (flipYCoord / fadeThreshold)));
    
    //fade out at top
    if (flipYCoord > max(fadeThreshold - fadeDist, 0))
    {
        float smoothStepFade = smoothstep(max(fadeThreshold - fadeDist, 0), fadeThreshold, flipYCoord);
        strength *= 1 - smoothStepFade;
        strength = pow(strength, lerp(1, 2, smoothStepFade));
    }
    
    //fade out at horizontal edge
    if (fadeXCoords > 0.9f * progress)
    {
        float smoothStepFade = smoothstep(0.9f * progress, (0.9f * progress) + 0.1f, fadeXCoords);
        strength *= 1 - smoothStepFade;
        strength = pow(strength, lerp(1, 2, smoothStepFade));
    }
    
    float colorStrength = pow(strength, 0.75f);
    colorStrength = round(colorStrength * numColors) / numColors;
    
    float4 finalColor = uColor;
    
    if (colorStrength < 0.5f)
        finalColor = lerp(uColor3, uColor2, colorStrength * 2);
    else
        finalColor = lerp(uColor2, uColor, (colorStrength - 0.5f) * 2);
    
    return input.Color * finalColor * finalIntensityMod * pow(strength, 0.75f);
}

technique BasicColorDrawing
{
    pass PrimitiveTextureMap
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};