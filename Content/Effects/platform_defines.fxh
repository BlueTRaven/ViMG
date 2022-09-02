#ifndef PLATFORMDEFINES_FXH
#define PLATFORMDEFINES_FXH

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

#define DECLARE_TEXTURE(Name, index) \
    Texture2D<float4> Name : register(t##index); \
    sampler Name##Sampler : register(s##index);

#define DECLARE_TEXTURE_3D(Name, index) \
	Texture3D<float4> Name : register(t##index); \
	sampler Name##Sampler : register(s##index);

#define SAMPLE_TEXTURE(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)

#endif
