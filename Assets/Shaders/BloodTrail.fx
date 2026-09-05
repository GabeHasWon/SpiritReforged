sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
matrix WorldViewProjection;
texture uTexture;
sampler2D textureSampler = sampler_state
{
    texture = <uTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture uTexture2;
sampler2D texture2Sampler = sampler_state
{
    texture = <uTexture2>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float repeats = 1;
float scroll = 0;
float4 uColorLight;
float4 uColorDark;
float progress;

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

float adjustYCoord(float yCoord, float multFactor, float anchorCoord = 0.5f)
{
    float temp = yCoord - anchorCoord;
    temp /= multFactor + 0.0001f;
    return temp + anchorCoord;
}

float4 White(VertexShaderOutput input) : COLOR0
{
    //taper logic
    float yAbsDist = 1 - (abs(input.TextureCoordinates.y - 0.5f) * 2);
    float xCoord = adjustYCoord(input.TextureCoordinates.x, pow(EaseCircOut(yAbsDist), 3));
    if (xCoord > 1 || xCoord < 0)
        return float4(0, 0, 0, 0);
    
    float xAbsDist = 1 - (abs(xCoord - 0.5f) * 2);
    float2 coords = float2((input.TextureCoordinates.y * repeats) + scroll, xCoord);
    
    //smoothstep to make the trail's shape
    float widthFactor = EaseCircOut(tex2D(texture2Sampler, coords).r * 0.75f + xAbsDist * 0.25f);
    float lengthFactor = pow(1 - input.TextureCoordinates.y, 1.5f);
    float4 color;
    color.r = smoothstep(0, tex2D(textureSampler, coords).r, widthFactor * lengthFactor);
    
    //raise to a power to make the colors more intense
    float strength = pow(color.r, 0.33f);
    
    //position fading
    strength *= pow(EaseCircOut(xAbsDist), 3);
    strength = pow(strength, 2.5f);
    
    strength *= pow(EaseCircOut(yAbsDist), 3);
    if (yAbsDist < 0.3f)
        strength *= pow(1 - ((0.3f - yAbsDist) / 0.3f), 2);
    
    //progress-based position fade from tail
    strength *= smoothstep(input.TextureCoordinates.y, 1, 1 - progress);
    
    //interpolate between 2 colors based on strength
    strength = round(strength * 12) / 12;
    color = lerp(uColorDark, uColorLight, pow(strength, 2));
    
    return color * input.Color * strength * 1.5f;
}

technique BasicColorDrawing
{
    pass DefaultPass
    {
        VertexShader = compile vs_3_0 MainVS();
    }
    pass MainPS
    {
        PixelShader = compile ps_3_0 White();
    }
};