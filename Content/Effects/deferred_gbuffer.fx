#include "platform_defines.fxh"
#include "vertex_structs.fxh"

sampler Sampler : register(s0);
sampler BilinearSampler : register(s1);

Texture2D Diffuse			: register(t0);
Texture2D Normal			: register(t1);
Texture2D Specular			: register(t2);
Texture2D Emissive			: register(t3);

Texture2D WorldheightMapAmb	: register(t4);
//Texture2D Heightmap			: register(t5);

float4x4 World;
float4x4 View;
float4x4 WorldNormal;
float4x4 ViewProjection;
float4x4 InvViewProjection;

bool UseInstancing;

bool UseSourceRect;
float2 SourceRectPos;
float2 SourceRectFarPos;
float2 TextureSize;
float2 TexCoordOffset;

float AmbientStrength;
float SpecularPower;

float3 TintColor;

float Time;

struct InstancedDrawParams
{
    float4x4 World;
    float4x4 WorldNormal;

    bool UseSourceRect;
    float2 SourceRectPos;
    float2 SourceRectFarPos;

    float3 TintColor;
};

struct PSOutputGBuffer
{
	float4 Diffuse				: COLOR0;			//color/albedo rgb; Specular a
	float4 LightAccumulation	: COLOR1;	//ambient + emissive; light is accumulated after
	float4 Depth				: COLOR2;
	float4 Position				: COLOR3;
	float4 Normal				: COLOR4;
	float4 AO					: COLOR5;
};

struct VSInputGBuffer
{
    float4 Position		: POSITION0;
    float4 Color		: COLOR0;
    float2 TexCoord		: TEXCOORD0;
    float3 Normal		: NORMAL0;
    float3 Tangent		: NORMAL1;
    float3 Bitangent	: NORMAL2;
    float AO			: TEXCOORD1;

    float AnimFrameTime : TEXCOORD2;
    float NumAnimFrames : TEXCOORD3;
    float AnimFrameSize : TEXCOORD4;
	
    uint InstanceID		: SV_INSTANCEID;
};

struct VSOutputGBuffer
{
    float4 Position		: SV_Position;
    float4 Color		: COLOR0;
    float2 TexCoord		: TEXCOORD0;
    float3 PositionWS	: TEXCOORD1;
    float4 PositionSS	: TEXCOORD2;
    float3x3 TBN		: TEXCOORD3;
    float3 Normal		: NORMAL0;
    float AO			: AO;
    float DepthVS		: DEPTHVS;
	
	float4x4 t : TEXCOORD8;
    uint InstanceID		: INSTANCEID;
    //float2 Depth		: DEPTHA;
};

//A structured buffer containing all the instanced draw parameters.
StructuredBuffer<InstancedDrawParams> InstancedDraws : register(t15);

VSOutputGBuffer MainVS(in VSInputGBuffer input)
{
    VSOutputGBuffer output = (VSOutputGBuffer) 0;

    float4x4 useWorld;
    float4x4 useWorldNormal;
	
    bool useUseSourceRect;
    float2 useSourceRectPos;
    float2 useSourceRectFarPos;
	
    float4 useTintColor;
	
    if (UseInstancing)
    {
        useWorld = InstancedDraws[input.InstanceID].World;
        useWorldNormal = InstancedDraws[input.InstanceID].WorldNormal;
		
        useUseSourceRect = InstancedDraws[input.InstanceID].UseSourceRect;
        useSourceRectPos = InstancedDraws[input.InstanceID].SourceRectPos;
        useSourceRectFarPos = InstancedDraws[input.InstanceID].SourceRectFarPos;
		
        useTintColor = float4(InstancedDraws[input.InstanceID].TintColor, 1);
		
        output.InstanceID = input.InstanceID;
    }
    else
    {
        useWorld = World;
        useWorldNormal = WorldNormal;
        
		useUseSourceRect = UseSourceRect;
        useSourceRectPos = SourceRectPos;
        useSourceRectFarPos = SourceRectFarPos;
		
        useTintColor = float4(TintColor, 1);
    }
	
    output.PositionWS = mul(input.Position, useWorld).xyz;
	output.Position = mul(float4(output.PositionWS, 1), ViewProjection);
	output.PositionSS = output.Position;
	output.Color = input.Color * useTintColor;
	
    float3 T = normalize(mul(float4(input.Tangent, 0), useWorldNormal)).xyz;
    float3 B = normalize(mul(float4(input.Bitangent, 0), useWorldNormal)).xyz;
    float3 N = normalize(mul(float4(input.Normal, 0), useWorldNormal)).xyz;
	
	//might need to be transposed
    output.TBN = float3x3(T, B, N);
	
    output.Normal = mul(float4(input.Normal, 1), useWorldNormal).xyz;
	
	output.AO = input.AO;
	//output.Depth = output.Position.zw;
	output.DepthVS = output.Position.w;

    if (useUseSourceRect)
	{
        float2 xy = useSourceRectPos / TextureSize;
        float2 wh = (useSourceRectFarPos - useSourceRectPos) / TextureSize;

		output.TexCoord = xy + (wh * input.TexCoord);
	}
	else output.TexCoord = input.TexCoord + TexCoordOffset;

	//bool HasAnimation: whether or not the vertex has a texcoord animation.
	//float AnimationFrameTime: how long each frame of the animation lasts.
	//uint NumAnimFrames: the number of frames the animation has.
	//Animation will be offset in the X axis by the size of the source rectangle.
	//Sample flame animation: 
	//SourceRectPos: 192, 16
	//HasAnimation: true
	//AnimFrameTime: 0.25
	//NumAnimFrames: 3
	if (input.AnimFrameTime > 0)
	{
		float totalFrameTime = input.AnimFrameTime * input.NumAnimFrames;

		uint frame = ((Time % totalFrameTime) / totalFrameTime) * input.NumAnimFrames;

		float2 wh = input.AnimFrameSize / 1024.0;

		output.TexCoord.x += wh.x * frame;
	}

	return output;
}

float3 ScreenSpaceToWorldSpace(float2 screenSpace, float depth)
{
	float x = screenSpace.x * 2.0f - 1.0f;
	float y = (1 - screenSpace.y) * 2.0f - 1.0f;

	float4 position_s = float4(x, y, depth, 1.0f);
	float4 position_v = mul(InvViewProjection, position_s);
	
	return position_v.xyz / position_v.w;
}

PSOutputGBuffer MainPS(VSOutputGBuffer input)
{
	float4 albedoSample = Diffuse.Sample(Sampler, input.TexCoord);
	if (albedoSample.a < 0.1)
		discard;

	PSOutputGBuffer output = (PSOutputGBuffer)0;
	
	float ambientWorldheight = WorldheightMapAmb.Sample(Sampler, float2(0.5, 1 - (input.PositionWS.y / (512.0 * 0.1)))).r;
	float3 ambient = albedoSample.rgb * AmbientStrength * ambientWorldheight;
	//Note: this is sampled with a normal sampler
	//float heightmapHeight = Heightmap.Sample(Sampler, float2(input.PositionWS.xz / 0.1 / 512)).r * 512;

	float3 emissive = Emissive.Sample(Sampler, input.TexCoord).rgb * input.Color.rgb;

    float3 normal = Normal.Sample(Sampler, input.TexCoord).rgb;
    normal = normal * 2.0 - 1.0;	//to [-1, 1]
    normal = normalize(mul(normal, input.TBN));
	
	float depth = input.DepthVS;

	output.Diffuse.rgb = albedoSample.rgb * input.Color.rgb;
	output.Diffuse.a = Specular.Sample(Sampler, input.TexCoord).r;

	output.LightAccumulation = float4(ambient + emissive, 1);

	output.Depth = float4(depth, depth, depth, 1.0);
	output.Position = float4(input.PositionWS, 1);
    output.Normal = float4(normal.x, normal.y, normal.z, 1); //float4(-normalize(cross(ddx(input.PositionWS), ddy(input.PositionWS))), 1);
	output.AO = float4(input.AO, input.AO, input.AO, 1);
	
	return output;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};