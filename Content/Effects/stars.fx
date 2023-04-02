#include "platform_defines.fxh"

float Time;
float3 CameraDirection;
float4x4 WorldMatrix;

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
    VertexShaderOutput output = (VertexShaderOutput) 0;

    output.Position = input.Position;
    output.TexCoord = input.TexCoord;

    return output;
}

// 3D Gradient noise from: https://www.shadertoy.com/view/Xsl3Dl
float3 hash(float3 p) // replace this by something better
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
			  dot(p, float3(269.5, 183.3, 246.1)),
			  dot(p, float3(113.5, 271.9, 124.6)));

    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
}
float noise(in float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
	
    float3 u = f * f * (3.0 - 2.0 * f);

    return lerp(lerp(lerp(dot(hash(i + float3(0.0, 0.0, 0.0)), f - float3(0.0, 0.0, 0.0)),
                          dot(hash(i + float3(1.0, 0.0, 0.0)), f - float3(1.0, 0.0, 0.0)), u.x),
                     lerp(dot(hash(i + float3(0.0, 1.0, 0.0)), f - float3(0.0, 1.0, 0.0)),
                          dot(hash(i + float3(1.0, 1.0, 0.0)), f - float3(1.0, 1.0, 0.0)), u.x), u.y),
                lerp(lerp(dot(hash(i + float3(0.0, 0.0, 1.0)), f - float3(0.0, 0.0, 1.0)),
                          dot(hash(i + float3(1.0, 0.0, 1.0)), f - float3(1.0, 0.0, 1.0)), u.x),
                     lerp(dot(hash(i + float3(0.0, 1.0, 1.0)), f - float3(0.0, 1.0, 1.0)),
                          dot(hash(i + float3(1.0, 1.0, 1.0)), f - float3(1.0, 1.0, 1.0)), u.x), u.y), u.z);
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    float3 texcoord_world = mul(float4(input.TexCoord, 1, 1), WorldMatrix).xyz;

    // Compute the stars
    float stars_threshold = 8.0f; // modifies the number of stars that are visible
    float stars_exposure = 200.0f; // modifies the overall strength of the stars
    float stars = pow(clamp(noise(texcoord_world * 200.0f), 0.0f, 1.0f), stars_threshold) * stars_exposure;
    stars *= lerp(0.4, 1.4, noise(texcoord_world * 100.0f + float3(Time, Time, Time))); // time based flickering

    return float4(float3(stars, stars, stars), 1.0);
}

technique T1
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};