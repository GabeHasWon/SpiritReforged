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
float2 distortionScale;
float2 distortionStrength;
float distortionPower;
float fade;

float4 MainPS(float2 coords : TEXCOORD0, float4 ocolor : COLOR0) : COLOR0
{
    float4 mapColor = tex2D(map, coords);
    float4 distortionColor = tex2D(distortion, coords % distortionScale);
    float4 tileColor = tex2D(tile, coords);
    
    mapColor *= tileColor.a; //Strictly adhere to tile bounds

    float reflectionY = ((1 - mapColor.g) * 255) / reflectionHeight;
    float4 reflectedColor = tex2D(uImage0, coords - float2(0, reflectionY) + float2(distortionColor.x * distortionStrength.x, distortionColor.y * distortionStrength.y) * max(pow(1 - mapColor.g, fade * distortionPower), 0));

    return reflectedColor * ocolor * pow(mapColor.b, fade);
}

technique BasicColorDrawing
{
    pass MainPS
    {
        PixelShader = compile ps_2_0 MainPS();
    }
};