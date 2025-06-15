matrix WorldViewProjection;
texture uTexture;
sampler textureSampler = sampler_state
{
    Texture = (uTexture);
    AddressU = wrap;
    AddressV = clamp;
};
texture noise;
sampler noiseSampler = sampler_state
{
    Texture = (noise);
    AddressU = wrap;
    AddressV = wrap;
};
texture secondaryNoise;
sampler secondaryNoiseSampler = sampler_state
{
    Texture = (secondaryNoise);
    AddressU = wrap;
    AddressV = wrap;
};

float Progress;
float uTime;
float dissolve;
float intensity;

float4 primaryColor;
float4 secondaryColor;
float4 tertiaryColor;
float colorLerpExp;

float2 coordMods;
float distortion;
float texExponent;

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
        return lerp(tertiaryColor, secondaryColor, EaseOutIn(amount * 2, 1));

    return lerp(secondaryColor, primaryColor, EaseOutIn((amount - 0.5f) * 2, 1));
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    
    float2 noiseCoords = float2(input.TextureCoordinates.x * coordMods.x, input.TextureCoordinates.y * coordMods.y);
    float noiseFactor = 2 * (tex2D(noiseSampler, noiseCoords) - 0.5f);
    float2 texCoords = float2(input.TextureCoordinates.x + (distortion * noiseFactor), input.TextureCoordinates.y + (distortion * noiseFactor));
    
    float baseTexStrength = tex2D(textureSampler, texCoords).r;
    if (baseTexStrength <= 0.05f)
        return float4(0, 0, 0, 0);
    
    float strength = pow(baseTexStrength, texExponent);
    
    float dissolveNoiseStrength = (1 - pow(tex2D(secondaryNoiseSampler, noiseCoords).r, 0.5f)) * dissolve;
    float dissolveYStrength = input.TextureCoordinates.y * dissolve;
    
    strength = smoothstep(min(dissolveNoiseStrength + dissolveYStrength, 1), 1, pow(strength, 0.5f)) * pow(strength, 0.5f);
    
    return color * strength * ColorLerp3(pow(strength, colorLerpExp)) * intensity;
}

technique BasicColorDrawing
{
    pass DefaultPS
	{
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};