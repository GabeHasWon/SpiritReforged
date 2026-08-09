sampler uImage0 : register(s0); // The contents of the screen
sampler gradientMap : register(s1);
sampler distortionTex : register(s2);
float2 tileTargetTopLeft;
float2 tileTargetBottomRight;

texture tileTarget;
sampler tileMask
{
    Texture = (tileTarget);
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = clamp;
    AddressV = clamp;
};


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
float2 sunPosition;
float sunIntensity;

float invlerp(float from, float to, float value)
{
    return saturate((value - from) / (to - from));
}

float2 invlerp(float2 from, float2 to, float2 value)
{
    return saturate((value - from) / (to - from));
}


float GetSunStrength(float2 uv)
{
    float2 zoomResolution = uScreenResolution * uZoom;
    float2 offsetToSun = (sunPosition / zoomResolution) - uv;
    offsetToSun.x *= uScreenResolution.x / uScreenResolution.y;
    float distanceToSun = invlerp(0.1 + sin(uTime * 0.2) * 0.03, 0.04, length(offsetToSun));
    distanceToSun = pow(distanceToSun, 2);
    
    float2 tileTargetUvStart = tileTargetTopLeft / zoomResolution;
    float2 tileTargetUvEnd = tileTargetBottomRight / zoomResolution;
    float2 tileTargetCoordinates = invlerp(tileTargetUvStart, tileTargetUvEnd, uv);
    float4 mask = tex2D(tileMask, tileTargetCoordinates).a;
    distanceToSun -= mask * 0.9;
    
    return max(0, distanceToSun);
}

float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    float distanceToCenter = length(float2(0.5, 0.5) - uv) * 1.4142;
    
    float sandstormStrength = uProgress * uOpacity;
    
    //Distort the edges of the screen (based on "drug intensity")
    float distortionStrenght = (pow(smoothstep(0.2, 1, distanceToCenter), 2) + uIntensity * 0.4) * pow(uOpacity, 0.5);
    float2 distortionUv = (uv * uScreenResolution) / 1000;
    
    float4 distortionNoise = tex2D(distortionTex, distortionUv + float2(uTime * 0.1, uTime * 0.1)) * tex2D(distortionTex, distortionUv * 0.4 + float2(-uTime * 0.1, -uTime * 0.1));
    uv += (float2(0.3, 0.3) - distortionNoise.xy) * distortionStrenght * 0.026;
    
    float4 baseColor = tex2D(uImage0, uv);
    
    float2 unitSide = float2(sin(uTime * 2) / uScreenResolution.x * (uIntensity * 2 + distanceToCenter * 1.5) * uZoom.x, 0);
    
    float chromaStrength = 0.1 * uIntensity + 0.5 * distanceToCenter;
    baseColor.r = lerp(baseColor.r, tex2D(uImage0, uv + unitSide * 2).r, chromaStrength);
    baseColor.gb = lerp(baseColor.gb, tex2D(uImage0, uv - unitSide * 2).gb, chromaStrength);
    float4 output = baseColor;
    
    //Tint the image, by making it lerp to a gradient map
    float brightness = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
    float tintStrenght = uOpacity * (0.23 + 0.1 * sin(uTime)) + uIntensity * 0.1 + sandstormStrength * 0.15;
    output = lerp(output, tex2D(gradientMap, float2(1 - uIntensity * 0.6, pow(1 - brightness, 1.5))), tintStrenght);
    
    output.r *= 1 + 0.1 * uOpacity + 0.1 * sandstormStrength;
    output.rgb += float3(1, 0.7, 0.3) * GetSunStrength(uv) * sandstormStrength * 0.7 * sunIntensity;
    
    output.a = 1;
    return output;
}

technique Technique1
{
    pass ScarabHeatHazePass
    {
        PixelShader = compile ps_3_0 Main();
    }
}