sampler cloudReflection : register(s0);

matrix WorldViewProjection;
float totalHeight;
float reflectionHorizonHeight;
float cloudTargetYOffset;
float topFadeStrength;
float shimmerAlpha;

texture reflectionMaskTexture;
sampler reflectionMask
{
    Texture = (reflectionMaskTexture);
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = clamp;
};
matrix maskTransform;
matrix maskReverseTransform;


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

float invlerp(float from, float to, float value)
{
    return saturate((value - from) / (to - from));
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    //Get the mask by transforming the screenspace coords into coordinates that match the drawn background texture outside of the shader
    float2 maskCoords = mul(float4(coords.x, coords.y, 1, 1), maskTransform).xy;
    float4 mask = tex2D(reflectionMask, maskCoords);
    
    //To get the screenspace position of the reflection horizon, use the reverse of the transform matrix
    float reflectionLine = mul(float4(1, reflectionHorizonHeight, 1, 1), maskReverseTransform).y;
    
    float2 flippedCoords = coords - float2(0, cloudTargetYOffset);
    flippedCoords.y = (flippedCoords.y - reflectionLine) * -1 + reflectionLine;
    
    
    float4 reflectedColor = tex2D(cloudReflection, flippedCoords);
    reflectedColor *= mask.a * pow(mask.r, 0.5);
    //Fade if going past the screen bounds
    reflectedColor *= invlerp(0, 0.2, flippedCoords.y);
    
    //Fade the sky near the top based on a value
    reflectedColor *= lerp(1, invlerp(0.2, 0.48, flippedCoords.y), topFadeStrength);
    
    
    reflectedColor = lerp(reflectedColor, float4(0, 0, 0, 1), shimmerAlpha);
    
    return reflectedColor * input.Color;
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};