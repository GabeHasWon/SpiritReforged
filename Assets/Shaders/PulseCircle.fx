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

//helpers
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

float3 rgb2hsv(float3 rgb)
{
    float r = rgb.r;
    float g = rgb.g;
    float b = rgb.b;
    
    float maxV = max(max(r, g), b);
    float minV = min(min(r, g), b);
    
    float h = 0;
    float s = 0;
    float v = maxV;
    
    float d = maxV - minV;
    
    if (maxV != 0)
    {
        s = d / maxV;
    }
    
    if (maxV == minV)
    {
        h = 0;
    }
    else
    {
        if (maxV == r)
        {
            h = (g - b) / d + (g < b ? 6 : 0);
        }
        else if (maxV == g)
        {
            h = (b - r) / d + 2;
        }
        else if (maxV == b)
        {
            h = (r - g) / d + 4;
        }
        
        h /= 6;
    }
    
    return float3(h, s, v);
}

float3 hsv2rgb(float3 hsv)
{
    float h = frac(hsv.r);
    float s = saturate(hsv.g);
    float v = saturate(hsv.b);
    
    float r = 0;
    float g = 0;
    float b = 0;
    
    if (s == 0)
    {
        r = v;
        g = v;
        b = v;
    }
    else
    {
        float h6 = h * 6;
        float i = floor(h6);
        float f = h6 - i;
        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));
        
        r = (i == 0 || i == 5) ? v : i == 4 ? t : i > 1 ? p : q;
        g = (i == 1 || i == 2) ? v : i == 0 ? t : i > 3 ? p : q;
        b = (i == 3 || i == 4) ? v : i == 2 ? t : i < 2 ? p : q;
    }
    
    return float3(r, g, b);
}

float GetAngle(float2 input)
{
    return atan2(input.y - 0.5f, input.x - 0.5f) + 3.14f;
}

//Main functions
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

float4 LensFlareRing(VertexShaderOutput input) : COLOR0
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
    
    float4 finalColor = texColor * input.Color;
    
    float3 HSV = rgb2hsv(finalColor.rgb);
    HSV.x = input.TextureCoordinates.y;
    finalColor.rgb = hsv2rgb(HSV);
    
    return finalColor;
}
float4 RoarRing(VertexShaderOutput input) : COLOR0
{
    float strength = max(1 - GetDistanceFromBase(input.TextureCoordinates), 0);
    float xCoord = GetAngle(input.TextureCoordinates) / (6.28f * RingWidth);
    float yCoord = GetDistance(input.TextureCoordinates) + scroll;
    yCoord -= 0.5f;
    yCoord *= textureStretch.y / RingWidth;
    yCoord += 0.5f;
    float texColor = strength;
    float ringWidthAdjusted = (1 - RingWidth);
    
    if (ringWidthAdjusted < strength)
    {
        float lerper = (strength - ringWidthAdjusted) / RingWidth;
        texColor = pow(tex2D(textureSampler, float2(xCoord * textureStretch.x, yCoord)).r, lerp(2, 3, pow(1 - lerper, 2)));
        texColor *= lerp(0.75f, 1, pow(lerper, 0.5f));
        texColor = (texColor * lerper + pow(lerper, 0.5f)) / 2;
    }
    
    else
        return float4(0, 0, 0, 0);
    
    float4 finalColor = RingColor * texColor;
    
    finalColor = pow(finalColor, lerp(2, 3, 1 - input.Color.r));
    finalColor *= input.Color;
    
    return finalColor;
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

    pass LensFlareStyle
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 LensFlareRing();
    }

    pass RoarStyle
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 RoarRing();
    }
};