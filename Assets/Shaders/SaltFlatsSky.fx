sampler drawTexture : register(s0);
float texColorUVLerper;
matrix WorldViewProjection;
matrix viewMatrix;

float3 baseColor;
float3 gradientColor;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 uv : TEXCOORD0;
    float2 screenUv : TEXCOORD1;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

float invlerp(float from, float to, float value)
{
    return saturate((value - from) / (to - from));
}
float2 invlerp(float2 from, float2 to, float2 value)
{
    return saturate((value - from) / (to - from));
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = pos;
    output.Color = input.Color;
    output.uv = input.TextureCoordinates;
    output.screenUv = mul(input.Position, viewMatrix);
    output.screenUv = invlerp(float2(-1, -1), float2(1, 1), output.screenUv);
    
    return output;
};

float4 MainPS(VertexShaderOutput i) : COLOR0
{
    float2 heightAndMask = float2(invlerp(0, 0.5, i.uv.y), invlerp(0, 0.2, i.uv.y));
    float mask = 1;
    
    float2 textureSample = tex2D(drawTexture, i.uv).ra;
    textureSample.y *= 0.4;
    heightAndMask = lerp(heightAndMask, textureSample, texColorUVLerper);
    heightAndMask.y *= i.Color.a;
    
    float4 blueGradient = float4(baseColor + gradientColor * heightAndMask.r, 0);
    
    return blueGradient * heightAndMask.y;
}

technique BasicColorDrawing
{
    pass MainPS
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};