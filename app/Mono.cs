namespace Oriels;

public class Mono {
  private static readonly Lazy<Oriels.Mono> lazy = new Lazy<Oriels.Mono>(() => new Oriels.Mono());
  public static Oriels.Mono inst { get { return lazy.Value; } }

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

  public void Init() {
    // spaceMono.Init();
    greenyard.Init();
  }


  // -------------------------------------------------

  // Space.Mono spaceMono = new Space.Mono();
  Greenyard.Mono greenyard = new Greenyard.Mono();


  PullRequest.PID pid = new PullRequest.PID(8, 0.8f);

  // -------------------------------------------------

  public void Step() {

    rig.Step();
    scene.Step();

    // -------------------------------------------------

    rGlove.Step(); lGlove.Step();

    // rBlock.Step(); lBlock.Step();

    // cubicCon.Step();

    // colorCube.Palm(lCon.device);

    oriel.Step();

    // -------------------------------------------------

    // spaceMono.Frame();
    greenyard.Frame();

    // -------------------------------------------------

    net.me.Step();
    net.send = true;

    ShowWindowButton();
  }

  Pose windowPoseButton = new Pose(0, 0, -1, Quat.Identity);
  void ShowWindowButton() {
    UI.WindowBegin("Window Button", ref windowPoseButton);

    if (UI.Button("Reset Oriel Quat")) { oriel.ori = Quat.Identity; }
    // if (UI.Button("Draw Oriel Axis")) { oriel.drawAxis = !oriel.drawAxis; }

    UI.WindowEnd();
  }
}
