global using System;
global using StereoKit;
// global using Oriels;

SKSettings settings = new SKSettings {
  appName = "oriels",
  assetsFolder = "add",
  depthMode = DepthMode.D32,
  disableUnfocusedSleep = true,
  displayPreference = DisplayMode.Flatscreen,
  // disableFlatscreenMRSim = true,
};
if (!SK.Initialize(settings))
  Environment.Exit(1);

Input.HandSolid(Handed.Max, false);
Input.HandVisible(Handed.Max, true);
// Input.HandMaterial(Handed.Max, Material.Default);
Renderer.EnableSky = false;
Renderer.ClearColor = new Color(0f / 256f, 162f / 256f, 206f / 256f);

Oriels.Mono mono = Oriels.Mono.inst;
mono.Init();
SK.Run(() => {
  mono.Frame();
});