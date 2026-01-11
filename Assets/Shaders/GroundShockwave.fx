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
    float2 texCoordsB = float2(baseCoords.x + scroll.x, baseCoords.y + scroll.y / 2) * textureStretch / 2;
    
    //combine 2 different samplings of noise
    float strength = (tex2D(textureSampler, texCoordsA).r + tex2D(textureSampler, texCoordsB).r) / 2;
    strength = pow(strength, lerp(5, 0.15f, baseCoords.y));
    
    float flipYCoord = (1 - baseCoords.y);
    float fadeXCoords = (direction > 0) ? baseCoords.x : (1 - baseCoords.x);
    
    float fadeThreshold = pow(progress, lerp(0.25f, 3, fadeXCoords));
    fadeThreshold *= pow(fadeXCoords, 0.7f);
    float fadeDist = verticalFade * progress;
    
    //fade out at top
    if (flipYCoord > max(fadeThreshold - fadeDist, 0))
    {
        float smoothStepFade = smoothstep(max(fadeThreshold - fadeDist, 0), fadeThreshold, flipYCoord);
        strength *= 1 - smoothStepFade;
        strength = pow(strength, lerp(1, 2, smoothStepFade));
    }
    
    //fade out at horizontal edge
    if (fadeXCoords > 0.9f)
    {
        float smoothStepFade = smoothstep(0.9f, 1, fadeXCoords);
        strength *= 1 - smoothStepFade;
        strength = pow(strength, lerp(1, 2, smoothStepFade));
    }
    
    //fade out from bottom
    float bottomFadeThreshold = pow(max(progress - 0.75f, 0) * 4, lerp(0.25f, 2.5f, fadeXCoords));
    strength = pow(strength, lerp(1, 4, bottomFadeThreshold));
    
    if (flipYCoord < bottomFadeThreshold)
    {
        float vertFade2 = lerp(verticalFade, 0, bottomFadeThreshold);
        float smoothStepFade = 1 - smoothstep(max(bottomFadeThreshold - vertFade2, 0), bottomFadeThreshold, flipYCoord);
        strength = pow(strength, lerp(1, 4, smoothStepFade));
        strength *= 1 - smoothStepFade;
    }
    
    float colorStrength = pow(strength, 0.5f);
    colorStrength = round(colorStrength * numColors) / numColors;
    
    float4 finalColor = uColor;
    
    if (strength < 0.5f)
        finalColor = lerp(uColor3, uColor2, colorStrength * 2);
    else
        finalColor = lerp(uColor2, uColor, (colorStrength - 0.5f) * 2);
    
    return input.Color * finalColor * finalIntensityMod * pow(strength, 0.5f);
}

technique BasicColorDrawing
{
    pass PrimitiveTextureMap
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};