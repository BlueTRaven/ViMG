#include "platform_defines.fxh"
#include "vertex_structs.fxh"

#include "ACES.fxh"

sampler Sampler : register(s0);
Texture2D Texture : register(t0);

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
	float4 hdrColor = Texture.Sample(Sampler, input.TexCoord);
	float3 ldrColor = ACESFitted(hdrColor.rgb);

	//luma is encoded in alpha channel
	//float alpha = dot(ldrColor.rgb, float3(0.299, 0.587, 0.114));

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