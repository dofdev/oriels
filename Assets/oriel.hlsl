#include "stereokit.hlsli"

//--name = dofdev/oriel
// float4       color;
float _height;
float _ypos;

struct vsIn {
  float4 pos  : SV_POSITION;
  float3 norm : NORMAL0;
  float2 uv   : TEXCOORD0;
  float4 col  : COLOR0;
};
struct psIn {
  float4 pos   : SV_POSITION;
  float3 world : NORMAL0;
  float3 norm  : NORMAL1;
  float2 uv    : TEXCOORD0;
  float4 color : COLOR0;
  uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
  psIn o;
  o.view_id = id % sk_view_count;
  id        = id / sk_view_count;

  o.world = mul(input.pos, sk_inst[id].world).xyz;
  o.pos = mul(float4(o.world, 1), sk_viewproj[o.view_id]);
  o.norm = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

  o.uv    = input.uv;
  o.color = input.col;
  float lighting = dot(o.norm, normalize(float3(-0.3, 0.6, 0.1)));
  lighting = (clamp(lighting, 0, 1) * 0.8) + 0.2;
  o.color.rgb = o.color.rgb * lighting;
  return o;
}

float4 ps(psIn input) : SV_TARGET {
  if (input.world.y - _ypos > (_height / 2.0) - 0.06) {
    // brighten;
    input.color.r += (1.0 - input.color.r) / 2.0;
    input.color.g += (1.0 - input.color.g) / 2.0;
    input.color.b += (1.0 - input.color.b) / 2.0;
    return input.color;
  }
  // clamp how dark the object is *hsv
  // float value = input.color.r * 0.3 + input.color.g * 0.59 + input.color.b * 0.11;
  // blue tint
  input.color.r /= 5.0;
  input.color.g /= 5.0;
  // input.color.a = 0.5;
  return input.color;
}