//matrix WorldViewProjection;
matrix World;
matrix View;
matrix Projection;

struct VertexShaderInput
{
	float4 Position : POSITION;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD;
	float4 Color 	: COLOR;
};

struct VertexShaderOutput
{
	float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float3 Normal   : TEXCOORD2;
    float4 Color    : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, World * View * Projection);
	output.TexCoord = input.TexCoord;
	output.Normal = float3(0, 0, 0);	//TODO
	output.Color = input.Color;

	return output;
}