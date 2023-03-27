#include "platform_defines.fxh"
//To be rendered additively
float Time;
float2 Resolution;
float3 CameraPosition;

float4x4 Mat;

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
    VertexShaderOutput output = (VertexShaderOutput) 0;

    output.Position = input.Position;
    output.TexCoord = input.TexCoord;

    return output;
}

float2x2 mm2(in float a)
{
    float c = cos(a), s = sin(a);
    return float2x2(c, s, -s, c);
}
float2x2 m2 = float2x2(0.95534, 0.29552, -0.29552, 0.95534);
float tri(in float x)
{
    return clamp(abs(frac(x) - .5), 0.01, 0.49);
}
float2 tri2(in float2 p)
{
    return float2(tri(p.x) + tri(p.y), tri(p.y + tri(p.x)));
}

float triNoise2d(in float2 p, float spd)
{
    float z = 1.8;
    float z2 = 2.5;
    float rz = 0.;
    float2 p2 = mul(p, mm2(p.x * 0.06));
    float2 bp = float2(p2);
    for (float i = 0.; i < 5.; i++)
    {
        float2 dg = tri2(bp * 1.85) * .75;
        dg = mul(dg, mm2(Time * spd));
        p2 -= dg / z2;

        bp *= 1.3;
        z2 *= .45;
        z *= .42;
        p2 *= 1.21 + (rz - 1.0) * .02;
        
        rz += tri(p2.x + tri(p2.y)) * z;
        p2 = mul(p2, -m2);
    }
    return clamp(1. / pow(rz * 29., 1.3), 0., .55);
}

float hash21(in float2 n)
{
    return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
}
float4 aurora(float3 ro, float3 rd, float2 texCoord)
{
    texCoord.y = 1 - texCoord.y;
    
    float4 col = float4(0, 0, 0, 0);
    float4 avgCol = float4(0, 0, 0, 0);
    
    for (float i = 0.; i < 50.; i++)
    {
        float of = 0.006 * hash21(texCoord * Resolution) * smoothstep(0., 15., i);
        float pt = ((.8 + pow(i, 1.4) * .002) - ro.y) / (rd.y * 2. + 0.4);
        pt -= of;
        float3 bpos = ro + pt * rd;
        float2 p = bpos.zx;
        float rzt = triNoise2d(p, 0.06);
        float4 col2 = float4(0, 0, 0, rzt);
        col2.rgb = (sin(1. - float3(2.15, -.5, 1.2) + i * 0.043) * 0.5 + 0.5) * rzt;
        avgCol = lerp(avgCol, col2, .5);
        col += avgCol * exp2(-i * 0.065 - 2.5) * smoothstep(0., 5., i);
        
    }
    
    col *= (clamp(rd.y * 15. + .4, 0., 1.));
    
    
    //return clamp(pow(col,float4(1.3))*1.5,0.,1.);
    //return clamp(pow(col,float4(1.7))*2.,0.,1.);
    //return clamp(pow(col,float4(1.5))*2.5,0.,1.);
    //return clamp(pow(col,float4(1.8))*1.5,0.,1.);
    
    //return smoothstep(0.,1.1,pow(col,float4(1.))*1.5);
    return col * 1.8;
    //return pow(col,float4(1.))*2.
}


//-------------------Background and Stars--------------------

float3 nmzHash33(float3 q)
{
    uint3 p = uint3(int3(q));
    p = p * uint3(374761393U, 1103515245U, 668265263U) + p.zxy + p.yzx;
    p = p.yzx * (p.zxy ^ (p >> 3U));
    return float3(p ^ (p >> 16U)) * (1.0 / float3(0xffffffffU, 0xffffffffU, 0xffffffffU));
}

float3 stars(in float3 p)
{
    float3 c = float3(0, 0, 0);
    float res = Resolution.x * 1.;
    
    for (float i = 0.; i < 4.; i++)
    {
        float3 q = frac(p * (.15 * res)) - 0.5;
        float3 id = floor(p * (.15 * res));
        float2 rn = nmzHash33(id).xy;
        float c2 = 1. - smoothstep(0., .6, length(q));
        c2 *= step(rn.x, .0005 + i * i * 0.001);
        c += c2 * (lerp(float3(1.0, 0.49, 0.1), float3(0.75, 0.9, 1.), rn.y) * 0.1 + 0.9);
        p *= 1.3;
    }
    return c * c * .8;
}

float3 bg(in float3 rd)
{
    float sd = dot(normalize(float3(-0.5, -0.6, 0.9)), rd) * 0.5 + 0.5;
    sd = pow(sd, 5.);
    float3 col = lerp(float3(0.05, 0.1, 0.2), float3(0.1, 0.05, 0.2), sd);
    return col * .63;
}
//-----------------------------------------------------------


float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 p = float2(input.TexCoord.x, 1 - input.TexCoord.y);
    p -= 0.5;
    //p.x *= Resolution.x / Resolution.y;
    
    float3 ro = float3(0, 0, -6.7);
    float3 rd = normalize(float3(p, 1.0 / 19.2));
    //currently passing in camera rotation -y,x to CameraPosition
    //float2 mo = CameraPosition.xy;// / Resolution.xy - .5;
    //mo = (mo == float2(-0.5, -0.5)) ? mo = float2(-0.1, 0.1) : mo;
    //mo.x *= Resolution.x / Resolution.y;
    rd = mul(float4(rd.xyz, 1), Mat).xyz;
    //rd.yz = mul(rd.yz, mm2(mo.y));
    //rd.xz = mul(rd.xz, mm2(mo.x));
    
    float3 col = float3(0, 0, 0);
    float3 brd = rd;
    float fade = smoothstep(0., 0.01, abs(brd.y)) * 0.1 + 0.9;
    
    //col = bg(rd) * fade;

    float4 aur = smoothstep(0., 1.5, aurora(ro, rd, input.TexCoord)) * fade;
    //col += stars(rd);
    //col = col * (1. - aur.a) + aur.rgb;
    
    return float4(aur.xyz, 1 - aur.a);
}

technique T1
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};