#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Diffuse : register(t0);
Texture2D Emissive : register(t1);

float4x4 World;
float4x4 ViewProjection;

float4 TintColor;
float AmbientStrength;

Texture2D WorldheightMapAmb	: register(t2);

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
	float ambientWorldheight = WorldheightMapAmb.Sample(Sampler, float2(0.5, 1 - (input.PositionWS.y / (512.0 * 0.1)))).r;

	float4 diffuse = Diffuse.Sample(Sampler, input.TexCoord);
	float3 emissive = Emissive.Sample(Sampler, input.TexCoord).rgb;
	float4 finalColor = float4(diffuse.rgb * AmbientStrength * ambientWorldheight, diffuse.a) * input.Color;
	return finalColor + ((diffuse * input.Color) * float4(emissive.rgb, 0));
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};