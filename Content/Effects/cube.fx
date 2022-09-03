//#include "Macros.fxh"
#include "platform_defines.fxh"

#define CASCADE_COUNT 16

DECLARE_TEXTURE(Texture, 0);
DECLARE_TEXTURE(TextureHeightFogMapDay, 1);
DECLARE_TEXTURE(TextureHeightFogMapNight, 2);
DECLARE_TEXTURE(TextureLightDepth, 3);
//DECLARE_TEXTURE_3D(TexturesLightDepth, 5, CASCADE_COUNT);
Texture2DArray<float4> TexturesLightDepth : register(t5); \
sampler TexturesLightDepthSampler : register(s5);

float4x4 World;
float4x4 View;
float4x4 WorldNormal;
float4x4 WorldViewProjection;
float FarPlane;

float3 TintColor;

float3 WorldSize;
float3 CubeSize;
float2 MaxReachable;

float3 AmbientColor;
float AmbientStrength;
float SpecularStrength;

int CascadePlanesUsed;
float CascadePlaneDistances[16];
float4x4 LightViewProjection;
float4x4 LightViewProjections[CASCADE_COUNT];
float3 LightDirection;
float3 LightColor;
float2 LightResolution;

float3 LightPos;

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
bool EnablePCF;

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

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.PositionWS = mul(input.Position, World).xyz;
	output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color;
	output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	output.AO = input.AO;
	output.PositionLS = mul(input.Position, mul(World, LightViewProjection));
	
	if (UseSourceRect)
	{
		float2 xy = SourceRectPos / TextureSize;
		float2 wh = (SourceRectFarPos - SourceRectPos) / TextureSize;
		
		output.TexCoord = xy + (wh * input.TexCoord);
	}
	else output.TexCoord = input.TexCoord + TexCoordOffset;
		
	return output;
}

float SampleWithOffset(float2 baseUV, float u, float v, float2 inv, int layer, float z)
{
	float2 uv = baseUV + float2(u, v) * inv;

	float sampledDepth = TexturesLightDepth.Sample(TexturesLightDepthSampler, float3(uv, layer)).r;

	float shadow = z < sampledDepth ? 1.0 : 0.0;

	return shadow;
}

float DirectionalShadowFuncCSM(float3 fragPosWorldSpace, float3 normal, float3 lightDir)
{
	float4 fragPosViewSpace = mul(float4(fragPosWorldSpace, 1.0), View);
	float depthValue = abs(fragPosViewSpace.z);

	int layer = -1;
	for (int i = 0; i < CascadePlanesUsed; ++i)
	{
		if (depthValue < CascadePlaneDistances[i])
		{
			layer = i;
			break;
		}
	}
	if (layer == -1)
	{
		layer = CascadePlanesUsed;
	}

	float4 csmFragPosLightSpace = mul(float4(fragPosWorldSpace, 1.0), LightViewProjections[layer]);

	float3 projectedTexCoords = csmFragPosLightSpace.xyz / csmFragPosLightSpace.w;
	projectedTexCoords.xy = (projectedTexCoords.xy * 0.5) + 0.5;
	projectedTexCoords.y = 1 - projectedTexCoords.y;

	if (projectedTexCoords.z > 1.0)
		return 0.0;
	//float bias = 0.005;
	float bias = max(0.006 * (1.0 - dot(normal, -lightDir)), 0.0005);

	//float closestDepth = TexturesLightDepth.Sample(TexturesLightDepthSampler, float3(projectedTexCoords.xy, layer)).r;//SAMPLE_TEXTURE(TexturesLightDepth, float3(projectedTexCoords.xy, layer)).r;
	float currentDepth = projectedTexCoords.z - bias;

	float2 uv = projectedTexCoords.xy * LightResolution;
	float2 invLightResolution = 1.0 / LightResolution;

	float2 baseUv;
	baseUv.x = floor(uv.x + 0.5);
	baseUv.y = floor(uv.y + 0.5);

	float s = (uv.x + 0.5 - baseUv.x);
	float t = (uv.y + 0.5 - baseUv.y);

	baseUv -= float2(0.5, 0.5);
	baseUv *= invLightResolution;

	float uw0 = (3 - 2 * s);
	float uw1 = (1 + 2 * s);

	float u0 = (2 - s) / uw0 - 1;
	float u1 = s / uw1 + 1;

	float vw0 = (3 - 2 * t);
	float vw1 = (1 + 2 * t);

	float v0 = (2 - t) / vw0 - 1;
	float v1 = t / vw1 + 1;

	float sum = 0;
	sum += uw0 * vw0 * SampleWithOffset(baseUv, u0, v0, invLightResolution, layer, currentDepth);
	sum += uw1 * vw0 * SampleWithOffset(baseUv, u1, v0, invLightResolution, layer, currentDepth);
	sum += uw0 * vw1 * SampleWithOffset(baseUv, u0, v1, invLightResolution, layer, currentDepth);
	sum += uw1 * vw1 * SampleWithOffset(baseUv, u1, v1, invLightResolution, layer, currentDepth);

	return sum * 1.0f / 16.0;

	//float shadow = currentDepth < closestDepth ? 1.0 : 0.0;

	//return shadow;
}

float DirectionalShadowFunc(float4 fragPosLightSpace, float3 normal, float3 lightDir)
{
	float3 projectedTexCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
	projectedTexCoords.xy = (projectedTexCoords.xy * 0.5) + 0.5;
	projectedTexCoords.y = 1 - projectedTexCoords.y;

	if (projectedTexCoords.z > 1.0)
		return 0.0;

	float closestDepth = SAMPLE_TEXTURE(TextureLightDepth, projectedTexCoords.xy).r;
	float currentDepth = projectedTexCoords.z;

	//float bias = max(0.05 * (1.0 - dot(float3(-normal.x, normal.y, -normal.z), lightDir)), 0.005);
	float bias = max(0.005 * (1.0 - dot(lightDir, normal)), 0.00005);

	float shadow = 0;
	//float2 sampleSize = 1.0 / float2(1024, 1024);
	//const int PCF_SAMPLE_COUNT = 1;
	//const float PCF_POW = 9;

	//for (int x = -1; x <= 1; ++x)
	//{
	//	for (int y = -1; y <= 1; ++y)
	//	{
	//		float pcfDepth = SAMPLE_TEXTURE(TextureLightDepth, projectedTexCoords.xy + (float2(x, y) * sampleSize)).r;
	//		shadow += currentDepth - bias < pcfDepth ? 1.0 : 0.0;
	//	}
	//}

	//shadow /= 9;

	shadow = currentDepth - bias < closestDepth ? 1.0 : 0.0;

	return shadow;
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
	float4 worldColor = SAMPLE_TEXTURE(Texture, input.TexCoord) * input.Color;

	if (worldColor.a < 0.01)
		discard;
		
	float3 norm = normalize(input.Normal);
	float3 lightDir = normalize(LightPos - input.PositionWS);
	
	//Units are calculated in world space.
	//Start: distance from world center where gradient starts fading in.
	//End: distance from world center where gradient ends and turns into solid color.
	float WorldDistStart = (WorldSize.x / 2 * CubeSize.x) - (64 * CubeSize.x);
	float WorldDistEnd = (WorldSize.x / 2 * CubeSize.x) - (72 * CubeSize.x);
	
	//fog
	float3 centerHoriz = float3(WorldSize.x / 2 * CubeSize.x, input.PositionWS.y, WorldSize.z / 2 * CubeSize.z);
	float distWorldCenter = length(centerHoriz - input.PositionWS);
	float fogFactorWorldCenter = (distWorldCenter - WorldDistEnd) / (WorldDistStart - WorldDistEnd);
	
	float distance = length(input.PositionWS - -CameraPos);
	float fogFactor = (distance - FogStart) / (FogEnd - FogStart);
	
	fogFactor = max(fogFactor, fogFactorWorldCenter);
	fogFactor = clamp(fogFactor, 0, 1);
	
	float3 sumLights = float3(0, 0, 0);
	for (int i = 0; i < 16; i++)
	{
		float distance = length(Lights[i].Position - input.PositionWS);
		
		float lightFactor = 1 - ((distance - Lights[i].Start) / (Lights[i].End - Lights[i].Start));
		lightFactor = clamp(lightFactor, 0, 1);
		sumLights += Lights[i].Color * lightFactor;

		sumLights = clamp(sumLights, float3(0, 0, 0), float3(1, 1, 1));
	}
	
	//ambient
	float3 ambientColor = AmbientColor * AmbientStrength;

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
		float shadow = DirectionalShadowFuncCSM(input.PositionWS, norm, LightDirection);
		//float shadow = DirectionalShadowFunc(input.PositionLS, norm, LightDirection);
		finalColor.rgb *= shadow;
		//finalColor.rgb *= 1 - shadow;
	}
	
	return finalColor;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_5_0 MainVS();
		PixelShader = compile ps_5_0 MainPS();
	}
};