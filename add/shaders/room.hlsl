#include "stereokit.hlsli"

//--name = dofdev/room

//--color:color = 1,1,1,1
//--tex_scale   = 1
//--diffuse     = white

float4       color;
float        tex_scale;
Texture2D    diffuse   : register(t0);
SamplerState diffuse_s : register(s0);

cbuffer BufferData : register(b3) {
  float4x4 oriel_matrix;
  float3   dimensions;
  float    time;
};

struct vsIn {
	float4 pos  : SV_Position;
	float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
	float4 col  : COLOR0;
};
struct psIn {
	float4 pos   : SV_Position;
  float3 world : WORLD;
	float2 uv    : TEXCOORD0;
	float4 color : COLOR0;
  float3 camdir : TEXCOORD1;
  float3 campos : TEXCOORD2;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	id        = id / sk_view_count;

  o.camdir = sk_camera_dir[o.view_id].xyz;
  o.campos = sk_camera_pos[o.view_id].xyz;

	o.world  = mul(input.pos, sk_inst[id].world).xyz;
	o.pos    = mul(float4(o.world, 1), sk_viewproj[o.view_id]);

	float3 normal = normalize(mul(input.norm, (float3x3)sk_inst[id].world));

	o.uv         = input.uv * tex_scale;
	o.color      = color * input.col * sk_inst[id].color;
	// o.color.rgb *= Lighting(normal);
	return o;
}

float sdBox(float3 p, float3 b) {
  float3 q = abs(p) - b;
  return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdSphere(float3 p, float r) {
  return length(p) - r;
}

float raymarch(float4x4 m, float3 ro, float3 rd) {
  ro = mul(float4(ro, 1), m).xyz;
  rd = mul(float4(rd, 0), m).xyz;
	float dist = 0.0;
  for (int i = 0; i < 256; i++) {
    float3 pos = ro + dist * rd;
    float step = sdBox(pos, dimensions / 2.0); 
    // float step = sdSphere(pos, dimensions.y / 2.0); 
    if (step < 0.0001 || dist > 100) break;              // 100 == distmax
    dist += step;
  }
  
  return dist;
}

// float4 read(float2 head, float x, float y) {
//   head += float2(x, y);
//   return diffuse.Sample(tex, head);
// }

float4 ps(psIn input) : SV_TARGET {
	float4 col = diffuse.Sample(diffuse_s, input.uv);
  
  // float2 head = float2(0, 0);

  // if (read(head, 0, 0).r > 0) {
  //   float4x4 m = float4x4(
  //     diffuse.Sample(tex, head + float2(1, 0)),
  //     diffuse.Sample(tex, head + float2(2, 0)),
  //     diffuse.Sample(tex, head + float2(3, 0)),
  //     diffuse.Sample(tex, head + float2(4, 0))
  //   );
  // }
  // float3 dim = float3(
  //   diffuse.Sample(tex, head + float2(1, 0)),
  // );


  float3 ro = input.campos;
  float3 rd = normalize(input.world - ro);
  float  ol = raymarch(oriel_matrix, ro, rd);

  clip(-(100 - (ol + 1)));
  // if ((100 - (ol + 1)) > 0) {
  //   col *= 0.1;
  // }

  ro += ol * rd;

  // clip((distance(input.campos, input.world) - distance(input.campos, ro)) * -1);
  // if ((distance(input.campos, input.world) - distance(input.campos, ro)) >= 0) {
  //   col *= 0.1;
  // }
  
  // if (input.world.y < bufferCenter.y) {
  //   col *= 0.1;
  // }


  // float value = (col.r + col.r + col.g + col.g + col.g + col.b) / 6;
	// return float4(value, value, value, 1);
  return col;
}