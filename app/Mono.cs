using System;
using StereoKit;

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
// TextStyle style = Text.MakeStyle(Font.FromFile("DMMono-Regular.ttf"), 0.1f, Color.White);

Mono mono = Mono.inst;
while (SK.Step(() => {
  mono.Step();
})) ;
SK.Shutdown();


public class Mono {
  private static readonly Lazy<Mono> lazy = new Lazy<Mono>(() => new Mono());
  public static Mono inst { get { return lazy.Value; } }

  public PullRequest.Noise noise = new PullRequest.Noise(939949595);

  public Rig rig = new Rig();
  public Scene scene = new Scene();

  // -------------------------------------------------
  public Oriel oriel = new Oriel(); // -> array ?

  public ColorCube colorCube = new ColorCube();

  public Glove rGlove = new Glove(true), lGlove = new Glove(false);
  public Glove Glove(bool chirality) { return chirality ? rGlove : lGlove; }

  public BlockCon rBlock = new BlockCon(true), lBlock = new BlockCon(false);
  public BlockCon BlockCon(bool chirality) { return chirality ? rBlock : lBlock; }
  public Block[] blocks = new Block[] { 
    new Block(), new Block(), new Block(), new Block(), new Block(), new Block() 
  };

  public CubicCon cubicCon = new CubicCon();
  public Cubic[] cubics = new Cubic[] { 
    new Cubic(), new Cubic(), new Cubic(), new Cubic(), new Cubic(), new Cubic() 
  };
  // -------------------------------------------------

  public MonoNet net = new MonoNet();

  public Mono() {
    Renderer.SetClip(0.02f, 1000f);

    // Console.WriteLine("noise = " + noise.value);
  }

  Vec3 pos = new Vec3(0f, 0f, 0f); // see below
  public void Step() {
    Renderer.CameraRoot = Matrix.T(pos);

    rig.Step();
    scene.Step();

    // -------------------------------------------------

    // Gloves
    rGlove.Step();
    lGlove.Step();

    // Blocks
    rBlock.Step();
    lBlock.Step();

    // Cubic
    cubicCon.Step();

    // Fullstick
    Vec3 fullstick = rig.Fullstick(false);

    // COLOR CUBE (RGB)
    // colorCube.Palm(lCon.device);



    oriel.Step();

    // -------------------------------------------------

    net.me.Step();
    net.send = true;

    ShowWindowButton();
  }

  Pose windowPoseButton = new Pose(0, 0, 0, Quat.Identity);
  void ShowWindowButton() {
    UI.WindowBegin("Window Button", ref windowPoseButton);

    if (UI.Button("Reset Oriel Quat")) { oriel.ori = Quat.Identity; }
    if (UI.Button("Draw Oriel Axis")) { oriel.drawAxis = !oriel.drawAxis; }

    UI.WindowEnd();
  }
}
