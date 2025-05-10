sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
matrix WorldViewProjection;
texture baseTexture;
sampler baseSampler = sampler_state
{
    Texture = (baseTexture);
    AddressU = wrap;
    AddressV = wrap;
};
texture bloomTex;
sampler bloomSampler = sampler_state
{
    Texture = (bloomTex);
    AddressU = wrap;
    AddressV = wrap;
};
float4 baseColorDark;
float4 baseColorLight;

float2 textureExponent;

float2 coordMods;
float timer;
float progress;
float trailLength;
float taperStrength;
float fadeStrength;
float intensity;

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
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;

    output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

const float FadeOutRangeX = 0.9f;

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

float4 CleanStreak(VertexShaderOutput input) : COLOR0
{
    float strength = 0;
    
    float trailEnd = max(progress - trailLength, 0);
    float frontFade = progress * FadeOutRangeX;
    float yCoord = input.TextureCoordinates.y;
    
    //fade out based on position
    if (input.TextureCoordinates.x < progress) //horizontal
    {
        float trailProgress = (input.TextureCoordinates.x - trailEnd) / (progress - trailEnd);
        strength = pow(trailProgress, fadeStrength);
        yCoord = adjustYCoord(yCoord, pow(trailProgress, taperStrength), 1);
        
        if (input.TextureCoordinates.x < trailEnd)
            return 0;
        
        float fadeStart = progress * FadeOutRangeX;
        if (input.TextureCoordinates.x > fadeStart)
        {
            float frontFade = (input.TextureCoordinates.x - fadeStart) / (progress - fadeStart);
            frontFade = 1 - frontFade;
            
            strength *= pow(frontFade, 1.75f);
        }
    }
    
    strength = min(strength, 1);
    if (yCoord > 1 || yCoord < 0)
        return 0;
    
    if (yCoord > 0.8f)
    {
        float yAbsDist = abs(yCoord - 0.9f) * 10;
        yAbsDist = 1 - yAbsDist;
        strength *= pow(EaseCircOut(yAbsDist), 2) / 2 + 0.5f;
    }
    else
        strength *= (yCoord * 0.8f) * 0.5f;

    float4 finalColor = input.Color * strength * lerp(baseColorDark, baseColorLight, strength);
    return finalColor * intensity;
}

float4 NoiseStreak(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float strength = 0;
    
    float trailEnd = max(progress - trailLength, 0);
    float frontFade = progress * FadeOutRangeX;
    float yCoord = input.TextureCoordinates.y;
    
    //fade out based on position
    if (input.TextureCoordinates.x < progress) //horizontal
    {
        float trailProgress = (input.TextureCoordinates.x - trailEnd) / (progress - trailEnd);
        strength = pow(trailProgress, fadeStrength);
        yCoord = adjustYCoord(yCoord, pow(trailProgress, taperStrength));
        
        if (input.TextureCoordinates.x < trailEnd)
            strength = 0;
        
        float fadeStart = progress * FadeOutRangeX;
        if (input.TextureCoordinates.x > fadeStart)
        {
            float frontFade = (input.TextureCoordinates.x - fadeStart) / (progress - fadeStart);
            frontFade = 1 - frontFade;
            
            strength *= pow(frontFade, 1.75f);
        }
    }
    
    strength = min(strength, 1);
    float absYDist = abs(yCoord - 0.5f) * 2;
    if (absYDist > 1)
        strength = 0;
    
    strength *= pow(EaseCircOut(1 - absYDist), 2);
    float uExponent = lerp(textureExponent.x, textureExponent.y, 1 - strength);
    strength *= pow(tex2D(baseSampler, float2((input.TextureCoordinates.x - timer) * coordMods.x, adjustYCoord(yCoord, 1 / coordMods.y))).r, uExponent);

    float4 finalColor = color * strength * lerp(baseColorDark, baseColorLight, strength);
    return finalColor * intensity;
}

float EaseOutIn(float input, float power)
{
    power = max(power, 0.001f);
    if (input < 0.5f)
        return pow(input, power);
    
    return pow(2 * (input - 0.5f), 1 / power);
}

float4 FlameTrail(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float strength = 0;
    
    float trailEnd = max(progress - trailLength, 0);
    float frontFade = progress * FadeOutRangeX;
    float yCoord = input.TextureCoordinates.y;
    float xCoord = (input.TextureCoordinates.x - trailEnd) / (progress - trailEnd);
    
    //fade out based on position
    if (input.TextureCoordinates.x < progress) //horizontal
    {
        strength = pow(xCoord, fadeStrength);
        yCoord = adjustYCoord(yCoord, pow(xCoord, taperStrength));
        
        if (input.TextureCoordinates.x < trailEnd)
            strength = 0;
        
        float fadeStart = progress * FadeOutRangeX;
        if (input.TextureCoordinates.x > fadeStart)
        {
            float frontFade = (input.TextureCoordinates.x - fadeStart) / (progress - fadeStart);
            frontFade = 1 - frontFade;
            
            strength *= pow(frontFade, 1.75f);
        }
    }
    
    strength = min(strength, 1);
    float absYDist = abs(yCoord - 0.5f) * 2;
    if (absYDist > 1)
        strength = 0;
    
    if (strength == 0)
        return float4(0, 0, 0, 0);
    
    strength *= pow(EaseCircOut(1 - absYDist), 3);
    
    float2 texCoordA = float2((xCoord - timer) * coordMods.x / 2, adjustYCoord(yCoord + timer / 2, 1 / coordMods.y));
    float2 texCoordB = float2((xCoord - timer * 0.75f) * coordMods.x * 0.7f, adjustYCoord(yCoord - timer / 2, 0.8f / coordMods.y));
    float2 texCoordC = float2((xCoord - timer * 2) * coordMods.x * 0.25f, adjustYCoord(yCoord, 0.25f / coordMods.y));
    
    float uExponent = lerp(textureExponent.x, textureExponent.y, xCoord);
    float colorStrength = pow(1 - (tex2D(baseSampler, texCoordA).r * tex2D(baseSampler, texCoordB).r), uExponent);
    if (step(colorStrength, pow(strength, 0.25f)) == 0 && colorStrength != 1)
        return float4(0, 0, 0, 0);
    
    colorStrength = max(min(1 - smoothstep(0, pow(strength, 0.25f), colorStrength), 1), 0) * strength;

    float4 finalColor = color * lerp(baseColorDark, baseColorLight, EaseOutIn(colorStrength, 2)) * pow(colorStrength, 1.5f);
    return finalColor * intensity;
}

technique BasicColorDrawing
{
    pass CleanStreakPass
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 CleanStreak();
    }

    pass NoiseStreakPass
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 NoiseStreak();
    }

    pass FlameTrailPass
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 FlameTrail();
    }
};