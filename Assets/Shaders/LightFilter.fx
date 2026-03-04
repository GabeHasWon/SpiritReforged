sampler uImage0 : register(s0); // The contents of the screen
sampler noiseTex : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);

float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float4 uShaderSpecificData;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float hueToRGB(float p, float q, float t)
{
    if (t < 0)
        t += 1;
    if (t > 1)
        t -= 1;
    if (t < 0.166f)
        return p + (q - p) * 6.0f * t;
    if (t < 0.5f)
        return q;
    if (t < 0.66f)
        return p + (q - p) * (0.66f - t) * 6.0f;
    return p;
}

float3 hslToRGB(float h, float s, float l)
{
    float r, g, b;
    float q = l < 0.5 ? l * (1 + s) : (l + s) - (l * s);
    float p = (2 * l) - q;
    r = hueToRGB(p, q, h + 0.33f);
    g = hueToRGB(p, q, h);
    b = hueToRGB(p, q, h - 0.33f);
    
    return float3(r, g, b);
}

float4 MainPS(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    float distanceToCenter = length(float2(0.5, 0.5) - uv) * 1.4142;
    
    float darknessMultiplier = uProgress;
    float intensity = uIntensity;
    float opacity = uOpacity;

    float4 color = tex2D(uImage0, uv);
    float4 noiseColor = tex2D(noiseTex, uv);

    float vignette = pow(distanceToCenter, 5) * 10 * intensity;
    color.rgb *= 1 + hslToRGB(noiseColor.w + (uTime / 10) % 1, 0.5, 0.5) * vignette;
    color *= 1 + vignette * (0.2 + 0.8 * darknessMultiplier);

    color.a = 1;
    return color;
}

technique Technique1
{
    pass LightFilterPass
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}