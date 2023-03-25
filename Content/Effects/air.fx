#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);

Texture2D Diffuse : register(t0);

float4x4 World;
float4x4 ViewProjection;

float4 TintColor;

float4 PositionRadius;	//xyz position, w radius

VSOutputCube MainVS(in VSInputAir input)
{
	VSOutputCube output = (VSOutputCube)0;

	output.PositionWS = mul(input.Position, World).xyz;
	output.Position = mul(float4(output.PositionWS, 1), ViewProjection);
	//output.PositionSS = mul(float4(output.PositionWS, 1), View).xyz;
	output.Color = input.Color * TintColor;
	//output.Normal = mul(float4(input.Normal, 1), WorldNormal).xyz;
	//output.AO = input.AO;
	//output.Depth = output.Position.zw;
	//output.DepthVS = output.Position.w;

	return output;
}

float4 MainPS(VSOutputCube input) : SV_TARGET
{
	float end = PositionRadius.w;
	float start = end * 0.85;

	float distance = length(PositionRadius.xyz - input.PositionWS);
	float distMod = 1 - saturate((distance - start) / (end - start));

	float4 diffuse = Diffuse.Sample(Sampler, input.TexCoord);
	return diffuse * input.Color * distMod;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};