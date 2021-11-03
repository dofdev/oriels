#include "stereokit.hlsli"

//--name = dofdev/oriel
// float4       color;
// Texture2D    diffuse   : register(t0);
// SamplerState diffuse_s : register(s0);

struct vsIn {
	float4 pos  : SV_POSITION;
	float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
	float4 col  : COLOR0;
};
struct psIn {
	float4 pos   : SV_POSITION;
  float3 norm  : NORMAL0;
	float2 uv    : TEXCOORD0;
  float1 depth : TEXCOORD1;
	float4 color : COLOR0;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	id        = id / sk_view_count;

	float3 world = mul(input.pos, sk_inst[id].world).xyz;
	o.pos        = mul(float4(world,         1), sk_viewproj[o.view_id]);
  o.norm     = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv    = input.uv;
	o.color = input.col;
  o.depth = dot(float4(o.norm, 1), normalize(float4(world,1) - sk_camera_pos[o.view_id]));
	return o;
}

float4 ps(psIn input) : SV_TARGET {
  clip(input.depth);
	return input.color; 
}