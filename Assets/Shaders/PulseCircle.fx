sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
matrix WorldViewProjection;
float4 RingColor;
float4 BloomColor;
float RingWidth;

texture uTexture;
sampler textureSampler = sampler_state
{
    Texture = (uTexture);
    AddressU = wrap;
    AddressV = wrap;
};
float2 textureStretch;
float texExponent;
float scroll;

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

const float ringBase = 0.5f;

float GetDistance(float2 input)
{
    return (2 * sqrt(pow(input.x - 0.5f, 2) + pow(input.y - 0.5f, 2)));
}

float GetDistanceFromBase(float input)
{
    return abs(input - ringBase) * 2;
}

float GetDistanceFromBase(float2 input)
{
    return GetDistanceFromBase(GetDistance(input));
}

float GetStrength(VertexShaderOutput input)
{
    float finalStrength;
    
    float distance = GetDistance(input.TextureCoordinates);
    float DistFromRingbase = GetDistanceFromBase(distance);
    if (distance >= 1) //transparent if too much distance from center, as the shader is being applied to a square
        return 0;
    
    else if (DistFromRingbase <= RingWidth * 0.5f) //always return peak opacity within the specified range
        finalStrength = 1;
    else if (DistFromRingbase <= RingWidth * 0.75f) //interpolate to the bloom color between the given range
    {
        float lerpFactor = 1 - cos(3.14f * min((abs(DistFromRingbase - (RingWidth / 2)) / (RingWidth / 2)), 1)) / 2;
        finalStrength = lerp(1, 0.5f, lerpFactor);
    }
    else //interpolate to transparent if too far from the ring's edges
    {
        float lerpFactor = min(abs(DistFromRingbase - (RingWidth * 0.75f)) / (RingWidth / 4), 1);
        finalStrength = lerp(0.5f, 0, lerpFactor);
    }
    
    return finalStrength;
}

float4 GeometricRing(VertexShaderOutput input) : COLOR0
{
    float4 finalColor = RingColor;
    float strength = GetStrength(input);
    if (strength == 0)
        return float4(0, 0, 0, 0);
    
    if (strength > 0.5f)
        finalColor = lerp(BloomColor, RingColor, (strength - 0.5f) * 2) * strength;
    else
        finalColor = lerp(float4(0, 0, 0, 0), BloomColor, strength * 2) * strength;
    
    finalColor *= input.Color;
    return finalColor * 2;
}

float GetAngle(float2 input)
{
    return atan2(input.y - 0.5f, input.x - 0.5f) + 3.14f;
}

float4 TexturedRing(VertexShaderOutput input) : COLOR0
{
    float strength = GetStrength(input);
    float xCoord = GetAngle(input.TextureCoordinates) / 6.28f;
    xCoord += scroll;
    float yCoord = GetDistance(input.TextureCoordinates);
    yCoord -= 0.5f;
    yCoord *= textureStretch.y / RingWidth;
    yCoord += 0.5f;
    float texColor = pow(tex2D(textureSampler, float2(xCoord * textureStretch.x, yCoord)).r, texExponent) * strength;
    if (texColor == 0)
        return float4(0, 0, 0, 0);
    
    float4 finalColor = RingColor;
    
    if (texColor > 0.5f)
        finalColor = lerp(BloomColor, RingColor, (texColor - 0.5f) * 2) * texColor;
    else
        finalColor = lerp(float4(0, 0, 0, 0), BloomColor, texColor * 2) * texColor;
    
    finalColor *= input.Color;
    return finalColor * 2;
}

technique BasicColorDrawing
{
    pass GeometricStyle
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 GeometricRing();
    }

    pass TexturedStyle
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 TexturedRing();
    }
};