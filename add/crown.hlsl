#include "stereokit.hlsli"

//--name = dofdev/crown

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
  float3 campos : NORMAL0;
  float3 world : NORMAL1;
  float3 norm  : NORMAL2;
  float2 uv    : TEXCOORD0;
  float4 color : COLOR0;
  uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
  psIn o;
  o.view_id = id % sk_view_count;
  id        = id / sk_view_count;

  o.campos = sk_camera_pos[o.view_id].xyz;
  o.world = mul(input.pos, sk_inst[id].world).xyz;
  o.pos = mul(float4(o.world, 1), sk_viewproj[o.view_id]);
  o.norm = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

  o.uv    = input.uv;
  o.color = input.col;
  float lighting = dot(o.norm, normalize(float3(-0.3, 0.6, 0.1)));
  lighting = (clamp(lighting, 0, 1) * 0.8) + 0.2;
  o.color.rgb = o.color.rgb * lighting; // * sk_inst[id].color;
  return o;
}

float3 cross(float3 a, float3 b) {
  return float3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
}

float dot(float3 a, float3 b) {
  return a.x * b.x + a.y * b.y + a.z * b.z;
}

float4 ps(psIn input) : SV_TARGET {
  float4 col = float4(0, 0, 0, 0);
  if (input.world.y - _ypos > (_height / 2.0) - 0.06) {
    col = float4(1,1,1,1) * 0.25 * dot(input.world, input.norm);
    if (input.norm.y > 0) {
      col = float4(1,1,1,1) * 0.025;
    }
  }
  return col;
}