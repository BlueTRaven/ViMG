Texture2D Texture;

SamplerState texSampler 
{
	Texture = <Texture>;
	MagFilter = Point;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 tc = input.TexCoord;
	return tex2D(Texture, tc) * input.Color;
}