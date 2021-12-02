#include "stereokit.hlsli"

//--name = dofdev/oriel
// float4       color;
float _height;
float _ypos;
Texture2D tex : register(t0);
SamplerState tex_s : register(s0);

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

float tri_raycast(float3 origin, float3 dir, float3 v0) {
  float final = -1;
  float3 v1 = float3(0, 0, 1);
  float3 v2 = float3(1, 0, 0);
  float3 e1 = v1 - v0;
  float3 e2 = v2 - v0;
  float3 h = cross(dir, e2);
  float a = dot(e1, h);
  if (a > -0.00001 && a < 0.00001) {} else{
    float f = 1 / a;
    float3 s = origin - v0;
    float u = f * dot(s, h);
    if (u < 0.0 || u > 1.0) {} else {
      float3 q = cross(s, e1);
      float v = f * dot(dir, q);
      if (v < 0.0 || u + v > 1.0) {} else {
        float t = f * dot(e2, q);
        if (t > 0.00001) { final = 1.0;} // t
      }
    }
  }
  return final;
}

float4 ps(psIn input) : SV_TARGET {
  // if (input.world.y - _ypos > (_height / 2.0) - 0.06) {
  //   // brighten;
  //   input.color.r += (1.0 - input.color.r) / 2.0;
  //   input.color.g += (1.0 - input.color.g) / 2.0;
  //   input.color.b += (1.0 - input.color.b) / 2.0;
  //   return input.color;
  // }

  // clamp how dark the object is *hsv
  // float value = input.color.r * 0.3 + input.color.g * 0.59 + input.color.b * 0.11;
  // blue tint
  input.color.r /= 5.0;
  input.color.g /= 5.0;
  // input.color.a = 0.5;

  // raycast or raymarch
  float4 col = tex.Sample(tex_s, float2(0.01, 0.01));
  float3 v0 = float3(0, col.r, 0);
  float3 ray = normalize(input.world - input.campos);
  input.color = float4(float3(1,1,1) * max(tri_raycast(input.world, ray, v0), 0.0), 1);
  

  return input.color;
}