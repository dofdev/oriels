#include "stereokit.hlsli"

// --name = dofdev/wireframe
float3 _rGlovePos;

struct vsIn {
  float4 pos   : SV_POSITION;
  float3 norm  : NORMAL;
};
struct psIn {
  float4 pos   : SV_POSITION;
  float4 col   : COLOR;
  float4 world : WORLD;
  uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
  psIn o;
  o.view_id    = id % sk_view_count;
	id           = id / sk_view_count;

  float3 norm = normalize(input.norm);

  // input.pos.xyz += norm * 0.01; 
  // wirefame
  // o.pos = mul(sk_view[o.view_id], input.pos);

  // float4 viewPosition = mul(input.pos, sk_view[o.view_id]);
  // o.pos = mul(viewPosition, sk_proj[o.view_id]);



  o.world = mul(input.pos, sk_inst[id].world);
	o.pos   = mul(o.world, sk_viewproj[o.view_id]);
  o.col   = sk_inst[id].color;

  return o;
}

float4 ps(psIn input) : SV_TARGET {
  float4 c = input.col;
  float3 rLocal = (input.world.xyz - _rGlovePos) * 6;
  c.r = 1 - min(abs(rLocal.x), 1.0);
  c.g = 1 - min(abs(rLocal.y), 1.0);
  c.b = 1 - min(abs(rLocal.z), 1.0);

  float m = min(c.r, min(c.g, c.b));
  c.rgb *= m;
  // if (m == 0) {
  // }


  return c;
}
