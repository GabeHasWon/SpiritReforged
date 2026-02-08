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

float EaseCircOut(float input)
{
    return sqrt(1 - pow(input - 1, 2));
}

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
    
    float fadeThreshold = pow(EaseCircOut(progress), 0.5f);
    
    float fadeDist = verticalFade * fadeThreshold;
    fadeThreshold *= lerp(0.2f, 1, fadeXCoords);
    
    //raise to a higher power the further up the pixel is, makes noise sampling thinner at top and thicker at bottom
    strength = pow(strength, lerp(4, 0.25f, 1 - (flipYCoord / fadeThreshold)));
    
    //fade out at top
    if (flipYCoord > max(fadeThreshold - fadeDist, 0))
    {
        float smoothStepFade = smoothstep(max(fadeThreshold - fadeDist, 0), fadeThreshold, flipYCoord);
        strength *= 1 - smoothStepFade;
        strength = pow(strength, lerp(1, 2, smoothStepFade));
    }
    
    //fade out at horizontal edge
    float largeEdge = 0.9f;
    if (fadeXCoords > largeEdge)
    {
        float smoothStepFade = smoothstep(largeEdge, largeEdge + 0.1f, fadeXCoords);
        strength *= 1 - smoothStepFade;
        strength = pow(strength, lerp(1, 2, smoothStepFade));
    }
    
    //interpolate color
    float4 finalColor = uColor;
    
    if (strength < 0.5f)
        finalColor = lerp(uColor3, uColor2, strength * 2);
    else
        finalColor = lerp(uColor2, uColor, (strength - 0.5f) * 2);
    
    return input.Color * finalColor * finalIntensityMod * strength;
}

technique BasicColorDrawing
{
    pass PrimitiveTextureMap
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};