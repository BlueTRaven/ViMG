#include "platform_defines.fxh"

float4x4 WorldViewProjection;

struct VertexShaderInput
{
	float4 Position : POSITION;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 SamplePosition : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.Position = mul(input.Position, WorldViewProjection);
	output.SamplePosition = mul(input.Position, WorldViewProjection);

	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
	float depth = input.SamplePosition.z / input.SamplePosition.w;
	
	return float4(depth, depth, depth, 1);
}

technique Depth
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};