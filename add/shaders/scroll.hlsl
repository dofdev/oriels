#include "stereokit.hlsli"

//--name = dofdev/above

//--color:color      = 1,1,1,1
//--ratio            = 1
//--scroll_y         = 1
//--scroll_t         = 0
//--diffuse          = white
//--clearcolor:color = 0,0,0,0

float4       color;
float        ratio;
float        scroll_y;
float        scroll_t;
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
  float3 norm  : NORMAL0;
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
	o.pos     = mul(o.world, sk_viewproj[o.view_id]);

	o.norm = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv         = float2(input.uv.x, ((scroll_t * scroll_y) + input.uv.y) * ratio);
	o.color      = color * input.col * sk_inst[id].color;
	o.color.rgb *= Lighting(o.norm);
	return o;
}

float4 ps(psIn input) : SV_TARGET {
	// clip(input.world.y);

	float4 col = diffuse.Sample(diffuse_s, input.uv);
	// col = col * input.color;

	// if (input.world.y < 0) {
	// 	col.r = col.r * 0.0;
	// 	col.g = col.g * 0.0;
	// 	col.b = col.b * 0.3;

	// 	col.rgb *= 0.1;
	// }

	// dist magnitude from center X0Z
	// float dist = max(1 - (length(input.world.xz) / 10.0), 0.0);
	// float4 color = lerp(clearcolor, col, 1 - ((1 - dist) * (1 - dist)));

  // float glow = sk_finger_glow(input.world.xyz, input.norm);
	// color.rgb += glow * 0.5;

	return col;
}