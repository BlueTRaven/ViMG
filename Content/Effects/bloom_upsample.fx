#include "platform_defines.fxh"
#include "vertex_structs.fxh"

//https://learnopengl.com/Guest-Articles/2022/Phys.-Based-Bloom
// This shader performs upsampling on a texture,
// as taken from Call Of Duty method, presented at ACM Siggraph 2014.

float FilterRadius;

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
    // The filter kernel is applied with a radius, specified in texture
    // coordinates, so that the radius will vary across mip resolutions.
    float x = FilterRadius;
    float y = FilterRadius;

    // Take 9 samples around current texel:
    // a - b - c
    // d - e - f
    // g - h - i
    // === ('e' is the current texel) ===
    float3 a = Texture.Sample(Sampler, float2(input.TexCoord.x - x, input.TexCoord.y + y)).rgb;
    float3 b = Texture.Sample(Sampler, float2(input.TexCoord.x,     input.TexCoord.y + y)).rgb;
    float3 c = Texture.Sample(Sampler, float2(input.TexCoord.x + x, input.TexCoord.y + y)).rgb;

    float3 d = Texture.Sample(Sampler, float2(input.TexCoord.x - x, input.TexCoord.y)).rgb;
    float3 e = Texture.Sample(Sampler, float2(input.TexCoord.x,     input.TexCoord.y)).rgb;
    float3 f = Texture.Sample(Sampler, float2(input.TexCoord.x + x, input.TexCoord.y)).rgb;

    float3 g = Texture.Sample(Sampler, float2(input.TexCoord.x - x, input.TexCoord.y - y)).rgb;
    float3 h = Texture.Sample(Sampler, float2(input.TexCoord.x,     input.TexCoord.y - y)).rgb;
    float3 i = Texture.Sample(Sampler, float2(input.TexCoord.x + x, input.TexCoord.y - y)).rgb;

    float3 upsample = 0;

    // Apply weighted distribution, by using a 3x3 tent filter:
    //  1   | 1 2 1 |
    // -- * | 2 4 2 |
    // 16   | 1 2 1 |
    upsample = e * 4.0;
    upsample += (b + d + f + h) * 2.0;
    upsample += (a + c + g + i);
    upsample *= 1.0 / 16.0;

    return float4(upsample, 1);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};