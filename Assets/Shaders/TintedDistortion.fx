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

// I honestly have no clue how bad for performance all this math is
// render target should help

float4 main(float2 coords : TEXCOORD0) : COLOR0
{
    float aspectRatio = uScreenResolution.x / uScreenResolution.y;
    
    float2 uv = coords;
    
    //float2 size = uImageSize1 / (uZoom.x * 2.0);
    //uv = floor(coords * size) / size;

    // our render target overlay
    float4 screenTarget = tex2D(uImage1, uv).rgba;
    float4 originalScreen = tex2D(uImage0, coords);
   
    float mask = screenTarget.g;
    
    // if nothing should be rendered return
    if (mask <= 0.01)
        return originalScreen;
    
    // we use the color channels of our overlay for the variables tied to individual projectiles. The screen target is full of bloom textures for distance checks 
    // we have to unmultiply these values to get the raw 0 to 1 values back
    float progress = screenTarget.r / max(mask, 0.001);
    uint packedColor = screenTarget.b;

    float2 noiseCoords = uv + float2(uTime * 0.05, uTime * 0.08);
    float4 noise = tex2D(uImage2, noiseCoords);

    float2 distortedUV = uv + float2(noise.r * 0.3, noise.r * 0.3) * 0.02;
    
    float4 screen = tex2D(uImage0, lerp(distortedUV, uv, progress));
    
    // decode our packed color
    uint r = (packedColor >> 5) & 0x07;
    uint g = (packedColor >> 2) & 0x07;
    uint b = packedColor & 0x03;
    
    float4 tintedNoise = noise * float4(r, g, b, 1);
    
    screen.rgb += tintedNoise * 0.1;
    // interpolate from our distortion to the original screen smoothly
    // interpolating to float4(0,0,0,0) doesn't work because it wouldn't draw the original screen (appearing as black instead)
    screen = lerp(screen, originalScreen, 1 - mask);
    
    return screen;
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_3_0 main();
    }
}