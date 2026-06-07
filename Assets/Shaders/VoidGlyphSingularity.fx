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
    
    // our render target overlay
    float3 screenTarget = tex2D(uImage1, coords).rgb;
   
    float mask = screenTarget.r;
    
    if (mask <= 0.01)
        return tex2D(uImage0, coords);
    
    // we use the color channels of our overlay for the variables tied to individual projectiles. The screen target is full of bloom textures for distance checks 
    // we have to unmultiply these values to get the raw 0 to 1 values back
    float2 data = screenTarget.gb / max(mask, 0.001);
    
    float progress = data.x;
    float intensity = data.y;
    
    float2 offset = float2(10.0 / uScreenResolution.x, 10.0 / uScreenResolution.y);
    
    // we offset the mask by 10 pixels to get a vector for our direction for the black holes distortion and the black hole itself
    float maskRight = tex2D(uImage1, coords + float2(offset.x, 0)).r;
    float maskDown = tex2D(uImage1, coords + float2(0, offset.y)).r;
    
    float2 inwardDir = float2(maskRight - mask, maskDown - mask);
    float2 outwardDir = float2(0.0, -1.0);
    
    if (length(inwardDir) >= 0.0001)
    {
        outwardDir = -normalize(inwardDir);
    }
    
    // this creates a twisting distortion
    float2 twistDir = float2(-outwardDir.y, outwardDir.x);
    float twistAmount = 1.0;
    float2 combinedPullDirection = normalize(outwardDir + (twistDir * twistAmount));

    float distortionStrength = lerp(0.01, 0.02, progress / 0.5);
    float progressInterpolant;
    
    if (progress < 0.5)
    {
        progressInterpolant = progress / 0.5;
    }
    else
    {
        progressInterpolant = 1.0 - (progress - 0.5) / 0.5;
        distortionStrength = lerp(0.02, 0.0, 1.0 - progressInterpolant);
    }
    
    float pullStrength = pow(mask, 2.0);

    float2 pullVector = combinedPullDirection * pullStrength * progress * distortionStrength * intensity * uZoom.x;
    pullVector.x /= aspectRatio;
    
    float2 distortedUV = coords;
    if (progress > 0)
        distortedUV += pullVector * 1.5;
    
    float4 screen = tex2D(uImage0, distortedUV);
    
    // this is the drawing for the actual black hole itself, basically just bloom
    float edgeGlow = smoothstep(0.825, 0.9, mask) * smoothstep(1.0, 0.95, mask);
    edgeGlow = pow(edgeGlow, 5.0);
    
    float3 ringColor = float3(1.0, 1.0, 1.0) * edgeGlow * 2.0;
    ringColor.rgb *= progressInterpolant;
    screen.rgb += ringColor * intensity;
    
    float3 innerRingColor = float3(1.0, 0.2, 1.0) * edgeGlow * 5.0;
    innerRingColor.rgb *= progressInterpolant;
    
    screen.rgb += innerRingColor * intensity;
    
    float blackHole = 1.0 - smoothstep(0.75, 0.85, pow(mask, 2.0));
    screen.rgb *= blackHole;
    
    return screen;
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_3_0 main();
    }
}