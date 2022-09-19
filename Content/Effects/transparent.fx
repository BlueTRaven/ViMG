#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Diffuse : register(t0);

float4x4 World;
float4x4 ViewProjection;

float4 TintColor;
float AmbientStrength;

bool UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

VSOutputCube MainVS(in VSInputCube input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.PositionWS = mul(input.Position, World).xyz;
	output.Position = mul(float4(output.PositionWS, 1), ViewProjection);
	//output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color * TintColor;
	//output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	//output.AO = input.AO;
	//output.Depth = output.Position.zw;
	//output.DepthVS = output.Position.w;

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
	float4 diffuse = Diffuse.Sample(Sampler, input.TexCoord);
	return float4(diffuse.rgb * AmbientStrength, diffuse.a) * input.Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};