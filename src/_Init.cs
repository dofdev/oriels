global using System;
global using StereoKit;
using System.Text.RegularExpressions;
// global using Oriels;

SKSettings settings = new SKSettings {
  appName = "oriels",
  assetsFolder = "add",
  depthMode = DepthMode.D32,
  disableUnfocusedSleep = true,
  // displayPreference = DisplayMode.Flatscreen,
  // disableFlatscreenMRSim = true,
};
if (!SK.Initialize(settings))
  Environment.Exit(1);

Input.HandSolid(Handed.Max, false);
Input.HandVisible(Handed.Max, true);
// Input.HandMaterial(Handed.Max, Material.Default);

Renderer.Scaling = 2;
Renderer.Multisample = 0;
Renderer.SetClip(0.01f, 100f);
Renderer.EnableSky = false;
Renderer.ClearColor = new Color(0f / 256f, 162f / 256f, 206f / 256f);
Renderer.LayerFilter = RenderLayer.All & ~RenderLayer.Layer1 & ~RenderLayer.Layer2;
Renderer.SetFOV(90f);

Oriels.Mono mono = Oriels.Mono.inst;
mono.Init();
SK.Run(() => {
  mono.Frame();
});