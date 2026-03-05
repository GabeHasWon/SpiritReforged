sampler uImage0 : register(s0);

float progress;
float strength;
float length;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

float4 MainPS(VertexShaderInput input) : COLOR0
{
    return input.Color * tex2D(uImage0, input.TextureCoordinates + float2(0, sin(progress + (input.TextureCoordinates.x / length)) * strength));
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
};