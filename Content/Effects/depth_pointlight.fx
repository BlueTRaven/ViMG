#include "platform_defines.fxh"

float4x4 World;
float4x4 ViewProjection;
float3 LightPosition;
float FarPlane;

struct VertexShaderInput
{
	float4 Position : POSITION;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 PositionWS : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	
	output.PositionWS = mul(input.Position, World);
	output.Position = mul(output.PositionWS, ViewProjection);

	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
	float distance = length(input.PositionWS.xyz - LightPosition);
	distance /= FarPlane;

	return float4(distance, distance, distance, 1);
}

technique Depth
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};