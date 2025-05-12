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
float dissipate;

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

float EaseCircOut(float x)
{
    return sqrt(1 - pow(x - 1, 2));
}

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
    float4 color = input.Color;
    
    float2 baseCoords = float2(round(input.TextureCoordinates.x * 200) / 200, round(input.TextureCoordinates.y * 50) / 50);
    float strength = pow(1 - baseCoords.x, 1.5f);
    float yCoord = baseCoords.y;

    
    float2 distortCoord = float2((baseCoords.x - distortScroll.x) * distortStretch.x / 2, adjustYCoord(yCoord + distortScroll.y, 1 / distortStretch.y));
    float distortStrength = (tex2D(distortSampler, distortCoord).r - 0.5f) * lerp(0, 0.1f, 1 - strength);
    
    float xCoord = baseCoords.x + distortStrength;
    yCoord += distortStrength;
    
    yCoord = adjustYCoord(yCoord, pow(strength, 0.25f));
    
    strength = min(strength, 1);
    float absYDist = abs(yCoord - 0.5f) * 2;
    if (absYDist > 1)
        strength = 0;
    
    if (strength == 0)
        return float4(0, 0, 0, 0);
    
    strength *= pow(EaseCircOut(1 - absYDist), 2);
    
    float2 texCoordA = float2((baseCoords.x - scroll.x) * textureStretch.x / 2, adjustYCoord(yCoord + scroll.y, 1 / textureStretch.y));
    float2 texCoordB = float2((baseCoords.x - scroll.x * 0.75f) * textureStretch.x * 0.7f, adjustYCoord(yCoord - scroll.y, 0.8f / textureStretch.y));
    float2 texCoordC = float2((baseCoords.x - scroll.x * 2) * textureStretch.x * 0.25f, adjustYCoord(yCoord, 0.25f / textureStretch.y));
    
    float colorStrength = pow(1 - (tex2D(textureSampler, texCoordA).r * tex2D(textureSampler, texCoordB).r), lerp(3, 0.5f, baseCoords.x));
    colorStrength = max(colorStrength - pow(strength, 3) / 3, 0);
    float stepStrength = min(strength + pow(strength, 2) * tex2D(textureSampler, texCoordC).r, 1);
    if (step(colorStrength, stepStrength) == 0)
        return float4(0, 0, 0, 0);
    
    colorStrength = max(1 - smoothstep(0, stepStrength, colorStrength), 0) * strength;
    colorStrength = round(colorStrength * 15) / 15;

    float4 finalColor = color * ColorLerp3(pow(colorStrength, 2)) * pow(colorStrength, 1.25f);
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