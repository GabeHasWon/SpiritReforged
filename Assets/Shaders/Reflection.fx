sampler uImage0 : register(s0);

texture mapTexture;
sampler map
{
    Texture = (mapTexture);
};

texture distortionTexture;
sampler distortion
{
    Texture = (distortionTexture);
};

texture tileTexture;
sampler tile
{
    Texture = (tileTexture);
};

float reflectionHeight;
float2 noiseStrength;
float2 noiseMult;
float fade;

float4 MainPS(float2 coords : TEXCOORD0, float4 ocolor : COLOR0) : COLOR0
{
    float4 finalColor = tex2D(uImage0, coords);
    float4 mapColor = tex2D(map, coords);
    float4 distortColor = tex2D(distortion, coords * noiseMult);
    float4 tileColor = tex2D(tile, coords);
    
    mapColor *= tileColor.a; //Strictly adhere to tile bounds

    float reflectionY = ((1 - mapColor.g) * 255) / reflectionHeight;
    float4 reflectedColor = tex2D(uImage0, coords - float2(0, reflectionY) + float2(distortColor.x * noiseStrength.x, distortColor.y * noiseStrength.y) * max(pow(1 - mapColor.g, fade), 0));

    return reflectedColor * ocolor * pow(mapColor.g, fade);
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
};