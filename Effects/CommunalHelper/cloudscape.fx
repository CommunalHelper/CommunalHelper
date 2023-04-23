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

int ring_count = 24;
int color_buffer_size = 1;

float2 offset = 0.0;

float inner_rotation = 1.0;
float outer_rotation = 1.0;
float rotation_exponent = 1.0;

float time = 0.0;

const float w = 320.0;
const float h = 180.0;

struct cloudscape_vertex
{
    float2 polar    : TEXCOORD0;
    float2 uv       : TEXCOORD1;
    short2 ids      : TEXCOORD2;
};

struct vertex_output
{
    float4 position : SV_POSITION0;
    float2 uv       : TEXCOORD0;
    int    id       : TEXCOORD1;
};

vertex_output vertex_shader(cloudscape_vertex input)
{
    vertex_output output;

    float angle = input.polar.x;
    float distance = input.polar.y;
    int id = (int)input.ids.x;
    int ring = (int)input.ids.y;

    float percent = (float)ring / ring_count;
    float d_rotation = outer_rotation - inner_rotation;
    float speed = d_rotation * pow(abs(percent), rotation_exponent) + inner_rotation;

    angle -= speed * time;

    float2 cartesian = float2(cos(angle), sin(angle)) * distance;
    float2 proj_offset = offset / float2(w / 2, h / 2);

    float x = cartesian.x / (w / 2.0) - 1 + proj_offset.x;
    float y = cartesian.y / (h / 2.0) + 1 - proj_offset.y;

    output.position = float4(x, y, 0.0, 1.0);
    output.uv = input.uv;
    output.id = id;

    return output;
}

float4 pixel_shader(vertex_output input) : COLOR
{   
    float uvx = (float)input.id / (float)color_buffer_size;
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