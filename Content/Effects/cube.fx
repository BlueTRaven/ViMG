#include "Macros.fxh"
#include "platform_defines.fxh"

sampler2D Texture : register(s0);
sampler2D TextureHeightFogMap : register(s1);

matrix World;
matrix View;
matrix WorldNormal;
matrix WorldViewProjection;

float AmbientStrength;
float SpecularStrength;

float3 LightPos;
float3 LightColor;

float3 CameraPos;

float3 FogColor;
float FogStart;
float FogEnd;

uint UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 P : TEXCOORD5;
	float3 PositionWS : TEXCOORD1;
	float3 PositionSS : TEXCOORD2;
	float3 Normal : TEXCOORD3;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.P = input.Position.xyz;
	output.PositionWS = mul(input.Position, World).xyz;
	output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color;
	output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
		
	if (UseSourceRect)
	{
		float2 xy = SourceRectPos / TextureSize;
		float2 wh = (SourceRectFarPos - SourceRectPos) / TextureSize;
		
		output.TexCoord = xy + (wh * input.TexCoord);
	}
	else output.TexCoord = input.TexCoord + TexCoordOffset;
		
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float3 norm = normalize(input.Normal);
	float3 lightDir = normalize(LightPos - input.PositionWS);
	
	//fog
	float distance = length(-CameraPos - input.PositionWS);
	float fogFactor = (distance - FogStart) / (FogEnd - FogStart);
	fogFactor = clamp(fogFactor, 0, 1);
	
	//ambient
	float3 ambientColor = LightColor * AmbientStrength;

	//diffuse
	float diffDotToCam = max(dot(norm, lightDir), 0.0);
	float3 diffuseColor = diffDotToCam * LightColor;
	
	//specular
	float3 camDir = normalize(CameraPos - input.PositionWS);
	float3 reflectDir = reflect(camDir, norm);
	
	float specToCam = pow(max(dot(camDir, reflectDir), 0), 32);
	float3 specularColor = specToCam * LightColor * SpecularStrength;
	
	float4 worldColor = tex2D(Texture, input.TexCoord) * input.Color;
	
	float4 finalColor = float4(ambientColor + diffuseColor + specularColor, 1.0) * worldColor;
	
	float percent = 1 - (max(4 * 20, input.PositionWS.y) / (512 * 20));
	percent = clamp(percent, 0, 1);
	float4 worldHeightColor = tex2D(TextureHeightFogMap, float2(0, percent));
	
	float4 fogModColor = lerp(finalColor, worldHeightColor, fogFactor);
	
	return fogModColor;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};