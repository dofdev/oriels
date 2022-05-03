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

  Vec3 boardDir = Vec3.Forward;
  public void Step() {
    rig.Step();
    scene.Step();

    // -------------------------------------------------

    // rGlove.Step(); lGlove.Step();

    // rBlock.Step(); lBlock.Step();

    // cubicCon.Step();

    // Vec3 fullstick = rig.Fullstick(false);

    // colorCube.Palm(lCon.device);

    // oriel.Step();
    ////



    // °board [ pseudo code ]
    // handling = 200±
    // speed = 10±
    // board.dir = Vec3.fwd

    // con.grip.frameDown: 
    //   handle = con

    // board.pos = FloorCenter
    // newDir = handle.pos.X0Z - board.pos.X0Z
    // board.dir = newDir.MagSq > 0.001f ? newDir.normalized : board.dir
    // board.ori = Quat.LookDir(board.dir)

    // twist = handle.grip * -(Quat.LookDir(board.dir).Inverse * handle.backhandDir).x
    // rig.ori *= Quat(0, twist * handling * delta, 0)

    // accel = handle.trigger
    // rig.pos += board.dir * accel * speed * delta


    // °board [ implementation ]
    float handling = 200;
    float speed = 10;

    Vec3 boardPos = rig.FloorCenter;
    Vec3 newDir = rig.HandleCon.pos.X0Z - boardPos.X0Z;
    boardDir = newDir.MagnitudeSq > 0.001f ? newDir.Normalized : boardDir;
    Quat boardOri = Quat.LookDir(boardDir);

    float twist = rig.HandleCon.device.grip * -(Quat.LookDir(boardDir).Inverse * rig.HandleCon.backhandDir).x;
    rig.ori *= Quat.FromAngles(0f, twist * handling * Time.Elapsedf, 0f);
    
    float accel = rig.HandleCon.device.trigger;
    rig.pos += boardDir * accel * speed * Time.Elapsedf;

    // Lines.Add(rig.HandleCon.pos, rig.HandleCon.pos + rig.HandleCon.backhandDir, Color.White, 0.01f);
    Mesh.Cube.Draw(Material.Default, Matrix.TRS(boardPos, boardOri, new Vec3(0.18f, 0.06f, 0.6f)));

    // DEPRECATED
    // PullRequest.Slerp(boardDir.Normalized, handleDelta.Normalized, handleDelta.Magnitude * handling * Time.Elapsedf) : boardDir;
    // Quat from = Quat.LookAt(rig.Head.position, rig.HandleCon.pos);
    // Lines.Add(rig.HandleCon.pos, rig.HandleCon.pos + from * Vec3.Up, Color.Black, 0.01f);


    // does the FloorCenter move with the CameraRoot?

    // having a board underneath my feet and a virtual handlebar in my hand
    // did a lot to improve the quality of the experience (+immersion -sickness)
    // the ability and quality at which this can be propagated is higher than I first imagined

    // next?
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
