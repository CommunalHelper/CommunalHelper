float4x4 world;
float4x4 view;
float4x4 proj;

float4 tint = 1;

float depth_edge_strength = 0.4;
float normal_edge_strength = 0.3;
float rainbow = 0.0;

float highlight_lower_bound = 0.8;
float highlight_upper_bound = 1.0;
float highlight_strength = 0.0;

const float min_ambient = 0.35;
const float max_ambient = 1.0;

texture2D atlas_texture;
sampler2D atlas_sampler = sampler_state
{
	Texture = <atlas_texture>;
};

struct vertex_input
{
    float4 position             : POSITION0;
    float4 color                : COLOR0;
    float2 uv                   : TEXCOORD0;
    float3 normal               : NORMAL0;
};

struct vertex_output
{
    float4 position             : SV_POSITION0;
    float4 color                : COLOR0;
    float2 uv                   : TEXCOORD0;
    float3 normal               : NORMAL0;
    float2 depth                : TEXCOORD1;
    float2 edges                : TEXCOORD2;
    float1 rainbow              : TEXCOORD3;
};

struct pixel_output
{
    float4 albedo   : COLOR0;
    float4 depth    : COLOR1;
    float4 normal   : COLOR2;
};

vertex_output vertex_shader(vertex_input input)
{
    vertex_output output;

    float4x4 mat = mul(mul(world, view), proj);

    // Calculate final vertex position
    output.position = mul(input.position, mat);
    
    // Output vertex attributes
    output.color = input.color;
    output.uv = input.uv;
    output.normal = input.normal;

    // putting values that are going to be used into a depth vector. cause we can't do it in the pixel shader
    output.depth.xy = output.position.zw;

    // passing per-pixel egde parameters
    output.edges = float2(depth_edge_strength, normal_edge_strength);
    output.rainbow = rainbow;
    
    return output;
}

pixel_output pixel_shader(vertex_output input)
{
    pixel_output output;

    float4x4 mat = mul(world, view);
    float3 normal = normalize(mul(input.normal, (float3x3)mat));

    float light_dot = dot(normal, float3(0.0, 0.0, -1.0));
    float lightning = clamp((max_ambient - min_ambient) * light_dot + min_ambient, min_ambient, max_ambient);

    float highlight = highlight_strength > 0.0
        ? smoothstep(highlight_lower_bound, highlight_upper_bound, light_dot) * highlight_strength
        : 0.0;
    
    // Output color to albedo buffer
    output.albedo = tex2D(atlas_sampler, input.uv) * input.color * tint;
    output.albedo.rgb *= lightning + highlight;
    
    // Output depth to depth buffer
    float depth = input.depth.x / input.depth.y;
    output.depth = float4(depth, input.edges.x, input.edges.y, rainbow); // add 1 to rainbow because depth.a = 0 fucks it up
    
    // Output normal to normal buffer
    float3 normal_color = (normal + 1) / 2;
    output.normal = float4(normal_color, 1.0);

    return output;
}

technique pctn_mrt
{
    pass mrt
    {
        VertexShader = compile vs_3_0 vertex_shader();
        PixelShader = compile ps_3_0 pixel_shader();
    }
}