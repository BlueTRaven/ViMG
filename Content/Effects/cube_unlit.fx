#include "platform_defines.fxh"
#include "vertex_structs.fxh"

DECLARE_TEXTURE(Texture, 0);
DECLARE_TEXTURE(TextureHeightFogMapDay, 1);
DECLARE_TEXTURE(TextureHeightFogMapNight, 2);

float4x4 World;
float4x4 View;
float4x4 WorldNormal;
float4x4 WorldViewProjection;

float3 WorldSize;
float3 CubeSize;

float3 CameraPos;

float FogStart;
float FogEnd;
float HeightFogMapLerp;

bool UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

bool EnableFog;

VSOutputCube MainVS(in VSInputCube input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.PositionWS = mul(input.Position, World).xyz;
	output.PositionSS = mul(float4(output.PositionWS, 1), View);
	output.Color = input.Color;
	output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	output.AO = input.AO;
	//output.PositionLS = mul(input.Position, mul(World, LightViewProjection));

	output.DepthVS = output.Position.w;

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
	float4 worldColor = SAMPLE_TEXTURE(Texture, input.TexCoord);

	float distance = length(input.PositionWS - -CameraPos);
	float fogFactor = (distance - FogStart) / (FogEnd - FogStart);
	fogFactor = clamp(fogFactor, 0, 1);

	if (EnableFog > 0)
	{
		float percent = 1 - (max(4 * CubeSize.y, input.PositionWS.y) / (WorldSize.y * CubeSize.y));
		percent = clamp(percent, 0, 1);
		float4 worldHeightColorDay = SAMPLE_TEXTURE(TextureHeightFogMapDay, float2(0, percent));
		float4 worldHeightColorNight = SAMPLE_TEXTURE(TextureHeightFogMapNight, float2(0, percent));

		float4 lerpedColor = lerp(worldHeightColorDay, worldHeightColorNight, HeightFogMapLerp);

		worldColor = lerp(worldColor, lerpedColor, fogFactor) * input.Color;
	}

	return worldColor;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};