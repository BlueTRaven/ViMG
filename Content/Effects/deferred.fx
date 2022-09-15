#include "platform_defines.fxh"
#include "vertex_structs.fxh"
#include "ACES.fxh"

#define MAX_LIGHTS 128

sampler Sampler : register(s0);

Texture2D Diffuse			: register(t0);
Texture2D LightAccumulation : register(t1);
Texture2D Depth				: register(t2);
Texture2D Position			: register(t3);
Texture2D Normal			: register(t4);
Texture2D AO				: register(t5);

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
	float3 diffuse = Diffuse.Sample(Sampler, input.TexCoord).rgb;
	float3 normal = Normal.Sample(Sampler, input.TexCoord).rgb;
	float3 position = Position.Sample(Sampler, input.TexCoord).rgb;
	float specular = Diffuse.Sample(Sampler, input.TexCoord).a;
	float ao = AO.Sample(Sampler, input.TexCoord).r;

	float3 pointLightsColor = float3(0, 0, 0);
	
	[unroll]
	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		float intensity = Lights[i].Color.a;
		if (intensity > 0) 
		{
			float3 dir = Lights[i].Position - position;

			float normMult = max(dot(normal, normalize(dir)), 0.0);

			float scaleByDistance = 1 - saturate((length(dir) - Lights[i].Start) / (Lights[i].End - Lights[i].Start));

			pointLightsColor += Lights[i].Color.rgb * scaleByDistance * normMult * intensity;
		}
	}

	float3 hdrColor = (diffuse + pointLightsColor) * ao;

	float3 ldrColor = ACESFitted(hdrColor);

	return float4(ldrColor, 1);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};