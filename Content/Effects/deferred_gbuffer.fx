#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Diffuse : register(t0);
Texture2D Specular : register(t1);
Texture2D Emissive : register(t2);

float4x4 World;
float4x4 View;
float4x4 WorldNormal;
float4x4 ViewProjection;

bool UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

float AmbientStrength;
float SpecularPower;

struct PSOutputGBuffer
{
	float4 Diffuse				: COLOR0;			//color/albedo rgb; Specular a
	float4 LightAccumulation	: COLOR1;	//ambient + emissive; light is accumulated after
	float4 Depth				: COLOR2;
	float4 Position				: COLOR3;
	float4 Normal				: COLOR4;
	float4 AO					: COLOR5;
};

VSOutputCube MainVS(in VSInputCube input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.PositionWS = mul(input.Position, World).xyz;
	output.Position = mul(float4(output.PositionWS, 1), ViewProjection);
	output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color;
	output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	output.AO = input.AO;
	output.Depth = output.Position.zw;
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

PSOutputGBuffer MainPS(VSOutputCube input)
{
	float4 albedoSample = Diffuse.Sample(Sampler, input.TexCoord);
	if (albedoSample.a < 0.1)
		discard;

	PSOutputGBuffer output = (PSOutputGBuffer)0;
	float3 ambient = albedoSample.rgb * AmbientStrength;
	float3 emissive = Emissive.Sample(Sampler, input.TexCoord).rgb * input.Color.rgb;

	float depth = input.DepthVS;

	output.Diffuse.rgb = albedoSample.rgb * input.Color.rgb;
	output.Diffuse.a = Specular.Sample(Sampler, input.TexCoord).r;

	output.LightAccumulation = float4(ambient + emissive, 1);

	output.Depth = float4(depth, depth, depth, 1.0);// float4(input.DepthVS, input.DepthVS, input.DepthVS, 1);
	output.Position = float4(input.PositionWS, 1);
	output.Normal = float4(normalize(input.Normal), 1);		
	output.AO = float4(input.AO, input.AO, input.AO, 1);
	
	return output;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};