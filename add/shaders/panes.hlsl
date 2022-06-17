#include "stereokit.hlsli"

// --name = dofdev/panes

float4x4 _matrix;

struct vsIn {
  float4 pos   : SV_POSITION;
  float3 norm  : NORMAL;
};
struct psIn {
  float4 pos   : SV_POSITION;
  float3 norm  : NORMAL;
  float4 world : WORLD;
  float4 color : COLOR;
  uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
  psIn o;
  o.view_id    = id % sk_view_count;
	id           = id / sk_view_count;

  o.world      = mul(input.pos, sk_inst[id].world);
	o.pos        = mul(o.world,   sk_viewproj[o.view_id]);
  o.norm       = normalize(mul(input.norm, (float3x3)sk_inst[id].world));
  o.color      = sk_inst[id].color;

  return o;
}

float4 ps(psIn input) : SV_TARGET {
  
  return input.color;
}
