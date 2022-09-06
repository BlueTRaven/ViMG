#include "platform_defines.fxh"
#include "vertex_structs.fxh"

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
//float FarPlane;

uint NumCascades;
float CascadePlaneDistances[CASCADE_COUNT];
float4 CascadeOffsets[CASCADE_COUNT];
float4 CascadeScales[CASCADE_COUNT];
float4x4 LightViewProjections[CASCADE_COUNT];
float3 LightDirection;
float3 LightColor;
float2 LightResolution;

float3 TintColor;

float3 WorldSize;
float3 CubeSize;
//float2 MaxReachable;

float3 AmbientColor;
float AmbientStrength;
float SpecularStrength;

//Used for non-CSM directional shadow
float4x4 LightViewProjection;

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

VSOutputCube MainVS(in VSInputCube input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.PositionWS = mul(input.Position, World).xyz;
	output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
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

float SampleShadowmap(float2 baseUV, float u, float v, float2 inv, int layer, float z)
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
	for (uint i = 0; i < NumCascades; ++i)
	{
		if (depthValue < CascadePlaneDistances[i])
		{
			layer = i;
			break;
		}
	}
	if (layer == -1)
	{
		layer = NumCascades;
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
	sum += uw0 * vw0 * SampleShadowmap(baseUv, u0, v0, invLightResolution, layer, currentDepth);
	sum += uw1 * vw0 * SampleShadowmap(baseUv, u1, v0, invLightResolution, layer, currentDepth);
	sum += uw0 * vw1 * SampleShadowmap(baseUv, u0, v1, invLightResolution, layer, currentDepth);
	sum += uw1 * vw1 * SampleShadowmap(baseUv, u1, v1, invLightResolution, layer, currentDepth);

	return sum * 1.0f / 16.0;

	//float shadow = currentDepth < closestDepth ? 1.0 : 0.0;

	//return shadow;
}


//Non-CSM directional shadows
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

float3 SampleShadowmapCascade(float3 shadowPosition, uint cascade)
{
	shadowPosition += CascadeOffsets[cascade].xyz;
	shadowPosition *= CascadeScales[cascade].xyz;

	float3 cascadeColor = float3(1.0f, 1.0f, 1.0f);

	float2 shadowMapSize = LightResolution;
	//float numSlices;
	//TexturesLightDepth.GetDimensions(shadowMapSize.x, shadowMapSize.y, numSlices);

	float lightDepth = shadowPosition.z;

	const float bias = 0.002f;

	lightDepth -= bias;

	float2 uv = shadowPosition.xy * shadowMapSize; // 1 unit - 1 texel

	float2 shadowMapSizeInv = 1.0 / shadowMapSize;

	float2 baseUv;
	baseUv.x = floor(uv.x + 0.5);
	baseUv.y = floor(uv.y + 0.5);

	float s = (uv.x + 0.5 - baseUv.x);
	float t = (uv.y + 0.5 - baseUv.y);

	baseUv -= float2(0.5, 0.5);
	baseUv *= shadowMapSizeInv;

	float sampledDepth = SAMPLE_TEXTURE(TexturesLightDepth, float3(shadowPosition.xy, cascade)).r;

	return lightDepth < sampledDepth ? 1.0 : 0.0;
}

float3 GetShadowPosOffset(float nDotL, float3 normal)
{
	float2 shadowMapSize = LightResolution;
	//float numSlices;
	//TexturesLightDepth.GetDimensions(shadowMapSize.x, shadowMapSize.y, numSlices);

	float texelSize = 2.0f / shadowMapSize.x;
	float nmlOffsetScale = saturate(1.0f - nDotL);
	return texelSize * nmlOffsetScale * normal;
}

float3 ShadowVisibility(float3 positionWS, float depthVS, float nDotL, float3 normal)
{
	float3 shadowVisibility = 1.0;
	uint cascade = 0;

	[unroll]
	for (uint i = 0; i < NumCascades - 1; ++i)
	{
		[flatten]
		if (depthVS > CascadePlaneDistances[i])
			cascade = i + 1;
	}

	/*const float3 CascadeColors[5] =
	{
		float3(1.0f, 0.0f, 0.0f),
		float3(0.0f, 1.0f, 0.0f),
		float3(0.0f, 0.0f, 1.0f),
		float3(1.0f, 1.0f, 0.0f),
		float3(0.0f, 1.0f, 1.0f),
	};*/

	float3 offset = GetShadowPosOffset(nDotL, normal) / abs(CascadeScales[cascade].z);

	float3 samplePos = positionWS + offset;
	float3 shadowPosition = mul(float4(samplePos, 1.0f), LightViewProjection).xyz;

	shadowVisibility = SampleShadowmapCascade(shadowPosition, cascade);//DirectionalShadowFuncCSM(positionWS, normal, LightDirection);

	const float BlendThreshold = 0.1f;
	float nextSplit = CascadePlaneDistances[cascade];
	float splitSize = cascade == 0 ? nextSplit : nextSplit - CascadePlaneDistances[cascade - 1];
	float splitDist = (nextSplit - depthVS) / splitSize;

	[branch]
	if (splitDist <= BlendThreshold && cascade != NumCascades - 1)
	{
		float3 nextSplitVisibility = SampleShadowmapCascade(shadowPosition, cascade + 1);
		float lerpAmt = smoothstep(0.0f, BlendThreshold, splitDist);
		shadowVisibility = lerp(nextSplitVisibility, shadowVisibility, lerpAmt);
	}

	return shadowVisibility;// *CascadeColors[cascade];
}

float4 MainPS(VSOutputCube input) : SV_Target
{
	float4 diffuseAlbedo = SAMPLE_TEXTURE(Texture, input.TexCoord) * input.Color;

	if (diffuseAlbedo.a < 0.01)
		discard;
		
	float3 normalWS = normalize(input.Normal);

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
	
	float3 pointLightsColor = float3(0, 0, 0);
	for (int i = 0; i < 16; i++)
	{
		float distance = length(Lights[i].Position - input.PositionWS);
		
		float lightFactor = 1 - ((distance - Lights[i].Start) / (Lights[i].End - Lights[i].Start));
		lightFactor = clamp(lightFactor, 0, 1);
		pointLightsColor += Lights[i].Color * lightFactor;

		pointLightsColor = clamp(pointLightsColor, float3(0, 0, 0), float3(1, 1, 1));
	}
	
	//ambient
	float3 ambientColor = AmbientColor * AmbientStrength;

	//ambientColor += pointLightsColor;
	//ambientColor = clamp(ambientColor, float3(0, 0, 0), float3(1, 1, 1));
	
	//diffuse
	float diffDotToCam = max(dot(normalWS, LightDirection), 0.0);
	float3 diffuseColor = diffDotToCam * LightColor;
	
	//specular
	float3 camDir = normalize(CameraPos - input.PositionWS);
	float3 reflectDir = reflect(camDir, normalWS);
	
	float specToCam = pow(max(dot(camDir, reflectDir), 0), 32);
	float3 specularColor = specToCam * LightColor * SpecularStrength;

	float3 shadowColor = 1;
	if (EnableShadows > 0)
	{
		float ndotl = saturate(dot(normalWS, LightDirection));
		shadowColor = ShadowVisibility(input.PositionWS, input.DepthVS, ndotl, normalWS);
	}

	float3 colorWithoutAlpha = (pointLightsColor + ambientColor + shadowColor * (diffuseColor + specularColor)) * (diffuseAlbedo.rgb * input.AO) * TintColor;
	float4 finalColor = float4(colorWithoutAlpha, diffuseAlbedo.a);
	//float4 finalColor = float4(ambientColor, 1.0) * diffuseAlbedo;
	//finalColor.rgb *= input.AO;

	//finalColor.rgb *= TintColor;
	
	//Fog is, for the moment, applied over top of all other calculations.
	if (EnableFog > 0)
	{
		float percent = 1 - (max(4 * CubeSize.y, input.PositionWS.y) / (WorldSize.y * CubeSize.y));
		percent = clamp(percent, 0, 1);
		float4 worldHeightColorDay = SAMPLE_TEXTURE(TextureHeightFogMapDay, float2(0, percent));
		float4 worldHeightColorNight = SAMPLE_TEXTURE(TextureHeightFogMapNight, float2(0, percent));

		float4 lerpedColor = lerp(worldHeightColorDay, worldHeightColorNight, HeightFogMapLerp);
		
		finalColor = lerp(finalColor, lerpedColor, fogFactor);
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