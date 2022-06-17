// float3 local  = input.world - _center;  
// clip(sign(_size - local.x) * sign(_size - local.y) * sign(_size - local.z));
// clip(sign(local.x + _size) * sign(local.y + _size) * sign(local.z + _size));  


// scale RGB by perceived luminance
// float value = (c.r + c.r + c.g + c.g + c.g + c.b) / 6;
// c *= value;


// if (trunc(H * 100) % 2 == 0) {
//   c = float3(1, 1, 1) - c;
// }


// float3 local = (input.world - _center) / _size * 2;
// float2 plane = float2(local.x, local.y);
// float H = acos(normalize(plane).y) / 3.141592653589793;
// if (sign(plane.x) < 0) {
//   H = (1 - H + 1) / 2;
// } else {
//   H = H / 2;
// }

// float C = distance(float2(0, 0), plane);
// float L = (1 + local.z) / 2;
// float3 c = HCLtoRGB(H, C, (L + 0.05) / 1.05);
// c *= L;