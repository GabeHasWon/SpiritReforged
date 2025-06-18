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

bool pixellate;
float2 pixelDimensions;
float2 scroll;

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

float4 ColorLerp3(float amount)
{
    if (amount < 0.5f)
        return lerp(tertiaryColor, secondaryColor, amount * 2);

    return lerp(secondaryColor, primaryColor, (amount - 0.5f) * 2);
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 baseCoords = input.TextureCoordinates;
    if (pixellate)
        baseCoords = round(baseCoords * pixelDimensions) / pixelDimensions;
    
    float2 noiseCoords = float2((baseCoords.x + scroll.x) * coordMods.x, (baseCoords.y + scroll.y) * coordMods.y);
    float noiseFactor = 2 * (tex2D(noiseSampler, noiseCoords) - 0.5f);
    float2 texCoords = float2(baseCoords.x + (distortion * noiseFactor), baseCoords.y + (distortion * noiseFactor));
    
    float baseTexStrength = tex2D(textureSampler, texCoords).r;
    if (baseTexStrength <= 0.05f)
        return float4(0, 0, 0, 0);
    
    float strength = pow(baseTexStrength, texExponent);
    
    float dissolveNoiseStrength = (1 - tex2D(secondaryNoiseSampler, noiseCoords).r) * dissolve;
    float dissolveYStrength = lerp(0, pow(input.TextureCoordinates.y, 0.66f), pow(dissolve, 0.5f));
    float dissolveXStrength = lerp(0, pow((1 - abs(baseCoords.x - 0.5f) * 2), 0.66f), pow(dissolve, 0.5f));
    float dissolvePosBase = dissolveYStrength + dissolveXStrength;
    
    strength = smoothstep(min((dissolveNoiseStrength + dissolvePosBase + pow(dissolve, 4)) / 2, 1), 1, pow(strength, 0.33f)) * pow(strength, 0.66f);
    
    return color * strength * ColorLerp3(pow(strength * pow(1 - dissolve, 0.5f), colorLerpExp)) * intensity;
}

technique BasicColorDrawing
{
    pass DefaultPS
	{
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};