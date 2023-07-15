Texture2D albedo_texture;
sampler2D albedo_sampler = sampler_state
{
    Texture = <albedo_texture>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D depth_texture;
sampler2D depth_sampler = sampler_state
{
    Texture = <depth_texture>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D normal_texture;
sampler2D normal_sampler = sampler_state
{
    Texture = <normal_texture>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct vertex_output
{
    float4 position : SV_POSITION;
    float4 color    : COLOR0;
    float2 uv       : TEXCOORD0;
};

const float2 offset_unit = float2(1.0 / 320.0, 1.0 / 180.0);
const float3 normal_edge_bias = 1.0;

float time = 0.0;
float4x4 MatrixTransform;

void vertex_shader(inout float4 color: COLOR0, inout float2 uv: TEXCOORD0, inout float4 position: SV_POSITION)
{
    position = mul(position, MatrixTransform);
}

float4 sample_at(sampler2D s, float2 at, float2 offset)
{
    return tex2D(s, at + offset * offset_unit);
}

float4 sample_albedo(float2 at, float2 offset = 0)
{
    return sample_at(albedo_sampler, at, offset);
}

float sample_depth(float2 at, float2 offset = 0)
{
    return sample_at(depth_sampler, at, offset).r;
}

float3 sample_normal(float2 at, float2 offset = 0)
{
    return (sample_at(normal_sampler, at, offset).xyz * 2) - 1;
}

float depth_edge_indicator(float2 uv, float depth)
{
    float i = 0.0;
    i += depth - sample_depth(uv, float2(0, 1));
    i += depth - sample_depth(uv, float2(0, -1));
    i += depth - sample_depth(uv, float2(1, 0));
    i += depth - sample_depth(uv, float2(-1, 0));
    i += (depth - sample_depth(uv, float2(1, 1))) / 2;
    i += (depth - sample_depth(uv, float2(-1, -1))) / 2;
    i += (depth - sample_depth(uv, float2(1, 1))) / 2;
    i += (depth - sample_depth(uv, float2(-1, -1))) / 2;
    return smoothstep(0.01, 0.02, i);
}

float neighbour_normal_edge_indicator(float2 uv, float3 normal, float depth, float2 offset)
{
    float depth_diff = sample_depth(uv, offset) - depth;
    float3 neighbor_normal = sample_normal(uv, offset);
    
    // edge pixels should yield to faces who's normals are closer to the bias normal.
    float normal_diff = dot(normal - neighbor_normal, normal_edge_bias);
    float i = clamp(smoothstep(-0.01, 0.01, normal_diff), 0.0, 1.0);
    
    // only the shallower pixel should detect the normal edge.
    float depth_indicator = clamp(sign(depth_diff * 0.25 + 0.0025), 0.0, 1.0);
    return (1.0 - dot(normal, neighbor_normal)) * depth_indicator * i;
}

float normal_edge_indicator(float2 uv, float3 normal, float depth)
{
    float i = 0.0;
    i += neighbour_normal_edge_indicator(uv, normal, depth, float2(0, -1));
    i += neighbour_normal_edge_indicator(uv, normal, depth, float2(0, 1));
    i += neighbour_normal_edge_indicator(uv, normal, depth, float2(-1, 0));
    i += neighbour_normal_edge_indicator(uv, normal, depth, float2(1, 0));
    return step(0.1, i);
}

float4 rainbow(float2 uv)
{
    float th = uv.x-uv.y;
    float cos_th = cos(th);
    float sin_th = sin(th);

    uv = uv.x * float2(cos_th, sin_th) + uv.y * float2(-sin_th, cos_th);
    return float4(cos(10 * uv.xyx + float3(0, 2, 4) + time) * 0.5 + 0.5, 1.0);
}

float4 pixel_shader(vertex_output input) : COLOR
{
    float4 albedo = sample_albedo(input.uv);
    float3 normal = sample_normal(input.uv);
    float4 depth_and_edges = sample_at(depth_sampler, input.uv, 0);

    float depth = depth_and_edges.x;
    float depth_edge_strength = depth_and_edges.y;
    float normal_edge_strength = depth_and_edges.z;
    float r = depth_and_edges.w;

    float dei = depth_edge_indicator(input.uv, depth);
    float nei = normal_edge_indicator(input.uv, normal, depth);

    float edge_lightning = dei > 0.0 ? (1 - dei * depth_edge_strength) : (1 + nei * normal_edge_strength);
    
    float4 final = float4(albedo.rgb * edge_lightning, albedo.a) * input.color;
    if (r > 0)
    {
        final = lerp(final, rainbow(input.uv), r);
    }
    return final;
}

technique pctn_compose
{
    pass compose
    {
        VertexShader = compile vs_3_0 vertex_shader();
        PixelShader = compile ps_3_0 pixel_shader();
    }
};