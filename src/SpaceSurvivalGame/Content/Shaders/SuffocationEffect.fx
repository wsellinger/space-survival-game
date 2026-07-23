#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D SpriteTextureSampler : register(s0);

// (0,0) disables pixelation entirely; otherwise the size, in UV units, of one pixelation block.
float2 PixelBlockSizeUV;

// 0 = full color, 1 = fully desaturated.
float GrayscaleIntensity;

// Aspect-corrected distance from screen center at which the vignette reaches full black;
// bigger than the screen's own extent means no visible darkening at all.
float VignetteRadius;
float VignetteFeatherRadius;

// (width/height, 1) so vignette distance is measured correctly on non-square viewports.
float2 AspectRatio;

// How many noise cells span the screen horizontally/vertically (screen size / grain size).
float2 NoiseCellCount;

// 0 = no noise, 1 = fully replaced/darkened per NoiseAdditiveBlend below.
float NoiseIntensity;

// 1 = additive: pixels blend toward random gray/white static (classic TV-static look).
// 0 = darkening grain: pixels are randomly and only ever darkened (film-grain look).
float NoiseAdditiveBlend;

// Changes every frame so the noise flickers instead of sitting as a fixed pattern.
float NoiseTimeSeed;

float Random(float2 value)
{
    return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453123);
}

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 uv = texCoord;

    if (PixelBlockSizeUV.x > 0.0001)
    {
        uv = (floor(uv / PixelBlockSizeUV) + 0.5) * PixelBlockSizeUV;
    }

    float4 sceneColor = tex2D(SpriteTextureSampler, uv) * color;

    float luminance = dot(sceneColor.rgb, float3(0.299, 0.587, 0.114));
    sceneColor.rgb = lerp(sceneColor.rgb, float3(luminance, luminance, luminance), GrayscaleIntensity);

    float noiseValue = Random(floor(texCoord * NoiseCellCount) + NoiseTimeSeed);
    float3 additiveNoise = lerp(sceneColor.rgb, float3(noiseValue, noiseValue, noiseValue), NoiseIntensity);
    float3 darkeningNoise = sceneColor.rgb * (1.0 - noiseValue * NoiseIntensity);
    sceneColor.rgb = lerp(darkeningNoise, additiveNoise, NoiseAdditiveBlend);

    float2 centered = (texCoord - 0.5) * AspectRatio;
    float distanceFromCenter = length(centered);
    float vignette = 1.0 - smoothstep(VignetteRadius - VignetteFeatherRadius, VignetteRadius, distanceFromCenter);
    sceneColor.rgb *= vignette;

    return sceneColor;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
