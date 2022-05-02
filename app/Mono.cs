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
  }

  Vec3 pos = new Vec3(0f, 0f, 0f);
  Quat ori = Quat.Identity;
  float yy = 0f;
  public void Step() {
    Renderer.CameraRoot = Matrix.TR(pos, ori);

    rig.Step();
    scene.Step();

    // -------------------------------------------------

    // rGlove.Step(); lGlove.Step();

    // rBlock.Step(); lBlock.Step();

    // cubicCon.Step();

    // Vec3 fullstick = rig.Fullstick(false);

    // colorCube.Palm(lCon.device);

    // oriel.Step();

    // board
    // use the active con position to point from the head *top down perspective (y = 0)
    // to determine the board direction
    Con handleCon = rig.HandleCon();
    Vec3 boardDir = (handleCon.pos.X0Z - rig.Head().position.X0Z).Normalized;
    Quat boardRot = Quat.LookDir(boardDir);

    // boardDir = (handleCon.pos.X0Z - head.position.X0Z).normalized
    // boardRot = Quat.LookDir(boardDir)
    // boardPos += boardDir * handleCon.trigger * speed * delta

    pos += boardDir * handleCon.device.trigger * Time.Elapsedf;
    yy += handleCon.device.stick.x * 90 * Time.Elapsedf;
    ori = Quat.FromAngles(0f, yy, 0f); // stick.x -> twist z


    Vec3 boardPos = pos + Vec3.Up * -1.35f; // rig.Head().position.X0Z
    Mesh.Cube.Draw(Material.Default, Matrix.TRS(boardPos, boardRot, new Vec3(0.18f, 0.06f, 0.6f)));

    // having a board underneath my feet and a virtual handlebar in my hand
    // did a lot to improve the quality of the experience (+immersion -sickness)

    // don't do more just yet!
    // these are all important, but lets start small and scrappy + it's 9AM
    // twist turning
    // handleCon
    // tighter boardDir
    // lean turning (head moving in relation to hand, doesn't that happen a little already?)

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
