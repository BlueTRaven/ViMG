#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Diffuse : register(t0);

float4x4 World;
float4x4 ViewProjection;

float SeaLevel;

float4 TintColor;
float AmbientStrength;

bool UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

VSOutputCube MainVS(in VSInputTransparent input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.PositionWS = mul(input.Position, World).xyz;
	output.Position = mul(float4(output.PositionWS, 1), ViewProjection);
	output.Color = input.Color * TintColor;

	if (UseSourceRect)
	{
		float2 xy = SourceRectPos / TextureSize;
		float2 wh = (SourceRectFarPos - SourceRectPos) / TextureSize;

		output.TexCoord = xy + (wh * input.TexCoord);
	}
	else output.TexCoord = input.TexCoord + TexCoordOffset;

	return output;
}

float4 MainPS(VSOutputCube input) : SV_TARGET
{
	float4 diffuse = (float4)0;
	if (input.PositionWS.y < SeaLevel)
		diffuse = Diffuse.Sample(Sampler, float2(0, 0));
	else diffuse = Diffuse.Sample(Sampler, input.TexCoord);

	return diffuse * input.Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};