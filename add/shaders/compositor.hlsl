#include "stereokit.hlsli"

//--name = dofdev/compositor

//--diffuse     = white

Texture2D    diffuse   : register(t0);
SamplerState diffuse_s : register(s0);

struct vsIn {
	float4 pos  : SV_Position;
  float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
};
struct psIn {
	float4 pos   : SV_Position;
  float3 world : WORLD;
	float2 uv    : TEXCOORD0;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	id        = id / sk_view_count;

	o.world = mul(input.pos, sk_inst[id].world).xyz;
	o.pos   = mul(float4(o.world, 1), sk_viewproj[o.view_id]);

  // float3 normal = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv = input.uv;

	return o;
}

float4 ps(psIn input) : SV_TARGET {
	float4 tex = diffuse.Sample(diffuse_s, input.uv);
  return tex;
  // 16 bit DepthTexture *non-linear* depth
  // render depth for debug by undoing the non-linear depth rcp
  float reciprocal_value = tex.r;
  float max_distance = 100.0;

  float depth = 1.0 / (reciprocal_value * (1.0 / max_distance) + 1.0);

  return float4(depth, depth, depth, 1);

  // if (depth > 0.0) {
  //   depth = 1.0;
  // }

  // depth = rcp(depth);
  
  // float4 og = mul(float4(input.world, 1), sk_viewproj[input.view_id]);
  // float depth = (og * rcp(og.w)).z;

  // return tex; // float4(tex.a, tex.a, tex.a, 1);
  // float v = -rcp(-val.r);
  // v = val.r;
  // return float4(v, v, v, 1);
}