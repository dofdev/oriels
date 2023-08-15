#ifndef _DOFDEV_HLSLI
#define _DOFDEV_HLSLI

struct oriel {
  float4x4 transform;
  float3   dimensions;
  float    x;
}; // sizeof(oriel) = 80bytes
cbuffer oriel_buffer : register(b3) {
  oriel oriels[819]; // UINT16_MAX(65535) / sizeof(oriel) = 819
};



#endif