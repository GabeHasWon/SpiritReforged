sampler uImage0 : register(s0); // The contents of the screen
sampler gradientMap : register(s1);
sampler distortionTex : register(s2);

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

//Constants
float brightnessTreshold = 0.2409972;
float satTreshold = 0.30747926;
float blueTreshold = 0.91689754;

float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    float distanceToCenter = length(float2(0.5, 0.5) - uv) * 1.4142;
    
    float darknessMultiplier = uProgress; //Used to avoid having the shader get too ugly when in broad daylight on the surface
    float drugIntensity = uIntensity; //Controls the strenght of the extra trippy psychedelic effects when spored
    
    //Distort the edges of the screen (based on "drug intensity")
    float distortionStrenght = pow(smoothstep(0.4, 1, distanceToCenter), 2);
    float4 distortionNoise = tex2D(distortionTex, uv * 0.4 + float2(uTime * 0.2, uTime * 0.2)) * tex2D(distortionTex, uv * 0.3 + float2(-uTime * 0.1, -uTime * 0.1));
    uv += (float2(0.5, 0.5) - distortionNoise.xy) * distortionStrenght * 0.056 * drugIntensity;
    
    float4 baseColor = tex2D(uImage0, uv);
    float4 output = baseColor;
    
    //Tint the image, by making it lerp to a gradient map
    float brightness = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
    float tintStrenght = uOpacity;
    output = lerp(output, tex2D(gradientMap, float2(baseColor.b, brightness)), tintStrenght);
    
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