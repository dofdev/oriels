#include "stereokit.hlsli"

// --name = dofdev/oriel
//--diffuse     = white

float3       _center;
float3       _dimensions;
float3       _light;
float4x4     _matrix;

Texture2D    diffuse   : register(t0);
SamplerState diffuse_s : register(s0);

struct vsIn {
  float4 position : SV_POSITION;
  float3 normal   : NORMAL;
  float2 uv       : TEXCOORD0;
  float3 color    : COLOR0;
};
struct psIn {
  float4 position : SV_POSITION;
  float2 uv       : TEXCOORD0;
  float3 world    : WORLD;
  float3 normal   : NORMAL;
  float4 color    : COLOR0;
  float3 camdir   : TEXCOORD1;
  float3 campos   : TEXCOORD2;
  uint view_id    : SV_RenderTargetArrayIndex;
};
struct psOut {
  float4 color : SV_TARGET;
  float  depth : SV_Depth;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
  psIn o;
  o.view_id  = id % sk_view_count;
  id         = id / sk_view_count;

  o.camdir   = sk_camera_dir[o.view_id].xyz;
  o.campos   = sk_camera_pos[o.view_id].xyz;
  o.world    = mul(input.position, sk_inst[id].world).xyz;
  o.position = mul(float4(o.world, 1), sk_viewproj[o.view_id]);
  o.normal   = normalize(mul(input.normal, (float3x3)sk_inst[id].world));
  o.uv       = input.uv;
  o.color    = float4(input.color * sk_inst[id].color.rgb, 1);

  return o;
}

float3 cross(float3 a, float3 b) {
  return float3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
}

float dot(float3 a, float3 b) {
  return a.x * b.x + a.y * b.y + a.z * b.z;
}

float sdBox(float3 p, float3 b) {
  float3 q = abs(p) - b;
  return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdSphere(float3 p, float r) {
  return length(p) - r;
}

float raymarch(float3 origin, float3 direction) {
  origin = mul(float4(origin, 1), _matrix).xyz;
  direction = mul(float4(direction, 0), _matrix).xyz;
	float dist = 0.0;
  for (int i = 0; i < 256; i++) {
    float3 pos = origin + dist * direction;
    float step = sdBox(pos, _dimensions / 2.0);
    if (step < 0.0001 || dist > 100) break;                       // 100 == distmax
    dist += step;
  }
  
  return dist;
}

psOut ps(psIn input) {
  psOut o;
  o.color = input.color * diffuse.Sample(diffuse_s, input.uv);
  o.depth = input.position.z;

  float3 origin = input.campos;
  float3 direction = normalize(input.world - origin);
  float ol = raymarch(origin, direction);
  clip(100 - (ol + 1));

  origin += ol * direction;

  clip(distance(input.campos, input.world) - distance(input.campos, origin));

  float t =  1 - (1 + dot(input.normal, _light)) / 2;
  o.color = float4(o.color.rgb * t, 1);


  // backface
  float3 localPos = mul(float4(input.world, 1), _matrix).xyz;
  
  if (localPos.y < -_dimensions.y / 2) {
    clip(-1);
  }

  if (dot(direction, input.normal) > 0) {
    o.color = float4(0.5, 0.5, 0.5, 1);
  }
  return o;
}
