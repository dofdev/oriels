namespace Oriels;

public class Mono {
  private static readonly Lazy<Oriels.Mono> lazy = new Lazy<Oriels.Mono>(() => new Oriels.Mono());
  public static Oriels.Mono inst { get { return lazy.Value; } }

  public PullRequest.Noise noise = new PullRequest.Noise(939949595);

  public Material matDev;

  public Rig rig = new Rig();
  public Scene scene = new Scene();

  // -------------------------------------------------

  public dof[] dofs;
  int dofIndex = 0;
  dof dof => dofs[dofIndex];

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

    dofs = new dof[] {
      // new StretchFinger(),
      new WaveCursor() { handed = Handed.Left,  deadzone = 0.01f, strength = 3f }, 
      new WaveCursor() { handed = Handed.Right, deadzone = 0.01f, strength = 3f },
      new Trackballer() { handed = Handed.Left },
      new Trackballer() { handed = Handed.Right },
      // new StretchCursor() { deadzone = 0.01f, strength = 3f }, 
      // new StretchCursor() { deadzone = 0.01f, strength = 3f }, 
    };
  }

  public void Init() {


    // dofs[0].Init();
    // dofs[1].Init();


    // spaceMono.Init();
    greenyard.Init();

    matDev = Material.Default.Copy();
    matDev.SetTexture("diffuse", Tex.DevTex);
  }

  // -------------------------------------------------

  // Space.Mono spaceMono = new Space.Mono();
  Greenyard.Mono greenyard = new Greenyard.Mono();
  Board board = new Board();


  PullRequest.PID pid = new PullRequest.PID(8, 0.8f);

  // -------------------------------------------------

  public void Frame() {

    rig.Step();

    // -------------------------------------------------

    // THE BACK BREAKING PROBLEM WITH THE CURRENT DOF SYSTEM
    // is that I can't pass input to it in a dynamic way
    // a pointer would solve this problem but c# doesn't have pointers
    // except for unsafe code, which opens up a whole new can of worms
    
    // dof.Frame();    
    dofs[0].Frame();
    dofs[1].Frame();
    dofs[2].Frame();
    dofs[3].Frame();


    // turn this into a function
    Vec3 vA = new Vec3(-1, 0, 0);
    Vec3 vB = new Vec3(1, 1, 1);

    Vec3 vC = Input.Hand(Handed.Right).palm.position;

    Quat q = Quat.LookDir((vB - vA).Normalized);

    // // snap vC to line vA-vB
    // Vec3 local = q.Inverse * (vC - vA);
    // local.x = 0;
    // local.y = 0;
    // vC = q * local + vA;

		vC = vC.SnapToLine(vA, vB, true);

    Lines.Add(vA, vB, new Color(1, 1, 1), 0.002f);
    Mesh.Cube.Draw(matDev, Matrix.TRS(vC, q, 0.04f));


    // rGlove.Step(); lGlove.Step();

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

    // if (UI.Button("Draw Oriel Axis")) { oriel.drawAxis = !oriel.drawAxis; }

    // if (UI.Button("Reset Oriel Quat")) { oriel.ori = Quat.Identity; }
    // if (UI.Button("Scale w/Height")) { oriel.scaleWithHeight = !oriel.scaleWithHeight; }
    // UI.HSlider("Scale", ref oriel.scale, 0.1f, 1f, 0.1f);
    // UI.HSlider("Multiplier", ref oriel.multiplier, 0.1f, 1f, 0.1f);
    // UI.Label("Player.y");
    // UI.HSlider("Player.y", ref greenyard.height, 0.1f, 1.5f, 0.1f);

    UI.Label("trail.length");
    UI.HSlider("trail.length", ref trailLen, 0.1f, 1f, 0.1f);

    UI.Label("trail.scale");
    UI.HSlider("trail.str", ref trailScl, 0.1f, 2f, 0.1f);

    UI.Label("str");
    UI.HSlider("str", ref stretchStr, 0.1f, 1f, 0.1f);


    // flipIndex
    // flipGrip



    UI.WindowEnd();
  }
  public float trailLen = 0.333f;
  public float trailScl = 1f;
  public float stretchStr = 0.333f;



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