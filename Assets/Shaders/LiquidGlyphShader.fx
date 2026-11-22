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
    
    float noise = tex2D(uImage1Sampler, pixelatedUV * 1.5 + float2(0, -uTime * 0.4)).r;
    float noise2 = tex2D(uImage2Sampler, pixelatedUV * 3.0 + float2(0, -uTime * 0.8)).r;
    
    float combined = (noise * 0.6) + (noise2 * 0.4);
    
    float mask = smoothstep(0.2, 1.0, combined);
    
    float2 distortedUV = pixelatedUV;
    distortedUV.y -= (mask * uStrength);
    
    float4 tex = tex2D(uImage0, distortedUV);

    tex *= lerp(uColor1, uColor2, sqrt(mask) + noise.r * 0.1);
    
    tex *= sqrt(1.0 - uv.y);
     
    return tex;
}

technique Technique1
{
    pass mainPass
    {
        PixelShader = compile ps_3_0 main();
    }
}