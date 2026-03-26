sampler scarabTex : register(s0);
float4 sourceRect;
float2 resolution;
float time;
float sheenOpacityMultiplier; //0.15
float saturationBoost; //0.1
float shellColorShift;

texture sheenMasks;
sampler sheenTex = sampler_state
{
    Texture = <sheenMasks>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = POINT;
    minfilter = POINT;
};

matrix WorldViewProjection;
float rotation;
bool flip;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, WorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;

    output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

float2 Rotate(float2 coords, float2 origin)
{
    coords -= origin;
    coords *= resolution / sourceRect.zw;
    
    float2x2 rotate = float2x2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
    coords = mul(coords, rotate);
        
    coords /= resolution / sourceRect.zw;
    coords += origin;
    return coords;
}

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

float4 getSheen(float2 uv, sampler tex)
{
    float4 compositedColor = tex2D(tex, uv);
    //Red channel for the opacity of the iridescence, green channel for the hueshift offset
    float2 sheenInfo = tex2D(sheenTex, uv).rg;
    
    float2 frameUv = (uv * resolution - sourceRect.xy) / sourceRect.zw;
    float2 pixUv = floor(frameUv * sourceRect.zw * 2) / (sourceRect.zw * 2);
    
    float3 colorHsv = rgb2hsv(compositedColor.rgb);
    
    //Hueshift that is offset by the fresnel value (which makes it hueshift more towards the edges of the shell) and by a left-right gradient
    float hueShift = sin(sheenInfo.g * 3 + pixUv.x * 3 - time + colorHsv.b) * (1 + sheenInfo.g);
    //Step the hueshift for a quirkier pixelated effect, and boost the saturation a smidge
    colorHsv.r += floor(hueShift * 8) / 20;
    colorHsv.g += saturationBoost;
    
    float iridescenceOpacity = sheenOpacityMultiplier + sin(time * 2) * 0.05;
    compositedColor.rgb = alphablend(compositedColor, float4(hsv2rgb(colorHsv), sheenInfo.r * iridescenceOpacity)).rgb;
    
    //hue shift the shell again for when there's multiple scarabs at once!
    colorHsv = rgb2hsv(compositedColor.rgb);
    colorHsv.r += shellColorShift * sheenInfo.r;
    compositedColor.rgb = lerp(compositedColor.rgb, hsv2rgb(colorHsv), min(1, ceil(shellColorShift)));
    
    return compositedColor;
}

float4 MainPS(float2 uv : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    return getSheen(uv, scarabTex) * vertexColor;
}

float4 BallPS(VertexShaderOutput input) : COLOR0
{
    float2 rotatedUv = input.TextureCoordinates;
    rotatedUv *= sourceRect.zw / resolution;
    rotatedUv += sourceRect.xy / resolution;
    
    if (flip)
        rotatedUv.x = 1 - rotatedUv.x;
    
    rotatedUv = Rotate(rotatedUv, (sourceRect.xy + sourceRect.zw * 0.5) / resolution);
    return getSheen(rotatedUv, scarabTex) * input.Color;
}

technique BasicColorDrawing
{
    pass DefaultPass
    {
        PixelShader = compile ps_3_0 MainPS();
    }

    pass BallPass
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 BallPS();
    }
};