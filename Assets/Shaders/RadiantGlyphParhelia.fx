sampler uImage0 : register(s0); // Star texture

texture uImage1; // Distortion noise texture
sampler2D uImage1Sampler = sampler_state
{
    texture = <uImage1>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float ringRadius;
float ringThickness;
float ringOpacity;

float2 scale; // idek what to call these
float2 scaleTwo;

float2 outerStarScale;

float4 uColor;

float time;
float distortionStrength;

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float noise = tex2D(uImage1Sampler, uv + float2(0.0, time)).r - 0.5;

    float2 distortedUV = uv + (noise * distortionStrength);
    
    // scale the star
    float2 starUV = ((distortedUV - 0.5) * scale) + 0.5;
    float4 starTex = tex2D(uImage0, starUV);  
    
    // draw two stars
    float2 starUV_2 = ((distortedUV - 0.5) * scaleTwo) + 0.5;
    float4 starTex_2 = tex2D(uImage0, starUV_2);
    
    starTex *= uColor;
    starTex_2 *= uColor * 0.1;
    
    float2 center = float2(0.5, 0.5);
    float dist = distance(distortedUV, center);
    
    // calculates a ring using the distance from the center
    // fades the ring in the four corners using the polar angle from the center
    
    float inner = smoothstep(ringRadius - ringThickness / 5, ringRadius, dist);
    
    float distOutward = max(0.0, dist - ringRadius);
    float density = 30.0;
    float outerEdge = exp(-distOutward * density);

    float ring = inner * outerEdge;
    
    float4 ringColor = uColor * ring;
    
    float yDist = abs(distortedUV.y - 0.5);
    float xDist = abs(distortedUV.x - 0.5);

    float angle = atan2(distortedUV.y - 0.5, distortedUV.x - 0.5);

    float polarMask = cos(angle * 2.0);

    polarMask = (polarMask * 0.5) + 0.5;

    polarMask = lerp(0.01, 1.2, polarMask);
 
    ringColor *= pow(polarMask, 4.0);
    ringColor *= ringOpacity;
    
    float2 leftUV = ((distortedUV - 0.5 - float2(-ringRadius, 0.0)) * outerStarScale) + 0.5;
    float2 rightUV = ((distortedUV - 0.5 - float2(ringRadius, 0.0)) * outerStarScale) + 0.5;
    
    float4 leftStar = tex2D(uImage0, leftUV);
    float4 rightStar = tex2D(uImage0, rightUV);
    
    leftStar *= uColor;
    rightStar *= uColor;
    
    float outerFade = smoothstep(ringRadius - 0.05, ringRadius, dist);
    leftStar *= outerFade;
    rightStar *= outerFade;
    
    return starTex + starTex_2 + ringColor + leftStar + rightStar;
}

technique Technique1
{
    pass mainPass
    {
        PixelShader = compile ps_3_0 main();
    }
}