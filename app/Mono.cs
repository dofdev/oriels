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
  Board board = new Board();


  PullRequest.PID pid = new PullRequest.PID(8, 0.8f);

  // -------------------------------------------------

  public void Step() {

    rig.Step();

    // -------------------------------------------------

    rGlove.Step(); lGlove.Step();

    // rBlock.Step(); lBlock.Step();

    // cubicCon.Step();

    // colorCube.Palm(lCon.device);

    oriel.Frame();

    scene.Step(); // after! (render scene after oriel)

    // -------------------------------------------------

    // spaceMono.Frame();
    greenyard.Frame();
    // board.Frame();

    // -------------------------------------------------

    oriel.Render();

    net.me.Step();
    net.send = true;

    ShowWindowButton();
  }

  Pose windowPoseButton = new Pose(0, 0, 0.75f, Quat.Identity);
  void ShowWindowButton() {
    UI.WindowBegin("Window Button", ref windowPoseButton);

    if (UI.Button("Reset Oriel Quat")) { oriel.ori = Quat.Identity; }
    // if (UI.Button("Draw Oriel Axis")) { oriel.drawAxis = !oriel.drawAxis; }
    if (UI.Button("Scale w/Height")) { oriel.scaleHeight = !oriel.scaleHeight; }
    UI.HSlider("Scale", ref oriel.scale, 0.1f, 1f, 0.1f);
    UI.HSlider("Multiplier", ref oriel.multiplier, 0.1f, 1f, 0.1f);


    UI.WindowEnd();
  }
}



[Serializable]
public class Board {
  // velocity button controls
  // "center off mass" thrust vector steering

  Mesh meshCube = Mesh.Cube;

  public bool goofy = false;

  public Vec3 front, back, pos, dir, vector;
  public float vel;

  float length = 1.5f;
  public Board() {
    front = new Vec3(0, 0, -1) * length / 2;
    back = new Vec3(0, 0, 1) * length / 2;
    dir = new Vec3(0, 0, -1);
    vel = 0;
  }

  public void Frame() {
    // tilt steer ("back wheel" turns *rudder)
    // button velocity controls
    Con con = goofy ? Mono.inst.rig.rCon : Mono.inst.rig.lCon;
    float accel = con.device.trigger;
    float deccel = con.device.grip;
    float lean = Mono.inst.rig.LocalPos(Mono.inst.rig.Head.position).x;

    // float length = 1.5f;
    float drag = 0.1f;
    float speed = 4f;
    float brake = 12f;
    float tight = 60f;


    // velocity
    vel *= 1f - (drag * Time.Elapsedf);
    vel += accel * speed * Time.Elapsedf;
    vel = MathF.Max(vel - deccel * brake * Time.Elapsedf, 0);
   
    // steering
    vector = Quat.LookDir(dir) * Quat.FromAngles(0, lean * tight, 0) * Vec3.Forward;
    back += vector * vel * Time.Elapsedf;
    dir = (front - back).Normalized;

    front = back + dir * length;
    pos = Vec3.Lerp(front, back, 0.5f);


    Mono.inst.rig.pos = pos;
    Mono.inst.rig.ori = Quat.LookDir(dir);

    // hover
    // if (rig.lCon.gripBtn.held) {
    //   rig.pos.y = pid.Update(rig.LocalPos(rig.Head.position).y);
    // }


    meshCube.Draw(Material.Default,
      Matrix.TRS(
        Mono.inst.rig.FloorCenter,
        Quat.LookDir(dir),
        new Vec3(0.05f, 0.2f, 0.2f)
      )
    );

    // mini board in hand for debugging
    // meshCube.Draw(Material.Default,
    //   Matrix.TRS(
    //     rig.rCon.pos,
    //     board.ori,
    //     new Vec3(0.4f, 0.1f, reach * 2) * 0.1f
    //   )
    // );
    // // front wheel
    // meshCube.Draw(Material.Default,
    //   Matrix.TRS(
    //     rig.rCon.pos + board.dir * reach * 0.1f,
    //     Quat.LookDir(frontDir),
    //     new Vec3(0.05f, 0.2f, 0.2f) * 0.1f
    //   )
    // );





    // pillars to hover through
    // spawn in front every 1m away from last
    if (Vec3.Distance(pos, lastSpawnPos) > 1f) {
      // odd or even
      float chirality = pillarIndex % 2 == 0 ? 1f : -1f;
      pillars[pillarIndex] = pos + Quat.LookDir(dir) * new Vec3(chirality * 8 * Mono.inst.noise.uvalue, 0, -24);

      lastSpawnPos = pos;

      pillarIndex++;
      if (pillarIndex >= pillars.Length) { pillarIndex = 0; }
    }

    for (int i = 0; i < pillars.Length; i++) {
      meshCube.Draw(Material.Default,
        Matrix.TRS(
          pillars[i],
          Quat.Identity,
          new Vec3(0.1f, 20f, 0.1f)
        )
      );
    }
  }
  Vec3[] pillars = new Vec3[64];
  int pillarIndex = 0;
  Vec3 lastSpawnPos = Vec3.Zero;
}