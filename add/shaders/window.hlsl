#include "stereokit.hlsli"

//--name = dofdev/window

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

	o.uv         = input.uv * tex_scale;
	o.color      = color * input.col * sk_inst[id].color;
	o.color.rgb *= Lighting(o.norm);
  // o.color.rgb *= sk_lighting(o.normal);
	return o;
}

float4 ps(psIn input) : SV_TARGET {
  float glow = FingerGlow(input.world.xyz, input.norm);
  // glow = 0;
	float4 col  = float4(lerp(input.color.rgb, float3(2,2,2), glow), input.color.a);

	// clip(input.world.y);

	// float4 col = diffuse.Sample(diffuse_s, input.uv);
	// col = col * input.color;

	return col;
}