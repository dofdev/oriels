#include "stereokit.hlsli"

//--name = dofdev/compositor

//--near = white
//--far  = white

Texture2D    near   : register(t0);
SamplerState near_s : register(s0);

Texture2D    far   : register(t1);
SamplerState far_s : register(s1);

struct vsIn {
	float4 pos  : SV_Position;
  float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
};
struct psIn {
  float4 screen_pos : SV_Position;
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
  o.screen_pos = input.pos;

  // float3 normal = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv = input.uv;
  o.uv.y = 1 - o.uv.y; // gl flip

	return o;
}

float far_clip = 1.0; // 1.0 for anything behind the near of the oriel is clipped
float near_flatten = 1.0; // 1.0 for anything behind the near of the oriel having its depth be set to the near of the oriel

float front_clip = 1.0; // same as far_clip
float front_flatten = 1.0; // same as near_flatten

static const float NEAR = 0.01;
static const float FAR  = 100.0;

// clip depth -> view depth
float depth_cv(float d) {
  // 32 bit DepthTexture *clip space
  return (NEAR * FAR) / (FAR - d * (FAR - NEAR));
}

float4 ps(psIn input) : SV_TARGET {

  // uint stencil = tex.a * 255.0;
  // float2 screen_uv = input.pos.xy / input.pos.w; // flip y?
  // screen_uv.y = 1 - screen_uv.y;

  // screen_uv.x = 744  - screen_uv.x * 744;
  // screen_uv.y = 1050 - screen_uv.y * 1050;

  float2 screen_uv = float2(input.pos.x * sk_aspect_ratio(input.view_id) / 744.0, input.pos.y / 1050.0);

  // screen_uv.y *= sk_aspect_ratio(input.view_id);

  
	float oriel_n = depth_cv(near.Sample(near_s, screen_uv).r);
  // if (oriel_n < 1.0) {
  //   discard;
  // }
  // clip(2 - oriel_n);
  
  // float oriel_f = depth_cv(far.Sample(far_s,   screen_uv).r);

  // clip
  // float4 pos_view  = mul(float4(input.world, 1), sk_viewproj[input.view_id]);
  // pos_view = input.pos;
  // float  d = (pos_view * rcp(pos_view.w)).z;
  // d = depth_cv(d);
  // if (d < oriel_n) {
  //   discard;
  // }
  // if (d > oriel_f) {
  //   discard;
  // }

  // if (depth_cv(near.Sample(near_s, pos_view.xy / input.pos.w).r) < 1.0) {
  //   discard;
  // }

  // screen space checkerboard using fmod
  float checkerboard = fmod(floor(input.uv.x  * 8.0) + floor(input.uv.y  * 8.0), 2);
  float checkeruv    = fmod(floor(screen_uv.x * 8.0) + floor(screen_uv.y * 8.0), 2);
  return float4(checkerboard, screen_uv.y, checkeruv, 1);


  // // new depth
  // if(depth < front_depth) {
  //   depth = lerp(0, front_depth, front_clip); // clip is t
  // } else if(depth > near_depth) {
  //   depth = lerp(FAR, near_depth, far_clip); // clip is t
  // } else {
  //   out_depth = depth;
  // }
  

  return float4(1,0,1,1);
}