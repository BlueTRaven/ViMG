#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Diffuse : register(t0);
Texture2D Emissive : register(t1);

Texture2D Skybox : register(t2);

Texture2D WorldheightMapAmb	: register(t3);

float4x4 World;
float4x4 ViewProjection;

float3 CameraPosition;
float2 FogExtents;

float4 TintColor;
float AmbientStrength;

bool UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

struct PSOutputGBuffer
{
	float4 TransparentDiffuse	: COLOR0;
	//float4 Position				: COLOR1;
};

VSOutputCube MainVS(in VSInputCube input)
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

float4 PSNoFog(VSOutputCube input) : SV_TARGET
{
	float ambientWorldheight = WorldheightMapAmb.Sample(Sampler, float2(0.5, 1 - (input.PositionWS.y / (512.0 * 0.1)))).r;

	float4 diffuse = Diffuse.Sample(Sampler, input.TexCoord);
	float3 emissive = Emissive.Sample(Sampler, input.TexCoord).rgb;
	float4 emissiveColor = (diffuse * input.Color) * float4(emissive.rgb, 0);
	float4 finalColor = float4(diffuse.rgb * AmbientStrength * ambientWorldheight, diffuse.a) * input.Color;

	float4 output = finalColor + emissiveColor;

	return output;
}

float4 PSFog(VSOutputCube input) : SV_TARGET
{
	float ambientWorldheight = WorldheightMapAmb.Sample(Sampler, float2(0.5, 1 - (input.PositionWS.y / (512.0 * 0.1)))).r;

	float4 skybox = Skybox.Sample(Sampler, input.TexCoord);

	float distance = length(input.PositionWS - CameraPosition);
	float fogFactor = (distance - FogExtents.x) / (FogExtents.y - FogExtents.x);
	fogFactor = saturate(fogFactor);

	float4 fogColor = skybox * fogFactor;

	float4 diffuse = Diffuse.Sample(Sampler, input.TexCoord);
	float3 emissive = Emissive.Sample(Sampler, input.TexCoord).rgb;
	float4 emissiveColor = (diffuse * input.Color) * float4(emissive.rgb, 0);
	float4 finalColor = float4(diffuse.rgb * AmbientStrength * ambientWorldheight, diffuse.a) * input.Color;

	float4 output = (finalColor + emissiveColor) * fogColor;

	return output;
}

technique T0
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL PSNoFog();
	}
};

technique T1
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL PSFog();
	}
}