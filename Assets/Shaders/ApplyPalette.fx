sampler baseTex : register(s0);

texture recolorRamp;
sampler rampTex = sampler_state
{
    Texture = <recolorRamp>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = LINEAR;
    minfilter = LINEAR;
};

bool smoothRamp;

float invlerp(float from, float to, float value)
{
    // Utils.GetLerpValue()
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float4 MainPS(float2 uv : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 brightnessAndOpacity = tex2D(baseTex, uv).ra;
    float4 returnColor = tex2D(rampTex, float2(smoothRamp ? 0 : 1, brightnessAndOpacity.r));
    return returnColor * vertexColor * brightnessAndOpacity.g;
}

technique BasicColorDrawing
{
    pass PalettePass
	{
        PixelShader = compile ps_3_0 MainPS();
    }
};