#include "platform_defines.fxh"
#include "vertex_structs.fxh"
#include "ACES.fxh"

#define CASCADE_COUNT 16
#define MAX_LIGHTS 8

DECLARE_TEXTURE(Texture, 0);
DECLARE_TEXTURE(TextureHeightFogMapDay, 1);
DECLARE_TEXTURE(TextureHeightFogMapNight, 2);
DECLARE_TEXTURE(TextureLightDepth, 3);
Texture2DArray<float4> LightDepthTextures : register(t5); \
sampler LightDepthTexturesSampler : register(s5);

TextureCube<float> Test : register(t6);
//TextureCube<float> TexturesPointLights[MAX_LIGHTS] : register(t6);

float4x4 World;
float4x4 View;
float4x4 WorldNormal;
float4x4 WorldViewProjection;

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

//float3 LightPos;

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
	float4 Color;
	float3 Position;
	float Start;
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

	float sampledDepth = LightDepthTextures.Sample(LightDepthTexturesSampler, float3(uv, layer)).r;

	float shadow = z < sampledDepth ? 1.0 : 0.0;

	return shadow;
}

float Sameple2x2(float3 shadowPosition, float lightDepth, uint cascade)
{	
	float sampledDepth = SAMPLE_TEXTURE(LightDepthTextures, float3(shadowPosition.xy, cascade)).r;

	return lightDepth < sampledDepth ? 1.0 : 0.0;
}

float Sample3x3(float3 shadowPosition, uint cascade, float lightDepth)
{
	float2 uv = shadowPosition.xy * LightResolution; // 1 unit - 1 texel

	float2 shadowMapSizeInv = 1.0 / LightResolution;

	float2 baseUv;
	baseUv.x = floor(uv.x + 0.5);
	baseUv.y = floor(uv.y + 0.5);

	float s = (uv.x + 0.5 - baseUv.x);
	float t = (uv.y + 0.5 - baseUv.y);

	baseUv -= float2(0.5, 0.5);
	baseUv *= shadowMapSizeInv;

	float uw0 = (3 - 2 * s);
	float uw1 = (1 + 2 * s);

	float u0 = (2 - s) / uw0 - 1;
	float u1 = s / uw1 + 1;

	float vw0 = (3 - 2 * t);
	float vw1 = (1 + 2 * t);

	float v0 = (2 - t) / vw0 - 1;
	float v1 = t / vw1 + 1;

	float sum = 0;

	sum += uw0 * vw0 * SampleShadowmap(baseUv, u0, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw0 * SampleShadowmap(baseUv, u1, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw0 * vw1 * SampleShadowmap(baseUv, u0, v1, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw1 * SampleShadowmap(baseUv, u1, v1, shadowMapSizeInv, cascade, lightDepth);

	return sum * 1.0f / 16;
}

float Sample5x5(float3 shadowPosition, uint cascade, float lightDepth)
{
	float2 uv = shadowPosition.xy * LightResolution; // 1 unit - 1 texel

	float2 shadowMapSizeInv = 1.0 / LightResolution;

	float2 baseUv;
	baseUv.x = floor(uv.x + 0.5);
	baseUv.y = floor(uv.y + 0.5);

	float s = (uv.x + 0.5 - baseUv.x);
	float t = (uv.y + 0.5 - baseUv.y);

	baseUv -= float2(0.5, 0.5);
	baseUv *= shadowMapSizeInv;

	float uw0 = (4 - 3 * s);
	float uw1 = 7;
	float uw2 = (1 + 3 * s);

	float u0 = (3 - 2 * s) / uw0 - 2;
	float u1 = (3 + s) / uw1;
	float u2 = s / uw2 + 2;

	float vw0 = (4 - 3 * t);
	float vw1 = 7;
	float vw2 = (1 + 3 * t);

	float v0 = (3 - 2 * t) / vw0 - 2;
	float v1 = (3 + t) / vw1;
	float v2 = t / vw2 + 2;

	float sum = 0;

	sum += uw0 * vw0 * SampleShadowmap(baseUv, u0, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw0 * SampleShadowmap(baseUv, u1, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw0 * SampleShadowmap(baseUv, u2, v0, shadowMapSizeInv, cascade, lightDepth);

	sum += uw0 * vw1 * SampleShadowmap(baseUv, u0, v1, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw1 * SampleShadowmap(baseUv, u1, v1, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw1 * SampleShadowmap(baseUv, u2, v1, shadowMapSizeInv, cascade, lightDepth);

	sum += uw0 * vw2 * SampleShadowmap(baseUv, u0, v2, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw2 * SampleShadowmap(baseUv, u1, v2, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw2 * SampleShadowmap(baseUv, u2, v2, shadowMapSizeInv, cascade, lightDepth);

	return sum * 1.0f / 144;
}

float3 Sample7x7(float3 shadowPosition, uint cascade, float lightDepth)
{
	float2 uv = shadowPosition.xy * LightResolution; // 1 unit - 1 texel

	float2 shadowMapSizeInv = 1.0 / LightResolution;

	float2 baseUv;
	baseUv.x = floor(uv.x + 0.5);
	baseUv.y = floor(uv.y + 0.5);

	float s = (uv.x + 0.5 - baseUv.x);
	float t = (uv.y + 0.5 - baseUv.y);

	baseUv -= float2(0.5, 0.5);
	baseUv *= shadowMapSizeInv;

	float uw0 = (5 * s - 6);
	float uw1 = (11 * s - 28);
	float uw2 = -(11 * s + 17);
	float uw3 = -(5 * s + 1);

	float u0 = (4 * s - 5) / uw0 - 3;
	float u1 = (4 * s - 16) / uw1 - 1;
	float u2 = -(7 * s + 5) / uw2 + 1;
	float u3 = -s / uw3 + 3;

	float vw0 = (5 * t - 6);
	float vw1 = (11 * t - 28);
	float vw2 = -(11 * t + 17);
	float vw3 = -(5 * t + 1);

	float v0 = (4 * t - 5) / vw0 - 3;
	float v1 = (4 * t - 16) / vw1 - 1;
	float v2 = -(7 * t + 5) / vw2 + 1;
	float v3 = -t / vw3 + 3;

	float sum = 0;

	sum += uw0 * vw0 * SampleShadowmap(baseUv, u0, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw0 * SampleShadowmap(baseUv, u1, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw0 * SampleShadowmap(baseUv, u2, v0, shadowMapSizeInv, cascade, lightDepth);
	sum += uw3 * vw0 * SampleShadowmap(baseUv, u3, v0, shadowMapSizeInv, cascade, lightDepth);
								   
	sum += uw0 * vw1 * SampleShadowmap(baseUv, u0, v1, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw1 * SampleShadowmap(baseUv, u1, v1, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw1 * SampleShadowmap(baseUv, u2, v1, shadowMapSizeInv, cascade, lightDepth);
	sum += uw3 * vw1 * SampleShadowmap(baseUv, u3, v1, shadowMapSizeInv, cascade, lightDepth);
								   
	sum += uw0 * vw2 * SampleShadowmap(baseUv, u0, v2, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw2 * SampleShadowmap(baseUv, u1, v2, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw2 * SampleShadowmap(baseUv, u2, v2, shadowMapSizeInv, cascade, lightDepth);
	sum += uw3 * vw2 * SampleShadowmap(baseUv, u3, v2, shadowMapSizeInv, cascade, lightDepth);
								   
	sum += uw0 * vw3 * SampleShadowmap(baseUv, u0, v3, shadowMapSizeInv, cascade, lightDepth);
	sum += uw1 * vw3 * SampleShadowmap(baseUv, u1, v3, shadowMapSizeInv, cascade, lightDepth);
	sum += uw2 * vw3 * SampleShadowmap(baseUv, u2, v3, shadowMapSizeInv, cascade, lightDepth);
	sum += uw3 * vw3 * SampleShadowmap(baseUv, u3, v3, shadowMapSizeInv, cascade, lightDepth);

	return sum * 1.0f / 2704;
}

float3 SampleShadowmapCascade(float3 shadowPosition, uint cascade)
{
	shadowPosition += CascadeOffsets[cascade].xyz;
	shadowPosition *= CascadeScales[cascade].xyz;

	float3 cascadeColor = float3(1.0f, 1.0f, 1.0f);

	float lightDepth = shadowPosition.z;

	const float bias = 0.002f;

	lightDepth -= bias;

	return Sample7x7(shadowPosition, cascade, lightDepth);
}

float3 GetShadowPosOffset(float nDotL, float3 normal)
{
	float2 shadowMapSize = LightResolution;
	//float numSlices;
	//LightDepthTextures.GetDimensions(shadowMapSize.x, shadowMapSize.y, numSlices);

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

	//float3 lightDir = normalize(LightPos - input.PositionWS);
	
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
	int i = 0;
	//[unroll]
	//for (int i = 0; i < MAX_LIGHTS; i++)
	{
		float3 dir = input.PositionWS - Lights[i].Position;
		//dir.y *= -1;

		float sampledDepth = 1 - Test.Sample(LightDepthTexturesSampler, dir).r;
		sampledDepth *= Lights[i].End;

		float currentDepth = length(dir);

		float lightShadow = currentDepth < sampledDepth ? 1.0 : 0.0;
		//float distance = length(Lights[i].Position - input.PositionWS);

		float lightFactor = lightShadow;//(1 - (currentDepth - Lights[i].Start) / (Lights[i].End - Lights[i].Start)) * lightShadow;
		//lightFactor = clamp(lightFactor, 0, 1);

		pointLightsColor = lightFactor;//Lights[i].Color * lightFactor;

		//pointLightsColor = clamp(pointLightsColor, float3(0, 0, 0), float3(1, 1, 1));
	}
	
	//ambient
	float3 ambientColor = AmbientColor * AmbientStrength;

	//ambientColor += pointLightsColor;
	//ambientColor = clamp(ambientColor, float3(0, 0, 0), float3(1, 1, 1));
	
	//diffuse
	//This is pretty much just our light ndotl calculation
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

	//This breaks down to:
	//(point lights + ambient + (shadow * ndotl multiplier)) * diffuse * ao * tint
	//To be more realistic, this should probably be:
	//(point lights + ambient + shadow + specular) * diffuse * ao * tint
	//And ndotl multiplier should be built into shadow.
	float3 colorWithoutAlpha = (pointLightsColor + ambientColor + shadowColor * (diffuseColor + specularColor)) * (diffuseAlbedo.rgb * input.AO) * TintColor;
	
	const float exposure = 0.7;
	colorWithoutAlpha = ACESFitted(colorWithoutAlpha * exposure);

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