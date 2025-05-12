sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
matrix WorldViewProjection;
float4 lightColor;
float4 midColor;
float4 darkColor;

texture uTexture;
sampler textureSampler = sampler_state
{
    Texture = (uTexture);
    AddressU = wrap;
    AddressV = wrap;
};

texture distortTexture;
sampler distortSampler = sampler_state
{
    Texture = (distortTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float2 textureStretch;
float2 distortStretch;
float2 scroll;
float2 distortScroll;
float intensity;
float tapering;
float fadePower;
float2 pixelDimensions;

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

float adjustYCoord(float yCoord, float multFactor, float anchorCoord = 0.5f)
{
    float temp = yCoord - anchorCoord;
    temp /= multFactor + 0.0001f;
    return temp + anchorCoord;
}

float EaseOutIn(float input, float power)
{
    power = max(power, 0.001f);
    if (input < 0.5f)
        return pow(2 * input, power) / 2;
    
    return pow(2 * (input - 0.5f), 1 / power) + 0.5f;
}

float4 ColorLerp3(float amount)
{
    if (amount < 0.5f)
        return lerp(darkColor, midColor, amount * 2);

    return lerp(midColor, lightColor, (amount - 0.5f) * 2);
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    //Pixellate the shader
    float2 baseCoords = float2(round(input.TextureCoordinates.x * pixelDimensions.x) / pixelDimensions.x, round(input.TextureCoordinates.y * pixelDimensions.y) / pixelDimensions.y);
    float yCoord = baseCoords.y;
    
    //Fade out the gradient based on x coordinate
    float strength = pow(1 - baseCoords.x, fadePower);
    
    //Distort the shader using a noise texture
    float2 distortCoord = float2((baseCoords.x - distortScroll.x) * distortStretch.x / 2, adjustYCoord(yCoord + distortScroll.y, 1 / distortStretch.y));
    float distortStrength = (tex2D(distortSampler, distortCoord).r - 0.5f) * lerp(0, 0.1f, 1 - strength);
    
    float xCoord = baseCoords.x + distortStrength;
    yCoord += distortStrength;
    
    //Taper the result based on the x coordinate
    yCoord = adjustYCoord(yCoord, pow(1 - xCoord, tapering));
    
    //Fade the strength gradient out based on distance from the vertical center
    float absYDist = abs(yCoord - 0.5f) * 2;
    if (absYDist > 1)
        return float4(0, 0, 0, 0);
    
    strength *= 1 - pow(absYDist, 2);
    
    float2 texCoordA = float2((xCoord - scroll.x) * textureStretch.x / 2, adjustYCoord(yCoord + scroll.y, 1 / textureStretch.y));
    float2 texCoordB = float2((xCoord - scroll.x * 0.75f) * textureStretch.x * 0.7f, adjustYCoord(yCoord - scroll.y, 0.8f / textureStretch.y));
    float2 texCoordsMask = float2((xCoord - scroll.x * 2) * textureStretch.x * 0.25f, adjustYCoord(yCoord, 0.25f / textureStretch.y));
    
    //Multiply the noise with itself using 2 different scroll speeds and tilings to create a less static texture
    float colorStrength = 1 - (tex2D(textureSampler, texCoordA).r * tex2D(textureSampler, texCoordB).r);
    colorStrength = pow(colorStrength, lerp(2, 1, xCoord));
    colorStrength = max(colorStrength - pow(strength, 3) / 3, 0);
    
    //Use a step function and smoothstep to define the shape of the fire, then multiply by the strength gradient at the end
    float stepStrength = min(strength + pow(strength, 2) * tex2D(textureSampler, texCoordsMask).r, 1);
    if (step(colorStrength, stepStrength) == 0)
        return float4(0, 0, 0, 0);
    
    colorStrength = max(1 - smoothstep(0, stepStrength, colorStrength), 0) * strength;

    float4 finalColor = input.Color * ColorLerp3(pow(colorStrength, 2)) * colorStrength;
    return finalColor * intensity;
}

technique BasicColorDrawing
{
    pass GeometricStyle
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};