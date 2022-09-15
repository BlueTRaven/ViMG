#include "platform_defines.fxh"
#include "vertex_structs.fxh"

//This represents the MAXIMUM number of cascades, not the actual number.
//The actual number is stored in NumCascades.
#define CASCADE_COUNT 16

sampler Sampler : register(s0);

Texture2D LightAccumulation : register(t0);
Texture2D Position			: register(t1);
Texture2D Depth				: register(t2);
Texture2D Normal			: register(t3);

sampler LightDepthSampler : register(s1);

Texture2DArray LightDepthTextures : register(t4);

uint NumCascades;
float CascadePlaneDistances[CASCADE_COUNT];
float4 CascadeOffsets[CASCADE_COUNT];
float4 CascadeScales[CASCADE_COUNT];
float4x4 LightViewProjection;
float3 LightDirection;
float3 LightColor;
float2 LightResolution;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
};

float SampleShadowmap(float2 baseUV, float u, float v, float2 inv, int layer, float z)
{
	float2 uv = baseUV + float2(u, v) * inv;

	float sampledDepth = LightDepthTextures.Sample(LightDepthSampler, float3(uv, layer)).r;

	float shadow = z < sampledDepth ? 1.0 : 0.0;

	return shadow;
}

float Sameple2x2(float3 shadowPosition, float lightDepth, uint cascade)
{
	float sampledDepth = LightDepthTextures.Sample(LightDepthSampler, float3(shadowPosition.xy, cascade)).r; //SAMPLE_TEXTURE(LightDepthTextures, float3(shadowPosition.xy, cascade)).r;

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

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = input.Position;
	output.TexCoord = input.TexCoord;

	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
	float3 lightAccumulation = LightAccumulation.Sample(Sampler, input.TexCoord).rgb;
	float3 normal = Normal.Sample(Sampler, input.TexCoord).rgb;
	float depth = Depth.Sample(Sampler, input.TexCoord).r;
	float3 position = Position.Sample(Sampler, input.TexCoord).rgb;
	
	float ndotl = saturate(dot(normal, LightDirection));
	float3 shadowColor = ShadowVisibility(position, depth, ndotl, normal);

	return float4(lightAccumulation * shadowColor, 1);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};