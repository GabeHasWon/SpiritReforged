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

const float FadeOutRangeX = 0.95f;

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

float2 SwingTrailBasicShape(float inputXCoord, float2 inputStrengthYCoord, float yCoordCenterpoint = 0.5f)
{
    float trailEnd = max(progress - trailLength, 0);
    if (inputXCoord < progress)
    {
        float trailProgress = (inputXCoord - trailEnd) / (progress - trailEnd);
        inputStrengthYCoord.x = pow(trailProgress, fadeStrength);
        inputStrengthYCoord.y = adjustYCoord(inputStrengthYCoord.y, pow(trailProgress, taperStrength), yCoordCenterpoint);
        
        if (inputXCoord < trailEnd)
            return 0;
        
        float fadeStart = progress * FadeOutRangeX;
        if (inputXCoord > fadeStart)
        {
            float frontFade = (inputXCoord - fadeStart) / (progress - fadeStart);
            frontFade = 1 - frontFade;
            
            inputStrengthYCoord.x *= pow(frontFade, 1.75f);
        }
    }
    
    return float2(inputStrengthYCoord.x, inputStrengthYCoord.y);
}

float4 CleanStreak(VertexShaderOutput input) : COLOR0
{
    float strength = 0;
    float yCoord = input.TextureCoordinates.y;
    
    float2 tempStrYCoord = float2(strength, yCoord);
    tempStrYCoord = SwingTrailBasicShape(input.TextureCoordinates.x, tempStrYCoord, 1);
    strength = tempStrYCoord.x;
    yCoord = tempStrYCoord.y;
    
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
    float yCoord = input.TextureCoordinates.y;
    
    float2 tempStrYCoord = float2(strength, yCoord);
    tempStrYCoord = SwingTrailBasicShape(input.TextureCoordinates.x, tempStrYCoord);
    strength = tempStrYCoord.x;
    yCoord = tempStrYCoord.y;
    
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

float4 FlameTrail(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float strength = 0;
    float trailEnd = max(progress - trailLength, 0);
    float yCoord = input.TextureCoordinates.y;
    float xCoord = (input.TextureCoordinates.x - trailEnd) / (progress - trailEnd);
    
    float2 tempStrYCoord = float2(strength, yCoord);
    tempStrYCoord = SwingTrailBasicShape(input.TextureCoordinates.x, tempStrYCoord, 0.8f);
    strength = tempStrYCoord.x;
    yCoord = tempStrYCoord.y;
    
    strength = min(strength, 1);
    float absYDist = abs(yCoord - 0.5f) * 2;
    if (absYDist > 1)
        strength = 0;
    
    if (strength == 0)
        return float4(0, 0, 0, 0);
    
    strength *= pow(EaseCircOut(1 - absYDist), 3);
    
    float2 texCoordA = float2((input.TextureCoordinates.x - timer) * coordMods.x / 2, adjustYCoord(yCoord + timer / 2, 1 / coordMods.y));
    float2 texCoordB = float2((input.TextureCoordinates.x - timer * 0.75f) * coordMods.x * 0.7f, adjustYCoord(yCoord - timer / 2, 0.8f / coordMods.y));
    float2 texCoordC = float2((input.TextureCoordinates.x - timer * 2) * coordMods.x * 0.25f, adjustYCoord(yCoord, 0.25f / coordMods.y));
    
    float uExponent = lerp(textureExponent.x, textureExponent.y, xCoord);
    float colorStrength = pow(1 - (tex2D(baseSampler, texCoordA).r * tex2D(baseSampler, texCoordB).r), uExponent);
    float stepStrength = min(strength + pow(strength, 0.25f) * tex2D(baseSampler, texCoordC).r, 1);
    if (step(colorStrength, stepStrength) == 0 && colorStrength != 1)
        return float4(0, 0, 0, 0);
    
    colorStrength = max(min((strength / 5) + 1 - smoothstep(0, stepStrength, colorStrength), 1), 0) * strength;

    float4 finalColor = color * lerp(baseColorDark, baseColorLight, pow(EaseOutIn(colorStrength, 0.5f), 3)) * pow(colorStrength, 1.25f);
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