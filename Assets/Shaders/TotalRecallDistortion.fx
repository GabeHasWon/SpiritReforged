sampler uImage0 : register(s0); // The contents of the screen.
sampler uImage1 : register(s1); // Up to three extra textures you can use for various purposes (for instance as an overlay).
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);

float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition; // The position of the camera.
float2 uTargetPosition; // The "target" of the shader, what this actually means tends to vary per shader.
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect; // Doesn't seem to be used, but included for parity.
float2 uZoom;

float4 main(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    
    float2 size = uImageSize1 / (uZoom.x * 2);
    uv = floor(coords * size) / size;

    float4 screenTarget = tex2D(uImage1, uv);
    float4 originalScreen = tex2D(uImage0, coords);

    float mask = max(screenTarget.r, max(screenTarget.g, screenTarget.b));

    // we use the color channels of our overlay for the variables tied to individual projectiles. The screen target is full of bloom textures for distance checks 
    // we have to unmultiply these values to get the raw 0 to 1 values back
    float2 data = screenTarget.gb / max(mask, 0.001);
    
    float distortionIntensity = data.x;
    float colorInterpolant = data.y;
    
    float3 tintColor = lerp(uColor, uSecondaryColor, colorInterpolant);

    float2 noiseCoords = frac(uv * 1.5 + float2(uTime * 0.05, uTime * 0.08));
    float4 noise = tex2D(uImage2, noiseCoords);
    
    noise *= distortionIntensity;

    float wobblyMask = mask + (noise.r - 0.5) * 0.2 * distortionIntensity;

    float threshold = 0.25;
    float edge = smoothstep(threshold, threshold + 0.06, wobblyMask);

    float dist = 1.0 - saturate((wobblyMask - threshold) / (1.0 - threshold));

    float2 ripple = (noise.rg - 0.5) * 0.03 * dist;
    ripple *= distortionIntensity;
    
    float4 screen = tex2D(uImage0, uv + ripple);

    float rim = pow(dist, 12.0);

    float3 overlay = tintColor * rim * 0.5;
    screen.rgb += overlay * distortionIntensity;

    return lerp(originalScreen, screen, edge);
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_3_0 main();
    }
}