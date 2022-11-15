#include "platform_defines.fxh"
#include "ACES.fxh"

#define MAX_LIGHTS 128

sampler Sampler : register(s0);
sampler CubeSampler : register(s4);

Texture2D Position			: register(t0);
Texture2D Depth				: register(t1);
Texture2D Normal			: register(t2);
Texture2D Diffuse			: register(t3);

TextureCubeArray Cubemaps : register(t4);
//TextureCube Cubemaps : register(t4);

float4x4 ViewProjection;
float4x4 InvViewProjection;
float3 CameraPosition;

bool UseInstancing;

bool UseShadowmap;

uint LightIndex;

struct Light
{
	float4 Color;
	float3 Position;
	float Start;
	float End;

	float UseNDotL;
};

//a structured buffer containing a list of all indices of lights to draw.
StructuredBuffer<uint> LightInstanceIndices : register(t12);
//a structured buffer containing a list of all lights.
StructuredBuffer<Light> Lights : register(t14);
//a structured buffer containing a list of all shadowmapped lights.
StructuredBuffer<Light> ShadowmappedLights : register(t15);

struct VertexShaderInput
{
	float4 Position : POSITION0;
	uint InstanceID : SV_INSTANCEID;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 PositionSS : TEXCOORD1;
	uint InstanceID : INSTANCEID;
};

Light GetLight(uint instanceId)
{
	if (UseShadowmap) 
	{
		if (UseInstancing)
			return ShadowmappedLights[LightInstanceIndices[instanceId]];
		else return ShadowmappedLights[LightIndex];
	}
	else 
	{
		if (UseInstancing)
			return Lights[LightInstanceIndices[instanceId]];
		else return Lights[LightIndex];
	}
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	float3 wpos = input.Position.xyz * GetLight(input.InstanceID).End + GetLight(input.InstanceID).Position;

	output.Position = mul(float4(wpos, input.Position.w), ViewProjection);
	//output.Position = input.Position;
	output.PositionSS = output.Position;

	output.InstanceID = input.InstanceID;

	return output;
}

float3 ScreenSpaceToWorldSpace(float2 screenSpace, float depth)
{
	float4 position = float4(screenSpace.x * 2.0 - 1.0, (1 - screenSpace.y) * 2.0 - 1.0, depth, 1.0);
	//position.y = 1 - position.y;

	position = mul(position, InvViewProjection);

	float3 positionVS = position.xyz / position.w;

	return positionVS;
}

float linearize_depth(float d, float zNear, float zFar)
{
	return zNear * zFar / (zFar + d * (zNear - zFar));
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
	float2 texCoord = (input.PositionSS.xy / input.PositionSS.w) * 0.5 + 0.5;
	texCoord.y = 1 - texCoord.y;

	float3 normal = Normal.Sample(Sampler, texCoord).rgb;
	//float depth = Depth.Sample(Sampler, texCoord).r;

	//float3 position = ScreenSpaceToWorldSpace(texCoord, depth);
	float3 position = Position.Sample(Sampler, texCoord).rgb;
	//float specular = Diffuse.Sample(Sampler, texCoord).a;

	float3 pointLightsColor = 0;

	if (UseShadowmap)
	{
		Light light = GetLight(input.InstanceID);

		float intensity = light.Color.a;

		if (intensity > 0)
		{
			float3 dir = light.Position - position;
			//Don't ask...
			//have to negate shadow direction as the cubemap has an inverted y axis
			float3 shadowDir = position - light.Position;
			shadowDir.y = -shadowDir.y;

			float normMult = max(dot(normal, normalize(dir)), 0.0);

			//Create a gradient beginning at light.Start and ending at light.End.
			float scaleByDistance = 1 - saturate((length(dir) - light.Start) / (light.End - light.Start));

			float3 lightDiffuse = light.Color.rgb * scaleByDistance * normMult * intensity;

			uint index;

			if (UseInstancing)
				index = LightInstanceIndices[input.InstanceID];
			else index = LightIndex;

			float4 sampledDepth = Cubemaps.Sample(CubeSampler, float4(shadowDir, index));
			float realDepth = sampledDepth * light.End;
			float currentDepth = length(dir);

			float shadow = (currentDepth - 0.009 < realDepth) ? 1.0 : 0.0;

			pointLightsColor += lightDiffuse * shadow;
		}
	}
	else 
	{
		Light light = GetLight(input.InstanceID);

		float intensity = light.Color.a;
		if (intensity > 0)
		{
			float3 dir = light.Position - position;

			float nDotL = 1; 

			if (light.UseNDotL > 0)
				nDotL = max(dot(normal, normalize(dir)), 0.0);

			float scaleByDistance = 1 - saturate((length(dir) - light.Start) / (light.End - light.Start));

			if (light.UseNDotL > 0)
				scaleByDistance = scaleByDistance * scaleByDistance;

			float3 halfwayDir = normalize(normalize(dir) + CameraPosition);
			float spec = pow(max(dot(normal, halfwayDir), 0.0), 16.0);
			float3 lightSpec = light.Color.rgb * spec * scaleByDistance * intensity;

			float3 lightDiffuse = light.Color.rgb * scaleByDistance * nDotL * intensity;

			pointLightsColor += lightDiffuse + lightSpec;
		}
	}

	return float4(pointLightsColor, 1);
}

//[unroll]
//for (int i = 0; i < MAX_LIGHTS; i++)
//{
//	float intensity = Lights[i].Color.a;
//	if (intensity > 0)
//	{
//		float3 dir = Lights[i].Position - position;
//
//		float normMult = max(dot(normal, normalize(dir)), 0.0);
//
//		float scaleByDistance = 1 - saturate((length(dir) - Lights[i].Start) / (Lights[i].End - Lights[i].Start));
//
//		float3 halfwayDir = normalize(normalize(dir) + CameraPosition);
//		float spec = pow(max(dot(normal, halfwayDir), 0.0), 16.0);
//		float3 lightSpec = Lights[i].Color.rgb * spec * scaleByDistance * intensity;
//
//		float3 lightDiffuse = Lights[i].Color.rgb * scaleByDistance * normMult * intensity;
//
//		pointLightsColor += lightDiffuse + lightSpec;
//	}
//}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};