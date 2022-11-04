#include "stereokit.hlsli"

//--name = dofdev/below

//--color:color = 1,1,1,1
//--tex_scale   = 1
//--diffuse     = white
//--clearcolor:color = 0,0,0,0

float4       color;
float        tex_scale;
Texture2D    diffuse   : register(t0);
SamplerState diffuse_s : register(s0);
float4       clearcolor;

struct vsIn {
	float4 pos  : SV_Position;
	float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
	float4 col  : COLOR0;
};
struct psIn {
	float4 pos   : SV_Position;
	float2 uv    : TEXCOORD0;
	float4 world : WORLD;
	float4 color : COLOR0;
	float3 campos : TEXCOORD1;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	id        = id / sk_view_count;

  o.campos  = sk_camera_pos[o.view_id].xyz;

	o.world   = mul(input.pos, sk_inst[id].world);
	o.world.y = -o.world.y;
	o.pos     = mul(o.world, sk_viewproj[o.view_id]);

	float3 normal = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv         = input.uv * tex_scale;
	o.color      = color * input.col * sk_inst[id].color;
	o.color.rgb *= Lighting(normal);
	return o;
}

float4 ps(psIn input) : SV_TARGET {
	clip(-input.world.y);

	float4 col = diffuse.Sample(diffuse_s, input.uv);
	col = col * input.color;

	// dot product of the view direction and the normal
	float3 tocam   = normalize(input.campos - input.world.xyz);
	float3 normal  = normalize(float3(0, 1, 0));
	float  dotprod = max(dot(tocam, normal), 0.0);
	float  radians = acos(dotprod);
	float  degrees = radians * 180.0 / 3.14159;
	float a    = input.world.y * 2.0;
	// float bdeg = (1.0 - dotprod) * 90.0;
	float bdeg = degrees;
	float angle = 180.0 - (bdeg * 2.0);

	// float depth = max(1.0 - (input.world.y / -3.0), 0.0);
	float x = angle / 180.0;
	x = 1 - x;
	x = x * 0.1;
	x = x + 0.9;

	// dist magnitude from center X0Z
	float dist = max(1 - (length(input.world.xz) / 10.0), 0.0);
	float t = (x * x) * (dist * dist);
	// bluer
	col.r *= 1 - (t / 10);
	col.g *= 1 - (t / 10);

	col.rgb = lerp(col.rgb, float3(0.3, 1, 0.2), (-input.world.y / 20));
		
	return lerp(clearcolor, col, t);


	// float r = (a / sin(angle)) * sin(bdeg);

	// float3 topWorld = float3(input.world.x, 0, input.world.z);
	// float3 camWorld = float3(input.campos.x, 0, input.campos.z);
	// float3 proj = normalize(camWorld - topWorld);

	// float3 reflectionPoint = topWorld + (proj * r);

	// float val = max(dot(normalize(input.campos - reflectionPoint), normal), 0.0);

	// return col * val;



}