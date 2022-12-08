#include "platform_defines.fxh"
#include "vertex_structs.fxh"

//To be rendered additively
sampler Sampler : register(s0);
Texture2D Position : register(t0);
Texture2D Color : register(t1);
Texture2D Height : register(t2);

float3 CameraPosition;
float2 FogExtents;

//x: start, y: end, z: time
float3 ColorInterpolate;

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

float4 MainPS1(VertexShaderOutput input) : COLOR
{
	float4 position = Position.Sample(Sampler, input.TexCoord);

	float distance = length(position.xyz - CameraPosition);
	float fogFactor = (distance - FogExtents.x) / (FogExtents.y - FogExtents.x);
	fogFactor = saturate(fogFactor);

	//float u = (ColorInterpolate.z - ColorInterpolate.x) / (ColorInterpolate.y - ColorInterpolate.x);
	float u1 = ColorInterpolate.x / 128.0;
	float u2 = ColorInterpolate.y / 128.0;

	float4 fogColor1 = Color.Sample(Sampler, float2(u1, 0));
	float4 fogColor2 = Color.Sample(Sampler, float2(u2, 0));

	return lerp(fogColor1, fogColor2, ColorInterpolate.z) * fogFactor * position.w;
}

float4 MainPS2(VertexShaderOutput input) : COLOR
{
	float4 position = Position.Sample(Sampler, input.TexCoord);
	float4 skybox = Color.Sample(Sampler, input.TexCoord);

	float distance = length(position.xyz - CameraPosition);
	float fogFactor = (distance - FogExtents.x) / (FogExtents.y - FogExtents.x);
	fogFactor = saturate(fogFactor);

	return skybox * fogFactor;
}

technique T1
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS1();
	}
};

technique T2
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS2();
	}
};