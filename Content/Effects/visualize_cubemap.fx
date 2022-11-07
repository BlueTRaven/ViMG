#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Position	: register(t0);
TextureCube Cubemap : register(t1);

float3 CubemapCenter;
float ProjectionFar;

float4x4 World;
float4x4 ViewProjection;

float linearize_depth(float d, float zNear, float zFar)
{
	return zNear * zFar / (zFar + d * (zNear - zFar));
}

VSOutputCube MainVS(in VSInputCube input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.PositionWS = mul(input.Position, World).xyz;
	output.Position = mul(float4(output.PositionWS, 1), ViewProjection);
	output.PositionSS = output.Position;
	output.Color = input.Color;

	return output;
}

float4 MainPS(VSOutputCube input) : SV_TARGET
{
	float2 texCoord = (input.PositionSS.xy / input.PositionSS.w) * 0.5 + 0.5;
	texCoord.y = 1 - texCoord.y;

	float3 position = Position.Sample(Sampler, texCoord).rgb;

	float3 dir = position - CubemapCenter;
	dir.y = -dir.y;

	float depth = linearize_depth(Cubemap.Sample(Sampler, dir).r, 0.001, ProjectionFar);
	return float4(input.Color.rgb * depth, 1);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};