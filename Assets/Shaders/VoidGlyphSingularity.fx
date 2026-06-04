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

float4 main(float2 coords : TEXCOORD0) : COLOR0
{
    float2 targetUV = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float aspectRatio = uScreenResolution.x / uScreenResolution.y;
    
    float2 diff = coords - targetUV;
    // we need a circle
    diff.x *= aspectRatio;
    
    float dist = length(diff);
    dist /= uZoom.x;
    
    float distortionStrength = lerp(0.01, 0.02, uProgress / 0.5);
    float progressInterpolant;
    
    if (uProgress < 0.5)
    {
        progressInterpolant = uProgress / 0.5;
    }
    else
    {
        progressInterpolant = 1.0 - (uProgress - 0.5) / 0.5;
        
        distortionStrength = lerp(0.02, 0.0, 1.0 - progressInterpolant);
    }
    
    float pullStrength = smoothstep(0.2, 0.05, dist);
    pullStrength = pow(pullStrength, 3.0);
    
    float2 outwardDir = normalize(diff);
    float2 twistDir = float2(-outwardDir.y, outwardDir.x);
    
    float twistAmount = 1.0;
    float2 combinedPullDirection = normalize(outwardDir + (twistDir * twistAmount));
    
    float2 pullVector = combinedPullDirection * pullStrength * uProgress * distortionStrength * uIntensity * uZoom.x;
    
    pullVector.x /= aspectRatio;
    
    float2 distortedUV = coords;
    if (uProgress > 0)
        distortedUV -= pullVector * 1.2;
    
    float4 screen = tex2D(uImage0, distortedUV);
    
    float edgeGlow = 1.0 - smoothstep(distortionStrength * uIntensity, 0.025, dist);
    edgeGlow = pow(edgeGlow, 5.0);
    
    float3 ringColor = float3(1.0, 0.6, 0.2) * edgeGlow * 4.0;
    ringColor.rgb *= progressInterpolant;
    
    screen.rgb += ringColor * uProgress * uIntensity;
    
    float3 innerRingColor = float3(1.0, 0.2, 1.0) * edgeGlow * 2.0;
    innerRingColor.rgb *= progressInterpolant;
    
    screen.rgb += innerRingColor * uProgress * uIntensity;
    
    float blackHole = smoothstep(0.015 * progressInterpolant * uIntensity, 0.025 * progressInterpolant * uIntensity, dist);
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