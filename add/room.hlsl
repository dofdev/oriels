#include "stereokit.hlsli"

//--name = dofdev/room

//--color:color = 1,1,1,1
//--tex_scale   = 1
//--diffuse     = white

float4       color;
float        tex_scale;
Texture2D    diffuse   : register(t0);
SamplerState diffuse_s : register(s0);

struct vsIn {
	float4 pos  : SV_Position;
	float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
	float4 col  : COLOR0;
};
struct psIn {
	float4 pos   : SV_Position;
	float2 uv    : TEXCOORD0;
	float4 color : COLOR0;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	id        = id / sk_view_count;

	float4 world = mul(input.pos, sk_inst[id].world);
	o.pos        = mul(world,     sk_viewproj[o.view_id]);

	float3 normal = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv         = input.uv * tex_scale;
	o.color      = color * input.col * sk_inst[id].color;
	// o.color.rgb *= Lighting(normal);
	return o;
}

float4 ps(psIn input) : SV_TARGET {
	float4 col = diffuse.Sample(diffuse_s, input.uv);
  float value = (col.r + col.r + col.g + col.g + col.g + col.b) / 6;
	return float4(value, value, value, 1);
}