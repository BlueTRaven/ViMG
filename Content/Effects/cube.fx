//#include "Macros.fxh"
#include "platform_defines.fxh"

DECLARE_TEXTURE(Texture, 0);
DECLARE_TEXTURE(TextureHeightFogMapDay, 1);
DECLARE_TEXTURE(TextureHeightFogMapNight, 2);
DECLARE_TEXTURE(TextureLightDepth, 3);

cbuffer Whatever : register(b0) 
{

	float4x4 World;
	float4x4 View;
	float4x4 WorldNormal;
	float4x4 WorldViewProjection;

	float3 TintColor;

	float3 WorldSize;
	float3 CubeSize;
	float2 MaxReachable;

	float AOStrength;
	float AmbientStrength;
	float SpecularStrength;

	float4x4 LightViewProjection;
	float3 LightPos;
	float3 LightColor;

	float3 CameraPos;

	float FogStart;
	float FogEnd;
	float HeightFogMapLerp;

	bool UseSourceRect;
	float2 SourceRectPos;
	float2 SourceRectFarPos;
	float2 TextureSize;
	float2 TexCoordOffset;

	bool EnableShadows;
	bool EnableFog;
}

struct Light 
{
	float3 Position;
	float Start;
	float3 Color;
	float End;
};

StructuredBuffer<Light> Lights : register(t4);

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
	float4 Position : SV_Position;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 PositionWS : TEXCOORD1;
	float3 PositionSS : TEXCOORD2;
	float3 Normal : TEXCOORD3;
	float AO : TEXCOORD4;
	float4 PositionLS : TEXCOORD5;
};

float Shadow(float4 positionLS, float3 normal, float3 lightDir);

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.PositionWS = mul(input.Position, World).xyz;
	output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color;
	output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	output.AO = input.AO;
	output.PositionLS = mul(float4(output.PositionWS, 1), LightViewProjection);
	
	if (UseSourceRect)
	{
		float2 xy = SourceRectPos / TextureSize;
		float2 wh = (SourceRectFarPos - SourceRectPos) / TextureSize;
		
		output.TexCoord = xy + (wh * input.TexCoord);
	}
	else output.TexCoord = input.TexCoord + TexCoordOffset;
		
	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
	float4 worldColor = SAMPLE_TEXTURE(Texture, input.TexCoord) * input.Color;

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
	
	//float closestLightStart;
	//float closestLightEnd;
	//float3 closestLightColor;
	//float closestDistance = 10000000;
	float3 sumLights = float3(0, 0, 0);
	for (int i = 0; i < 16; i++)
	{
		float distance = length(Lights[i].Position - input.PositionWS);
		
		float lightFactor = 1 - ((distance - Lights[i].Start) / (Lights[i].End - Lights[i].Start));
		lightFactor = clamp(lightFactor, 0, 1);
		sumLights += Lights[i].Color * lightFactor;
		/*if (length(Lights[i].Position - input.PositionWS) < closestDistance)
		{
			closestLightStart = Lights[i].Start;
			closestLightEnd = Lights[i].End;
			closestLightColor = Lights[i].Color;
			closestDistance = length(Lights[i].Position - input.PositionWS);
		}*/
	}
	
	//float lightFactor = 1 - ((closestDistance - closestLightStart) / (closestLightEnd - closestLightStart));
	//lightFactor = clamp(lightFactor, 0, 1);

	//ambient
	float3 ambientColor = LightColor * AmbientStrength;

	ambientColor += sumLights;
	ambientColor = clamp(ambientColor, float3(0, 0, 0), float3(1, 1, 1));
	
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
	
	finalColor.rgb *= TintColor;
	
	if (EnableFog > 0)
	{
		float percent = 1 - (max(4 * CubeSize.y, input.PositionWS.y) / (WorldSize.y * CubeSize.y));
		percent = clamp(percent, 0, 1);
		float4 worldHeightColorDay = SAMPLE_TEXTURE(TextureHeightFogMapDay, float2(0, percent));
		float4 worldHeightColorNight = SAMPLE_TEXTURE(TextureHeightFogMapNight, float2(0, percent));

		float4 lerpedColor = lerp(worldHeightColorDay, worldHeightColorNight, HeightFogMapLerp);
		
		finalColor = lerp(finalColor, lerpedColor, fogFactor);
	}
	
	if (EnableShadows > 0)
	{	
		float shadow = Shadow(input.PositionLS, input.Normal, lightDir);
		finalColor.rgb *= 1 - shadow;
	}
	
	return finalColor;
}

float Shadow(float4 positionLS, float3 normal, float3 lightDir)
{
	float3 projCoords = positionLS.xyz / positionLS.w;
	
	projCoords = projCoords * 0.5 + 0.5;
	
	float closestDepth = 1- SAMPLE_TEXTURE(TextureLightDepth, float2(projCoords.x, 1 - projCoords.y)).r;
	
	float currentDepth = projCoords.z;
	
	float bias = 0;//max(0.05 * (1 - dot(normal, lightDir)), 0.005);
	
	float shadow = currentDepth - bias > closestDepth ? 1.0 : 0.0;
	
	if (projCoords.z > 1.0)
		shadow = 0.0;
	
	return shadow;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_5_0 MainVS();
		PixelShader = compile ps_5_0 MainPS();
	}
};