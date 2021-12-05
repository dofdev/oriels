#include "stereokit.hlsli"

//--name = dofdev/oriel
// float4       color;
float _height;
float _ypos;
float3 _dimensions;
float3 _center;
Texture2D tex; // : register(t0);
SamplerState tex_s; // : register(s0);

cbuffer BufferData : register(b3) {
  float3 windDirection;
  float  windStrength;
};

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

// float tri_raycast(float3 origin, float3 dir) {
//   float final = -1;
//   float3 v0 = tri[0].xyz;
//   float3 v1 = tri[1].xyz;
//   float3 v2 = tri[2].xyz;
//   float3 e1 = v1 - v0;
//   float3 e2 = v2 - v0;
//   float3 h = cross(dir, e2);
//   float a = dot(e1, h);
//   if (a > -0.00001 && a < 0.00001) {} else{
//     float f = 1 / a;
//     float3 s = origin - v0;
//     float u = f * dot(s, h);
//     if (u < 0.0 || u > 1.0) {} else {
//       float3 q = cross(s, e1);
//       float v = f * dot(dir, q);
//       if (v < 0.0 || u + v > 1.0) {} else {
//         float t = f * dot(e2, q);
//         if (t > 0.00001) { final = 1.0;} // t
//       }
//     }
//   }
//   return final;
// }

float sdSphere(float3 p, float s) {
  return length(p) - s;
}

float sdPlane(float3 p, float3 n, float h)
{
  // n must be normalized
  return dot(p,n) + h;
}

float sdBox(float3 p, float3 b)
{
  float3 q = abs(p) - b;
  return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdOctahedron(float3 p, float s)
{
  p = abs(p);
  return (p.x + p.y + p.z - s) * 0.57735027;
}

float map(float3 pos) {
  float sphere = sdSphere(pos + float3(0, 0.5, 0), 0.1);
  // return sdLink(pos, 0.1, 0.1, 0.1);
  float octo = sdOctahedron(pos, 0.1);
  // return lerp(sphere, octo, windStrength);

  float plane = sdPlane(pos, float3(0, 1, 0), 0);

  float phere = lerp(plane, sphere, windStrength);
  return min(phere, octo);
}

float3 calcNormal(float3 pos)
{
  float2 e = float2(1.0, -1.0) * 0.5773;
  float eps = 0.0005;
  return normalize( 
    e.xyy * map(pos + e.xyy * eps) + 
    e.yyx * map(pos + e.yyx * eps) + 
    e.yxy * map(pos + e.yxy * eps) + 
    e.xxx * map(pos + e.xxx * eps) 
  );
}

float calcAO(float3 pos, float3 nor)
{
	float occ = 0.0;
  float sca = 1.0;
  for (int i = 0; i < 5; i++) {
    float h = 0.01 + 0.12 * float(i)/4.0;
    float d = map(pos + h * nor).x;
    occ += (h - d) * sca;
    sca *= 0.95;
    if (occ > 0.35) break;
  }
  return clamp(1.0 - 3.0 * occ, 0.0, 1.0) * (0.5 + 0.5 * nor.y);
}


// float RayMarch(vec3 ro, vec3 rd) {
// 	float dO=0.;
    
//   for(int i=0; i<MAX_STEPS; i++) {
//     vec3 p = ro + rd*dO;
//     float dS = GetDist(p);
//     dO += dS;
//     if(dO>MAX_DIST || dS<SURF_DIST) break;
//   }
  
//   return dO;
// }

float4 ps(psIn input) : SV_TARGET {
  float3 ro = input.world; // ray origin
  if (!(sdBox(input.campos, _dimensions / 2) > 0.0)) {
    ro = input.campos;
    // always cull front
    // then replace the input.world with a raymarched box position
    // brings hands into the space
  }
  float3 rd = normalize(input.world - input.campos); // ray direction
  // input.color = float4(float3(1,1,1) * max(tri_raycast(input.world, ray), 0.0), 1);

  // raymarch
  float tmax = 3.0;
  float t = 0.0;
  for (int i = 0; i < 256; i++) {
    float3 pos = ro + t * rd;
    float h = map(pos);
    if (h < 0.0001 || t > tmax) break;
    t += h;
  }

  // shading/lighting	
  float3 col = float3(0.0, 0.0, 0.0);
  if (t < tmax)
  {
    float3 pos = ro + t * rd;
    float3 nor = calcNormal(pos);
    float dif = clamp(dot(nor, float3(0.7, 0.6, 0.4)), 0.0, 1.0);
    float amb = 0.5 + 0.5 * dot(nor, float3(0.0, 0.8, 0.6));
    float ao = calcAO(pos, nor);
    dif *= ao;
    col = float3(0.2, 0.3, 0.4) * amb + float3(0.8, 0.7, 0.5) * dif;
  }

  // input.color = float4(float3(1,1,1) * max(t, 0.0), 1);
  input.color = float4(col, 1);

  // input.color = float4(float3(1,1,1) * sdSphere(input.uv, float2(0.2, 0.2), float2(0.8, 0.8)), 1);
  // input.color.r = rr;
  return input.color;
}