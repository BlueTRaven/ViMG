#include "platform_defines.fxh"
#include "vertex_structs.fxh"

//To be rendered additively
sampler Sampler : register(s0);
Texture2D Position : register(t0);

float3 CameraPosition;
float2 FogExtents;

float4 FogColor;

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

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 position = Position.Sample(Sampler, input.TexCoord);

	float distance = length(position.xyz - CameraPosition);
	float fogFactor = (distance - FogExtents.x) / (FogExtents.y - FogExtents.x);
	fogFactor = saturate(fogFactor);

	return FogColor * fogFactor * position.w;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};