#include "platform_defines.fxh"
#include "ACES.fxh"

#define MAX_LIGHTS 128

sampler Sampler : register(s0);

Texture2D Position			: register(t0);
Texture2D Depth				: register(t1);
Texture2D Normal			: register(t2);
Texture2D Diffuse			: register(t3);

float4x4 WorldViewProjection;
float3 CameraPosition;
uint LightIndex;

struct Light
{
	float4 Color;
	float3 Position;
	float Start;
	float End;
};

StructuredBuffer<Light> Lights : register(t15);

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

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	//output.Position = input.Position;
	output.PositionSS = mul(input.Position, WorldViewProjection);

	output.InstanceID = input.InstanceID;

	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
	float2 texCoord = (input.PositionSS.xy / input.PositionSS.w) * 0.5 + 0.5;
	texCoord.y = 1 - texCoord.y;
	
	float3 normal = Normal.Sample(Sampler, texCoord).rgb;
	float depth = Depth.Sample(Sampler, texCoord).r;
	float3 position = Position.Sample(Sampler, texCoord).rgb;
	//float specular = Diffuse.Sample(Sampler, texCoord).a;
	 
	float3 pointLightsColor = 0;

	Light light = Lights[LightIndex];

	float intensity = light.Color.a;
	if (intensity > 0)
	{
		float3 dir = light.Position - position;

		float normMult = max(dot(normal, normalize(dir)), 0.0);

		float scaleByDistance = 1 - saturate((length(dir) - light.Start) / (light.End - light.Start));

		float3 halfwayDir = normalize(normalize(dir) + CameraPosition);
		float spec = pow(max(dot(normal, halfwayDir), 0.0), 16.0);
		float3 lightSpec = light.Color.rgb * spec * scaleByDistance * intensity;

		float3 lightDiffuse = light.Color.rgb * scaleByDistance * normMult * intensity;

		pointLightsColor += lightDiffuse + lightSpec;
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