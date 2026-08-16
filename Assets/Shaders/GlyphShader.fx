sampler uImage0 : register(s0); // Item Texture

texture uImage1; // Base Noise Texture
sampler2D uImage1Sampler = sampler_state
{
    texture = <uImage1>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture uImage2; // Noise Texture for iridencent color ramp
sampler2D uImage2Sampler = sampler_state
{
    texture = <uImage2>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture uImage3; // Gradient Texture for color ramp
sampler2D uImage3Sampler = sampler_state
{
    texture = <uImage3>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = clamp;
};

float baseDepth;
float time;
float intensity;
float scale;
float2 screenPos;
float4 uColor1;
float4 uColor2;
float4 uColor3; // shine color

float2 itemSize; // size of the item texture

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 tex = tex2D(uImage0, uv);
    if (tex.a <= 0)
        return float4(0, 0, 0, 0);
    
    float2 pixelSize = float2(1.0 / itemSize.x, 1.0 / itemSize.y);
    
    float depth = baseDepth * (1.0 + intensity * 3);
    float2 offset = pixelSize * depth;
    
    // takes the sum of the alphas around the item sprite
    // for edge detection
    
    float alphaSum = 0.0;

    alphaSum += tex2D(uImage0, uv + float2(0, -offset.y)).a;
    alphaSum += tex2D(uImage0, uv + float2(0, offset.y)).a;
    alphaSum += tex2D(uImage0, uv + float2(-offset.x, 0)).a;
    alphaSum += tex2D(uImage0, uv + float2(offset.x, 0)).a;

    alphaSum += tex2D(uImage0, uv + float2(-offset.x, -offset.y)).a;
    alphaSum += tex2D(uImage0, uv + float2(offset.x, -offset.y)).a;
    alphaSum += tex2D(uImage0, uv + float2(-offset.x, offset.y)).a;
    alphaSum += tex2D(uImage0, uv + float2(offset.x, offset.y)).a;
    
    float density = alphaSum / (8.0 + intensity * 12.0);
    
    float interpolant = 1.0 - density;
    interpolant = smoothstep(0.0, 1.0, interpolant);
    
    if (interpolant < 0.01)
        return float4(0, 0, 0, 0);
    
    // pixelation pass
    float2 pixelatedUV = floor(uv * itemSize) / itemSize / 4.0;
    
    float2 scrollingUV = pixelatedUV + float2(time / 6.28f, time / 6.28f);
    
    float4 noise = tex2D(uImage1Sampler, (screenPos + scrollingUV) * scale);
    
    float iriOffset = (sin((noise.r + time * 0.5) * 6.2831) * 0.5) + 0.5;
    // iridecent noise texture
    float iridecence = tex2D(uImage2Sampler, scrollingUV + float2(0, iriOffset)).r;   
    //float4 gradientColor = tex2D(uImage3Sampler, float2(iriOffset, 0.5));
    
    float colorInterpolant = iridecence.r / 1.0;
    colorInterpolant = smoothstep(0.0, 1.0, colorInterpolant);
    
    float4 gradientColor = lerp(uColor1, uColor2, min(1, colorInterpolant));
    
    // clamps the intensity of the texture so the jump isnt harsh
    float clamp = lerp(0.2, 1.6, iridecence) * (1.0 + intensity);
    float4 finalColor = gradientColor * clamp;
    
    finalColor *= interpolant;
    finalColor.rgb *= interpolant;
    
    // make the edges a bit brighter in color
    if (interpolant > 0.75)
        finalColor.rgb *= 1.0 + 0.5 * (interpolant - 0.75) / 0.25;
    
    //finalColor.a = 0;
    
    float brightness = finalColor.r + finalColor.g + finalColor.b;
    
    // lerp brighter colors towards the shine color
    if (brightness > 1.0)
        finalColor = lerp(finalColor, uColor3, (brightness - 1.0) / 2.0);
    
    return finalColor;
}

technique Technique1
{
    pass mainPass
    {
        PixelShader = compile ps_3_0 main();
    }
}