texture sTexture;
sampler sTex = sampler_state {
    Texture = <sTexture>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = POINT; 
    minfilter = POINT;
};
texture sOutlineTexture;
sampler sOutlineTex = sampler_state
{
    Texture = <sOutlineTexture>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = POINT;
    minfilter = POINT;
};
texture piritTexture;
sampler piritTex = sampler_state
{
    Texture = <piritTexture>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = POINT;
    minfilter = POINT;
};
texture piritOutlineTexture;
sampler piritOutlineTex = sampler_state
{
    Texture = <piritOutlineTexture>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = POINT;
    minfilter = POINT;
};
texture reforgedTexture;
sampler reforgedTex = sampler_state
{
    Texture = <reforgedTexture>;
    AddressU = clamp;
    AddressV = clamp;
    magfilter = POINT;
    minfilter = POINT;
};
texture noiseTexture;
sampler noiseTex = sampler_state
{
    Texture = <noiseTexture>;
    AddressU = wrap;
    AddressV = wrap;
    magfilter = LINEAR;
    minfilter = LINEAR;
};

matrix WorldViewProjection;
float time;
float2 textureResolution; // 476 x 238
float2 sCenterCoordinates; // 120 x 120
float wobbleMult;

//Shared colors
float4 indigoOutline; // Color that outlines all the letters. There's also a pitch black underline 1px below every letter
float4 darkUnderlineColor; // By default, (0, 0, 0, 1)
float4 shadowColor; // By default, (0, 0, 0, 0.2)

// S colors
float4 sFillColor; // Fill color for the S. This should be white, but i'm making it configurable for more customizability
float4 sGradientTopColor; // Colors for the top/down color gradient on the spirit S's internal outline
float4 sGradientBottomColor;
float4 indigoOutlineGlowing; // The spirit S alternates between the regular indigo outline and this "glowing" color, making it look like its shining

// pirit colors
float4 fillColorBase; // Base fill color for the pirit letters
float4 fillColorSecondary; // Secondary fill color for the pirit letters, fading in towards the right side of the icon
float4 piritFillGlowColor; // Additive color layered ontop of the inner fill for the pirit letters
float4 piritOutlineBaseColor; // Base color for the inner outline of the letters
float4 innerOutlineGlowColor; // Additive color layered ontop of the inner outline. Affected by the glowing noise

// Reforged colors
float4 reforgedColorLeft; // Colors for the L/R gradient on reforged
float4 reforgedColorRight;


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


float invlerp(float from, float to, float value)
{
    // Utils.GetLerpValue()
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float4 alphaBlend(float4 bottomLayer, float4 topLayer)
{
    float4 returnColor = bottomLayer * (1 - topLayer.a) + topLayer * topLayer.a;
    returnColor.a = min(1, max(bottomLayer.a, topLayer.a));
    return returnColor;
}

float2 rotateUv(float2 uv, float rotateBy)
{
    // UVs need to be properly squished before useage since the sprite isn't a square
    float cosAngle = cos(rotateBy);
    float sinAngle = sin(rotateBy);
    return float2(uv.x * cosAngle + uv.y * -sinAngle, uv.x * sinAngle + uv.y * cosAngle);
}

float ditherPattern(float2 coords, float opacity)
{
    //Dither is done in 2x2
    float2 resScaled = floor(coords * textureResolution * 0.5);
    
    if (opacity > 0.55)
        return 1;
    if (opacity > 0.25)
        return 0.5 + 0.5 * (step(1, (resScaled.y + resScaled.x) % 2));

    return 0.5 + 0.5 * (step(1, resScaled.y % 2) * step(1, resScaled.x % 2)); // Sparse dither
}

// This grabs some twirly noise centered around the S, making it look like energy is swirling out from it
float getNoiseValue(float2 uv, float2 sOffset)
{
    float2 sUv = uv - sOffset;
    
    // Get radial UVs based on distance and angle to the central S
    float distToS = length(sUv * float2(textureResolution.x / textureResolution.y, 1)) * 0.75;
    float2 radialUv = float2(distToS, atan2(sUv.y, sUv.x) * 0.1);
    radialUv.x -= time * 0.05; // Scroll the noise outwards from the center
    radialUv.y += distToS * 0.5 - time * 0.1; // Make it spin around and add a "twist" to it with the distToS addition. So swirlful...
    
    // Get the noise and fade it when far enough away
    float noise = tex2D(noiseTex, radialUv).x;
    noise += invlerp(0.9, -0.4, distToS);
    noise *= invlerp(1.1, 0.6, distToS);
    
    //Curve the resulting value to avoid having too many low tones
    noise = pow(noise, 1.4);
    return noise;
}


float4 getPirit(float2 uv, float2 sUv, float2 sOffset)
{
    float2 pixelRes = textureResolution * 0.5; // 2x2 resolution
    
    //UVs of the pirit have a ghooOOoooOOOoooostly wave effect with a sine
    float2 piritUv = uv;
    piritUv.y += sin(floor(piritUv.x * pixelRes.x) / pixelRes.x * 5 + time) * 0.01 * wobbleMult;
    float4 pirit = tex2D(piritTex, piritUv);
    float whiteMask = step(0.5, pirit.g); // Mask that separates the black outline (that we will turn indigo) and the white fill)
    float4 outline = tex2D(piritOutlineTex, piritUv); //Inner outline texture

     //Get shadow and dark underline
    float2 darkUnderlineUv = piritUv - float2(0, 2 / textureResolution.y); //2 pixels offset for the dark outline under the letters
    float2 shadowUv = piritUv - float2(0, 10 / textureResolution.y); //10 pixels below for the shadow
    float4 darkUnderline = tex2D(piritTex, darkUnderlineUv).a * darkUnderlineColor;
    float4 shadow = tex2D(piritTex, shadowUv).a * shadowColor;
    
    //Get the distance to the center of the S (Multiplication needed to get uniform squish)
    float distToS = length(sUv * float2(textureResolution.x / textureResolution.y, 1)) * 0.75;
     
     //We start by adding in the shadow and the dark underline (easy peasy, its litterally just black)
    float4 returnValue = float4(0, 0, 0, pirit.a);
    returnValue = alphaBlend(shadow, returnValue);
    returnValue = alphaBlend(darkUnderline, returnValue);
     
     // Add in the indigo outline, that one doesn't get affected by the glow so we don't have to worry about it
    returnValue.rgb += (1 - whiteMask) * (indigoOutline) * pirit.a;
     
     // Grab noise for extra glowing parts on the letters. Pixelize the UVs so the noise is consistent
    float2 noiseUv = floor(piritUv * pixelRes) / pixelRes;
    float noise = getNoiseValue(noiseUv, sOffset);
     //Letter glow is determined by a straight gradient from the spirit S, and from the swirly noise emanating from the S. It gets dithered for swag points
    float glowStrenght = invlerp(0.8, 0., distToS) * 0.0 + noise * 0.6;
    glowStrenght *= ditherPattern(piritUv, glowStrenght);
     
     // The fill color gets tinted towards the right of the logo, then we add in the glow color from the noise
    float4 usedFillColor = lerp(fillColorBase, fillColorSecondary, invlerp(0.3, 1, distToS));
    usedFillColor.rgb += piritFillGlowColor.rgb * glowStrenght;
    
    returnValue.rgb += whiteMask * usedFillColor;
     
     // Add the brighter inner outline, which is also affected by the glowing effects
    float4 outlineColor = outline * piritOutlineBaseColor;
    outlineColor.rgb += innerOutlineGlowColor.rgb * glowStrenght * 0.2;
     
    returnValue = alphaBlend(returnValue, outlineColor);
    return returnValue;
}

float4 getReforged(float2 uv)
{
     //UVs of the reforged have a wave effect as well, with a sine, but this time the letters move as wholes
    float2 reforgedUv = uv;
    float letter = floor(reforgedUv.x * 13.6) / 13.6; //If we split the canvas in 13.6 sections, each letter has its own section
    reforgedUv.y += sin(letter * 5 + time + 0.4) * 0.01 * wobbleMult;
    reforgedUv.x += sin(letter * 5 + time * 1.3) * 0.002;
    float4 reforged = tex2D(reforgedTex, reforgedUv);
      //Get shadow and dark underline
      
    float2 darkUnderlineUv = reforgedUv - float2(0, 2 / textureResolution.y); //2 pixels offset for the dark outline under the letters
    float4 darkUnderline = tex2D(reforgedTex, darkUnderlineUv) * darkUnderlineColor;
    float2 shadowUv = reforgedUv - float2(0, 6 / textureResolution.y); //6 pixels below for the shadow (Makesi t look like reforged is hovering closer to the "wall"
    float4 shadow = tex2D(reforgedTex, shadowUv).a * shadowColor;
    
    float4 returnValue = float4(0, 0, 0, reforged.a);
    returnValue = alphaBlend(shadow, returnValue);
    returnValue = alphaBlend(darkUnderline, returnValue);
    returnValue += reforged.a * lerp(reforgedColorLeft, reforgedColorRight, invlerp(-0.5, 0.8, uv.x));
    return returnValue;
}

float4 getS(float2 uv)
{
    float4 s = tex2D(sTex, uv);
    float4 innerTint = tex2D(sOutlineTex, uv);
    float whiteMask = step(0.5, s.g); // Mask that separates the black outline (that we will turn indigo) and the white fill)
    
    //Get shadow and dark underline
    float2 darkUnderlineUv = uv - float2(0, 2 / textureResolution.y); //2 pixels offset for the dark outline under the S
    float2 shadowUv = uv - float2(0, 10 / textureResolution.y); //10 pixels below for the shadow
    float4 darkUnderline = tex2D(sTex, darkUnderlineUv).a * darkUnderlineColor;
    float4 shadow = tex2D(sTex, shadowUv).a * shadowColor;
    
    //We start by adding in the shadow and the dark underline 
    float4 returnValue = float4(0, 0, 0, s.a);
    returnValue = alphaBlend(shadow, returnValue);
    returnValue = alphaBlend(darkUnderline, returnValue);
    
    //Inside of the S is pure glowing white (By default)
    returnValue.rgb += whiteMask * sFillColor;
    //The internal outline is a gradient from top to bottom. Uv.y has got to be remapped to go from the top to bottom of the S
    returnValue = alphaBlend(returnValue, innerTint.a * lerp(sGradientTopColor, sGradientBottomColor, invlerp(0.1765, 0.8235, uv.y)));
    
    //Blue indigo outline which alternates between the usual indigo and a brighter color since the S is all glowy
    float4 outlineColor = lerp(indigoOutline, indigoOutlineGlowing, 0.6 - 0.4 * sin(time));
    returnValue.rgb += (1 - whiteMask) * s.a * outlineColor;
    return returnValue;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;
    //Transform the UVs centered around the center of the big spirit S
    float2 sOffset = sCenterCoordinates / textureResolution;
    float2 sUv = uv - sOffset;
    
    //Get all the color and such for the "pirit" part of the logo, using the centered UVs of the S
    float4 pirit = getPirit(uv, sUv, sOffset);
    float4 reforged = getReforged(uv);
    
    //Adjust S scale
    sUv *= 1 + 0.02 * sin(time) * wobbleMult;
     //Adjust S rotation
    sUv.y /= textureResolution.x / textureResolution.y;
    sUv = rotateUv(sUv, sin(time * 0.5) * 0.03 * wobbleMult);
    sUv.y *= textureResolution.x / textureResolution.y;
    //Recenter S UVs properly
    sUv += sOffset;
   
    float4 s = getS(sUv);
    
    float4 returnColor = alphaBlend(pirit, reforged);
    returnColor = alphaBlend(returnColor, s);
    
    return returnColor;
}

technique BasicColorDrawing
{
    pass GeometricStyle
	{
        PixelShader = compile ps_3_0 MainPS();
    }
};