#include "Macros.fxh"
#include "platform_defines.fxh"

sampler2D Texture : register(s0);
sampler2D TextureHeightFogMap : register(s1);

matrix World;
matrix View;
matrix WorldNormal;
matrix WorldViewProjection;

float3 WorldSize;
float3 CubeSize;
float2 MaxReachable;

float AOStrength;
float AmbientStrength;
float SpecularStrength;

float3 LightPos;
float3 LightColor;

float3 CameraPos;

float FogStart;
float FogEnd;

uint UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

float3 LightsPosition[16];
float LightsStart[16];
float LightsEnd[16];
float3 LightsColor[16];

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL0;
	float AO : TEXCOORD1;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 PositionWS : TEXCOORD1;
	float3 PositionSS : TEXCOORD2;
	float3 Normal : TEXCOORD3;
	float AO : TEXCOORD4;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.PositionWS = mul(input.Position, World).xyz;
	output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color;
	output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	output.AO = input.AO;
		
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
	float4 worldColor = tex2D(Texture, input.TexCoord) * input.Color;

	if (worldColor.a < 0.01)
		discard;
		
	float3 norm = normalize(input.Normal);
	float3 lightDir = normalize(LightPos - input.PositionWS);
	
	float WorldDistStart = (WorldSize.x / 2 * CubeSize.x) - 800;
	float WorldDistEnd = (WorldSize.x / 2 * CubeSize.x) - 400;
	
	//fog
	float3 centerHoriz = float3(WorldSize.x / 2 * CubeSize.x, input.PositionWS.y, WorldSize.z / 2 * CubeSize.z);
	float distWorldCenter = length(centerHoriz - input.PositionWS);
	float fogFactorWorldCenter = (distWorldCenter - WorldDistStart) / (WorldDistEnd - WorldDistStart);
	
	float distance = length(-CameraPos - input.PositionWS);
	float fogFactor = (distance - FogStart) / (FogEnd - FogStart);
	
	fogFactor = max(fogFactor, fogFactorWorldCenter);
	fogFactor = clamp(fogFactor, 0, 1);
	
	float closestLightStart;
	float closestLightEnd;
	float3 closestLightColor;
	float closestDistance = 10000000;
	for (int i = 0; i < 16; i++)
	{
		if (length(LightsPosition[i] - input.PositionWS) < closestDistance)
		{
			closestLightStart = LightsStart[i];
			closestLightEnd = LightsEnd[i];
			closestLightColor = LightsColor[i];
			closestDistance = length(LightsPosition[i] - input.PositionWS);
		}
	}
	
	float lightFactor = 1 - ((closestDistance - closestLightStart) / (closestLightEnd - closestLightStart));
	lightFactor = clamp(lightFactor, 0, 1);

	//ambient
	float3 ambientColor = LightColor * AmbientStrength;

	ambientColor = lerp(ambientColor, closestLightColor, lightFactor);
	
	//diffuse
	float diffDotToCam = max(dot(norm, lightDir), 0.0);
	float3 diffuseColor = diffDotToCam * LightColor;
	
	//specular
	float3 camDir = normalize(CameraPos - input.PositionWS);
	float3 reflectDir = reflect(camDir, norm);
	
	float specToCam = pow(max(dot(camDir, reflectDir), 0), 32);
	float3 specularColor = specToCam * LightColor * SpecularStrength;
		
	float4 finalColor = float4(ambientColor, 1.0) * worldColor;
	finalColor.rgb *= input.AO;
	
	float percent = 1 - (max(4 * CubeSize.y, input.PositionWS.y) / (WorldSize.y * CubeSize.y));
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