sampler uImage0 : register(s0); // base texture

float4 uColor;

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 tex = tex2D(uImage0, uv);
    
    return tex * uColor;
}

technique Technique1
{
    pass mainPass
    {
        PixelShader = compile ps_3_0 main();
    }
}