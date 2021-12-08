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
  float3 position;
  float  windStrength;
};

struct vsIn {
  float4 pos  : SV_POSITION;
  float3 norm : NORMAL0;
  float2 uv   : TEXCOORD0;
  float4 col  : COLOR0;
};
struct psIn {
  float4 color : COLOR0;
  float4 pos   : SV_POSITION;
  float3 norm  : NORMAL2;
  float2 uv    : TEXCOORD0;
  float3 campos : TEXCOORD1;
  float3 world : TEXCOORD2;
  uint view_id : SV_RenderTargetArrayIndex;
};
struct psOut {
	float4 color : SV_Target;
	float  depth : SV_Depth;
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

float sdPlane(float3 p, float3 n, float h) {
  // n must be normalized
  return dot(p,n) + h;
}

float sdBox(float3 p, float3 b) {
  float3 q = abs(p) - b;
  return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdOctahedron(float3 p, float s) {
  p = abs(p);
  return (p.x + p.y + p.z - s) * 0.57735027;
}

float sdBoxFrame(float3 p, float3 b, float e) {
  p = abs(p) - b;
  float3 q = abs(p + e) - e;
  return min(
    min(
      length(max(float3(p.x,q.y,q.z),0.0))+min(max(p.x,max(q.y,q.z)),0.0),
      length(max(float3(q.x,p.y,q.z),0.0))+min(max(q.x,max(p.y,q.z)),0.0)
    ),
    length(max(float3(q.x,q.y,p.z),0.0))+min(max(q.x,max(q.y,p.z)),0.0)
  );
}

float opRep(float3 p, float3 c)
{
  float3 q = modf(p + 0.5 * c, c) - 0.5 * c;
  return sdSphere(q, 0.1);
}

float map(float3 pos) {
  // pos.x = _center.x + pos.x;
  // pos.y = _center.y + pos.y;
  // pos.z = _center.z - pos.z;
  float sphere = sdSphere(pos + float3(0, 0, -1) - _center, 0.1);
  // return sdLink(pos, 0.1, 0.1, 0.1);
  // float octo = sdOctahedron(pos - _center - position, 0.2);
  float frame = sdBoxFrame(pos - _center - position, float3(0.06, 0.06, 0.06), 0.004);

  float orielFrame = sdBoxFrame(pos - _center, _dimensions / 2, 0.004);
  // return lerp(sphere, octo, windStrength);
  float plane = sdPlane(pos - _center + float3(0, 0.5, 0), float3(0, 1, 0), 0);

  // float blendd = lerp(octo, frame, windStrength);
  // return min(min(plane, orielFrame), frame);
  return min(min(plane, orielFrame), sphere);
  
  // return opRep(pos - _center, float3(0, 0, 0));
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

float calcShadow(float3 pos, float3 light) {
  float3 rd = normalize(light - pos);
  float3 ro = pos + rd * 0.1;

  float tmax = 100;
  float t = 0.0;
  for (int i = 0; i < 256; i++) {
    float3 pos = ro + t * rd;
    float h = map(pos);
    if (h < 0.0001 || t > tmax) break;
    t += h;
  }
  if (t < tmax) {
    t = 0;
  } else {
    t = 1;
  }

  return t;
}

float calcSoftshadow(in float3 ro, in float3 rd, in float mint, in float tmax, in float k) {
  // bounding volume
  float tp = (0.8-ro.y)/rd.y; if( tp>0.0 ) tmax = min( tmax, tp );

  float res = 1.0;
  float t = mint;
  for( int i=0; i<24; i++ ) {
  float h = map( ro + rd*t ).x;
    float s = clamp(8.0*h/t,0.0,1.0);
    res = min( res, k*s*s*(3.0-2.0*s) );
    t += clamp( h, 0.02, 0.2 );
    if( res<0.004 || t>tmax ) break;
  }
  return clamp( res, 0.0, 1.0 );
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

psOut ps(psIn input) {
  psOut result;

  result.depth = input.pos.z;

  float3 worldIntersection;
  float3 ro = input.campos; // ray origin
  float3 rd = normalize(input.world - ro); // ray direction
  // input.color = float4(float3(1,1,1) * max(tri_raycast(input.world, ray), 0.0), 1);

  // raymarch
  float tmax = 100;
  float t = 0.0;
  for (int i = 0; i < 256; i++) {
    float3 pos = ro + t * rd;
    float h = map(pos);
    if (h < 0.0001 || t > tmax) break;
    t += h;
  }

  // shading/lighting	
  float3 col = float3(0.5, 0.75, 0.9);
  if (t < tmax) {
    float3 pos = ro + (t * rd);
    float3 light = float3(0.0, 1.0, 0.0);
    float3 lightDir = normalize(light - pos);
    float3 nor = calcNormal(pos);
    float dif = clamp(dot(nor, lightDir), 0.0, 1.0);
    float amb = 0.5 + 0.5 * dot(nor, lightDir);
    float ao = calcAO(pos, nor);
    float sh = calcShadow(pos, light);
    // float sh = calcSoftshadow(pos, light, 0.02, 2.5, 32);
    dif *= ao * sh;
    col = float3(0.1, 0.5, 0.3) * amb + float3(0.6, 0.8, 0.3) * dif;

    // float3 lightPos = float3(0, 3, 0);
    // float3 rayo = pos;
    // float3 rayd = normalize(lightPos - pos);
    // float ttmax = 6.0;
    // float tt = 0.0;
    // for (int i = 0; i < 256; i++) {
    //   float3 pp = rayo + tt * rayd;
    //   float hh = map(pp);
    //   if (hh < 0.0001 || tt > tmax) break;
    //   tt += hh;
    // }
    // if (tt < length(lightPos - rayo)) {
    //   col *= 0.5;
    // }

    if(sdBox(pos - _center, _dimensions / 2) <= 0.005) {
      // float4x4 worldToClipMatrix = sk_view[input.view_id];

      // float4 clipPos = mul(float4(pos, 1), sk_viewproj[input.view_id]);

      // result.depth = clipPos.z/clipPos.w;

      float4 viewWorldPos = mul(float4(pos, 1), sk_view[input.view_id]);

      float near = 0.05;
      float far = 100;

      float a = (far+near)/(far-near);
      float b = 2.0*far*near/(far-near);
      result.depth = a + b/viewWorldPos.z;

      // result.depth = clipPos.z;

      // float4 properPos = normalize(input.pos)*(1/t);
      // float4 properPos = input.pos;
      // result.depth = properPos.z;
    }
  }

  // input.color = float4(float3(1,1,1) * max(t, 0.0), 1);
  input.color = float4(col, 1);

  // input.color = float4(float3(1,1,1) * sdSphere(input.uv, float2(0.2, 0.2), float2(0.8, 0.8)), 1);
  // input.color.r = rr;
  result.color = input.color;

  // float4x4 worldToViewMatrix = sk_view[input.view_id];
  // float4 viewIntersectionPos = worldToViewMatrix * float4(worldIntersection, 1.0);

  // float n = 0.0f;
  // float f = 200.0f;

  // result.depth = (-viewIntersectionPos.z - n) / (f - n) * viewIntersectionPos.w;

  // worldIntersection = float3(0.0);
///input.pos.w;
  // result.depth = zc/wc;
  // result.color.rgb = float3(zc/wc);

  return result;
}