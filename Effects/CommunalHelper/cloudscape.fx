texture2D atlas_texture;
sampler2D atlas_sampler = sampler_state
{
	Texture = <atlas_texture>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D color_texture;
sampler2D color_sampler = sampler_state
{
	Texture = <color_texture>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

int color_buffer_size = 1;
float2 offset = 0.0;

const float w = 320 / 2.0;
const float h = 180 / 2.0;

struct cloudscape_vertex
{
    float angle     : TEXCOORD0;
    float distance  : TEXCOORD1;
    float2 uv       : TEXCOORD2;
    short2 index    : TEXCOORD3;
};

struct vertex_output
{
    float4 position : SV_POSITION0;
    float2 uv       : TEXCOORD0;
    short2 index    : TEXCOORD1;
};

vertex_output vertex_shader(cloudscape_vertex input)
{
    vertex_output output;

    float2 cartesian = float2(cos(input.angle), sin(input.angle)) * input.distance + offset;

    output.position = float4(cartesian.x / w, cartesian.y / h, 0.0, 1.0);
    output.uv = input.uv;
    output.index = input.index;

    return output;
}

float4 pixel_shader(vertex_output input) : COLOR
{   
    float uvx = (float)input.index / (float)color_buffer_size;
    float4 color = tex2D(color_sampler, float2(uvx, 0.0));
    return tex2D(atlas_sampler, input.uv) * color;
}

technique cloudscape
{
    pass draw
    {
        VertexShader = compile vs_3_0 vertex_shader();
        PixelShader = compile ps_3_0 pixel_shader();
    }
}