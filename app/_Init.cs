global using System;
global using StereoKit;
global using Oriels;

SKSettings settings = new SKSettings {
  appName = "oriels",
  assetsFolder = "add",
  depthMode = DepthMode.D32,
  disableUnfocusedSleep = true,
};
if (!SK.Initialize(settings))
  Environment.Exit(1);

Input.HandSolid(Handed.Max, false);
Input.HandVisible(Handed.Max, true);

Mono mono = Mono.inst;
while (SK.Step(() => {
  mono.Step();
})) ;
SK.Shutdown();