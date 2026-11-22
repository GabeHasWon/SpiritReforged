sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;

float4 uShaderSpecificData;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 alphablend(float4 background, float4 foreground)
{
    float4 result = background * (1 - foreground.a) + foreground * foreground.a;
    result.a = saturate(background.a + foreground.a);
    return result;
}


float3 rgb2hsv(float3 rgb)
{
    float r = rgb.r;
    float g = rgb.g;
    float b = rgb.b;
    
    float maxV = max(max(r, g), b);
    float minV = min(min(r, g), b);
    
    float h = 0;
    float s = 0;
    float v = maxV;
    
    float d = maxV - minV;
    
    if (maxV != 0)
    {
        s = d / maxV;
    }
    
    if (maxV == minV)
    {
        h = 0;
    }
    else
    {
        if (maxV == r)
        {
            h = (g - b) / d + (g < b ? 6 : 0);
        }
        else if (maxV == g)
        {
            h = (b - r) / d + 2;
        }
        else if (maxV == b)
        {
            h = (r - g) / d + 4;
        }
        
        h /= 6;
    }
    
    return float3(h, s, v);
}

float3 hsv2rgb(float3 hsv)
{
    float h = frac(hsv.r);
    float s = saturate(hsv.g);
    float v = saturate(hsv.b);
    
    float r = 0;
    float g = 0;
    float b = 0;
    
    if (s == 0)
    {
        r = v;
        g = v;
        b = v;
    }
    else
    {
        float h6 = h * 6;
        float i = floor(h6);
        float f = h6 - i;
        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));
        
        r = (i == 0 || i == 5) ? v : i == 4 ? t : i > 1 ? p : q;
        g = (i == 1 || i == 2) ? v : i == 0 ? t : i > 3 ? p : q;
        b = (i == 3 || i == 4) ? v : i == 2 ? t : i < 2 ? p : q;
    }
    
    return float3(r, g, b);
}

float4 MainPS(float2 uv : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float4 compositedColor = tex2D(uImage0, uv);
    
    float2 frameUv = (uv * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    float2 pixUv = floor(frameUv * uSourceRect.zw) / uSourceRect.zw;
    
    float3 colorHsv = rgb2hsv(compositedColor.rgb);
    
    //Hueshift that is offset by the brightness of the color value (which makes it hueshift more towards the darker parts of the sprite) and by a up-down gradient
    float hueShift = sin(colorHsv.b * 2 + pixUv.y * 2 - uTime + colorHsv.b) * (1 + colorHsv.b);
    
    //Step the hueshift for a quirkier pixelated effect
    colorHsv.r += floor(hueShift * 8) / 20;
    colorHsv.g += uSaturation;
    float iridescenceOpacity = uOpacity + sin(uTime * 2) * 0.1;
    
    compositedColor.rgb = alphablend(compositedColor, float4(hsv2rgb(colorHsv), iridescenceOpacity * compositedColor.a)).rgb;
    return compositedColor * vertexColor;
}

technique BasicColorDrawing
{
    pass IridescencePass
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};