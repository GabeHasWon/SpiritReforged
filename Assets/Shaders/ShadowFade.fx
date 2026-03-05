matrix uWorldViewProjection;

float4 baseShadowColor;
float noiseScroll; // Scroll speed of the noise
float noiseStretch; // How far the noise is spread apart
float4 adjustColor; // "Fancy" color between the darkness

texture noiseTexture;
sampler2D noise = sampler_state
{
    texture = <noiseTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};


struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}


float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;

    //Base gradient that gets darker using UV coords
    float opacity = pow(1 - uv.y, 1.5);
    //Fade at the horizontal edges of the quad
    opacity *= pow(invlerp(0.5, 0.4, abs(uv.x - 0.5)), 0.6);

    //fade based on opacity of input
    opacity *= baseShadowColor.a;

    float3 shadowColor = baseShadowColor;
    
    //tint shadows blueish or reddish based on noisemap that scrolls
    float colorfulness = tex2D(noise, float2(uv.x * noiseStretch, noiseScroll)).r;
    //Color goes from blue to red as we approach the edges
    float3 shadowTint = lerp(adjustColor, float3(0.33, 0.1, 0.1), invlerp(0.4, 0.5, abs(uv.x - 0.5)));

    shadowColor += colorfulness * shadowTint;
    return float4(shadowColor * opacity, opacity);
}

technique Technique1
{
    pass TreeShadowCastPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}