#include "platform_defines.fxh"
#include "ACES.fxh"

#define MAX_LIGHTS 128

sampler Sampler : register(s0);

Texture2D Position			: register(t0);
Texture2D Depth				: register(t1);
Texture2D Normal			: register(t2);
Texture2D Diffuse			: register(t3);

float3 CameraPosition;

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
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = input.Position;
	output.TexCoord = input.TexCoord;

	return output;
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
	//float3 lightAccumulation = LightAccumulation.Sample(Sampler, input.TexCoord).rgb;
	float3 normal = Normal.Sample(Sampler, input.TexCoord).rgb;
	float depth = Depth.Sample(Sampler, input.TexCoord).r;
	float3 position = Position.Sample(Sampler, input.TexCoord).rgb;
	//float specular = Diffuse.Sample(Sampler, input.TexCoord).a;

	float3 pointLightsColor = 0;

	[unroll]
	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		float intensity = Lights[i].Color.a;
		if (intensity > 0)
		{
			float3 dir = Lights[i].Position - position;

			float normMult = max(dot(normal, normalize(dir)), 0.0);

			float scaleByDistance = 1 - saturate((length(dir) - Lights[i].Start) / (Lights[i].End - Lights[i].Start));

			float3 halfwayDir = normalize(normalize(dir) + CameraPosition);
			float spec = pow(max(dot(normal, halfwayDir), 0.0), 16.0);
			float3 lightSpec = Lights[i].Color.rgb * spec * scaleByDistance * intensity;

			float3 lightDiffuse = Lights[i].Color.rgb * scaleByDistance * normMult * intensity;

			pointLightsColor += lightDiffuse + lightSpec;
		}
	}

	return float4(pointLightsColor, 1);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};