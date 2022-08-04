#include "stereokit.hlsli"

// --name = dofdev/frame
float3 _rGlovePos;
float _time;

Texture2D    dither   : register(t0);
SamplerState dither_s : register(s0);

struct vsIn {
  float4 pos   : SV_POSITION;
  float3 norm  : NORMAL;
  float4 col   : COLOR;
};
struct psIn {
  float4 pos    : SV_POSITION;
  float4 world  : POSITION0;
  float3 norm   : NORMAL0;
  float4 col    : COLOR;
  float3 campos : POSITION2;
  float3 camdir : NORMAL1;
  uint view_id  : SV_RenderTargetArrayIndex;
  // uint id       : SV_InstanceID;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
  psIn o;
  o.view_id = id % sk_view_count;
  id        = id / sk_view_count;

  o.world = mul(input.pos, sk_inst[id].world);
  o.pos   = mul(o.world, sk_viewproj[o.view_id]);
  o.norm  = normalize(mul(input.norm, (float3x3)sk_inst[id].world));
  // o.col   = sk_inst[id].color;
  o.col   = input.col;

  o.campos = sk_camera_pos[o.view_id].xyz;
  o.camdir = sk_camera_dir[o.view_id].xyz;

  // o.id      = id;
  return o;
}

float3 hsv2rgb(float3 hsv) {
  float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);

  return hsv.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
}

float remap_tri(float v)
{
  float orig = v * 2.0 - 1.0;
  v = max(-1.0f, orig / sqrt(abs(orig)));
  return v - sign(orig) + 0.5f;
}

float4 ps(psIn input) : SV_TARGET {

  // float4 c = input.col;
  // float3 rLocal = (input.world.xyz - _rGlovePos) / 2;
  // c.r = 1 - min(abs(rLocal.x), 1.0);
  // c.g = 1 - min(abs(rLocal.y), 1.0);
  // c.b = 1 - min(abs(rLocal.z), 1.0);

  // float m = min(c.r, min(c.g, c.b));
  // c.rgb *= m;

  float3 flatnorm = (input.col.rgb - float3(0.5, 0.5, 0.5)) * 2;

  // flatnorm = normalize(mul(flatnorm, (float3x3)sk_inst[input.id].world));
  
  // float3 cross = input.camDir * input.norm;
  float dist = length(input.world.xyz - input.campos);
  float3 raydir = normalize(input.world.xyz - input.campos);

  float facing = 1 - dot(raydir, input.camdir);
  // facing = (1 + facing) / 2;
  // facing = facing;



  // float dot = (dot(input.norm, raydir) + dot(flatnorm, raydir)) / 2;




  float h = (1 + dot(input.norm, raydir)) / 2;

  float d = dither.Sample(dither_s, input.pos.xy / 64.0).r; // time scroll through dither textures

  // d = remap_tri(d);

  return float4(hsv2rgb(float3(h, 1, 1)), facing * facing * d * 24);

  // float4 col = float4(1, 1, 1, 0);
  // float n = saturate(dot(raydir, input.norm));
  //   float shade = n / 0.333;
  // if (n < 0.333)
  // {
  //   col.g *= 0.92 - 0.2;
  //   // col.r *= 0.96 - 0.2;
  //   col.r *= 1 - ((1 - shade) / 3);
  // }
  //   col.a = shade;
  // return col;
}
