#include "platform_defines.fxh"
#include "vertex_structs.fxh"

//https://learnopengl.com/Guest-Articles/2022/Phys.-Based-Bloom
// This shader performs downsampling on a texture,
// as taken from Call Of Duty method, presented at ACM Siggraph 2014.
// This particular method was customly designed to eliminate
// "pulsating artifacts and temporal stability issues".

float2 SrcResolution;

// Remember to add bilinear minification filter for this texture!
// Remember to use a floating-point texture format (for HDR)!
// Remember to use edge clamping for this texture!
sampler Sampler : register(s0);
Texture2D Texture : register(t0);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    output.Position = input.Position;
    output.TexCoord = input.TexCoord;

    return output;
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
    float2 srcTexelSize = 1.0 / SrcResolution;
    float x = srcTexelSize.x;
    float y = srcTexelSize.y;

    // Take 13 samples around current texel:
    // a - b - c
    // - j - k -
    // d - e - f
    // - l - m -
    // g - h - i
    // === ('e' is the current texel) ===

    float3 a = Texture.Sample(Sampler, float2(input.TexCoord.x - 2 * x, input.TexCoord.y + 2 * y)).rgb;
    float3 b = Texture.Sample(Sampler, float2(input.TexCoord.x, input.TexCoord.y + 2 * y)).rgb;
    float3 c = Texture.Sample(Sampler, float2(input.TexCoord.x + 2 * x, input.TexCoord.y + 2 * y)).rgb;

    float3 d = Texture.Sample(Sampler, float2(input.TexCoord.x - 2 * x, input.TexCoord.y)).rgb;
    float3 e = Texture.Sample(Sampler, float2(input.TexCoord.x, input.TexCoord.y)).rgb;
    float3 f = Texture.Sample(Sampler, float2(input.TexCoord.x + 2 * x, input.TexCoord.y)).rgb;

    float3 g = Texture.Sample(Sampler, float2(input.TexCoord.x - 2 * x, input.TexCoord.y - 2 * y)).rgb;
    float3 h = Texture.Sample(Sampler, float2(input.TexCoord.x, input.TexCoord.y - 2 * y)).rgb;
    float3 i = Texture.Sample(Sampler, float2(input.TexCoord.x + 2 * x, input.TexCoord.y - 2 * y)).rgb;

    float3 j = Texture.Sample(Sampler, float2(input.TexCoord.x - x, input.TexCoord.y + y)).rgb;
    float3 k = Texture.Sample(Sampler, float2(input.TexCoord.x + x, input.TexCoord.y + y)).rgb;
    float3 l = Texture.Sample(Sampler, float2(input.TexCoord.x - x, input.TexCoord.y - y)).rgb;
    float3 m = Texture.Sample(Sampler, float2(input.TexCoord.x + x, input.TexCoord.y - y)).rgb;

    float3 downsample = 0;

    // Apply weighted distribution:
    // 0.5 + 0.125 + 0.125 + 0.125 + 0.125 = 1
    // a,b,d,e * 0.125
    // b,c,e,f * 0.125
    // d,e,g,h * 0.125
    // e,f,h,i * 0.125
    // j,k,l,m * 0.5
    // This shows 5 square areas that are being sampled. But some of them overlap,
    // so to have an energy preserving downsample we need to make some adjustments.
    // The weights are the distributed, so that the sum of j,k,l,m (e.g.)
    // contribute 0.5 to the final color output. The code below is written
    // to effectively yield this sum. We get:
    // 0.125*5 + 0.03125*4 + 0.0625*4 = 1
    downsample = e * 0.125;
    downsample += (a + c + g + i) * 0.03125;
    downsample += (b + d + f + h) * 0.0625;
    downsample += (j + k + l + m) * 0.125;

    return float4(downsample, 1);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};