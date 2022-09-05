//This file contains vertex definitions so they are more easily shared across different shaders.
#ifndef VERTEXSTRUCTS_FXH
#define VERTEXSTRUCTS_FXH

struct VSInputCube
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL0;
	float AO : TEXCOORD1;
};

struct VSOutputCube
{
	float4 Position : SV_Position;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
	float3 PositionWS : TEXCOORD1;
	float3 PositionSS : TEXCOORD2;
	float3 Normal : TEXCOORD3;
	float AO : TEXCOORD4;
	float DepthVS : DEPTHVS;
	//float4 PositionLS : TEXCOORD5;
};

struct VSInputPC
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VSOutputPC
{
	float4 Position : SV_Position;
	float4 Color : COLOR0;
};

#endif
