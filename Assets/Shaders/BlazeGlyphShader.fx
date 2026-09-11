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

texture uImage2; // Base Noise Texture
sampler2D uImage2Sampler = sampler_state
{
    texture = <uImage2>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float uTime;
float uStrength;
float uPixelRes;

float4 uColor1;
float4 uColor2;

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 pixelatedUV = uv;
    if (uPixelRes > 0)
    {
        pixelatedUV = floor(uv * uPixelRes) / uPixelRes;
     
        pixelatedUV += (0.5 / uPixelRes);
    }
    
    float noise = tex2D(uImage1Sampler, pixelatedUV + float2(0, uTime)).r - 0.5;
    float noise2 = tex2D(uImage2Sampler, (pixelatedUV + float2(uTime / 4, uTime * 2)) * 2).b - 0.5;

    float2 distortedUV = pixelatedUV + (noise * uStrength) + (noise2 * uStrength);
    
    float colorUV = tex2D(uImage1Sampler, pixelatedUV + float2(uTime / 6.28f, uTime / 6.28f)).r;
    float interpolant = colorUV / 1.0;
    float4 applyColor = lerp(uColor1, uColor2, interpolant);
       
    float4 tex = tex2D(uImage0, distortedUV);

    tex *= applyColor;
     
    return tex;
}

technique Technique1
{
    pass mainPass
    {
        PixelShader = compile ps_3_0 main();
    }
}