using StereoKit;
using System;

class Program {
	static void Main(string[] args) {
    SKSettings settings = new SKSettings {
      appName = "oriels",
      assetsFolder = "Assets",
    };
		if (!SK.Initialize(settings))
      Environment.Exit(1);

    // TextStyle style = Text.MakeStyle(Font.FromFile("DMMono-Regular.ttf"), 0.1f, Color.White);

    Mono.Run();
	}
}

public static class Mono {

  public static Controller offHand, mainHand;

  public static void Run() {
    Random rnd = new Random();
    MonoNet net = new MonoNet(rnd.Next(1, 256)); // temp, until unique usernames
    net.Start();

    ColorCube cube = new ColorCube();
    OrbitalView.strength = 4;
    OrbitalView.distance = 0.4f;
    cube.thickness = 0.01f;

    // StretchCursor stretchCursor = new StretchCursor();
    // ReachCursor reachCursor = new ReachCursor();
    // SupineCursor supineCursor = new SupineCursor();
    // ClawCursor clawCursor = new ClawCursor();

    // Oriel oriel = new Oriel();
    // oriel.Start();

    Lerper lerper = new Lerper();

    Tex tex = new Tex(TexType.Image, TexFormat.Rgba32);
    tex.SetSize(128, 128);
    tex.SampleMode = TexSample.Point;
    

    Material material = Default.Material;
    material.SetTexture("diffuse", tex);
    Mesh quad = Default.MeshQuad;

    while (SK.Step(() => {
      offHand = Input.Controller(Handed.Left);
      mainHand = Input.Controller(Handed.Right);

      mainHand.aim = Input.Hand(Handed.Right).palm;

      // stretchCursor.Step(offHand.aim, mainHand.aim);


      net.cursor = Vec3.Up * (float)Math.Sin(Time.Total);
      net.head = Input.Head;
      net.offHand = offHand.aim;
      net.mainHand = mainHand.aim;
      for (int i = 0; i < net.peers.Length; i++) {
        MonoNet.Peer peer = net.peers[i];
        if (peer != null) {
          net.Cubee(Matrix.TRS(peer.cursor, Quat.Identity, Vec3.One * 0.05f));
          // Cubee(NetPose(peer.Head.Value).ToMatrix(Vec3.One * 0.3f));
          // Cubee(NetPose(peer.LHand.Value).ToMatrix(Vec3.One * 0.1f));
          // Cubee(NetPose(peer.RHand.Value).ToMatrix(Vec3.One * 0.1f));
        }
      } 


      // bool KeyDown(Key key) => Input.Key(key).IsActive();

      // bool[,] bitting = new bool[,] {
      //   {KeyDown(Key.F), false, KeyDown(Key.D), false, KeyDown(Key.S), false, KeyDown(Key.A)},
      //   {false, false, false, false, false, false, false},
      //   {KeyDown(Key.J), false, KeyDown(Key.K), false, KeyDown(Key.L), false, KeyDown(Key.Semicolon)},
      // };

      
      // bitting[1,0] = bitting[0,0] && bitting[2,0];
      // bitting[1,2] = bitting[0,2] && bitting[2,2];
      // bitting[1,4] = bitting[0,4] && bitting[2,4];
      // bitting[1,6] = bitting[0,6] && bitting[2,6];
      
      // bitting[0, 1] = bitting[0, 0] && bitting[0, 2];
      // bitting[0, 3] = bitting[0, 2] && bitting[0, 4];
      // bitting[0, 5] = bitting[0, 4] && bitting[0, 6];

      // bitting[2, 1] = bitting[2, 0] && bitting[2, 2];
      // bitting[2, 3] = bitting[2, 2] && bitting[2, 4];
      // bitting[2, 5] = bitting[2, 4] && bitting[2, 6];

      // Color32[] pixels = new Color32[tex.Width * tex.Height];
      // tex.GetColors(ref pixels);
      // for (int i = 0; i < pixels.Length; i++) {
      //   pixels[i] = new Color32(0, 0, 0, 0);
      //   int x = i % tex.Width;
      //   int y = i / tex.Width;
      //   if (x < 3 && y < 7 && bitting[x, y]) {
      //     pixels[i] = new Color32(0, 255, 255, 0);
      //   }
      // }
      // tex.SetColors(tex.Width, tex.Height, pixels);

      // quad.Draw(material, Matrix.TR(Vec3.Zero, Quat.FromAngles(0, 180, 0)));




      // domHand subHand ?? :3

      // if (offHand.trigger.) {
      //   lerper.t = 0;
      // }
      // lerper.Step(1, false);
      // Console.WriteLine(lerper.t);

      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      // reachCursor.Step();
      // supineCursor.Step(
      //   new Pose(Vec3.Zero, offHand.aim.orientation),
      //   new Pose(mainHand.aim.position, mainHand.aim.orientation),
      //   mainHand.IsStickClicked
      // );
      // clawCursor.Step(
      //   Input.Head.position - Vec3.Up * 0.2f,
      //   new Pose(offHand.aim.position, offHand.aim.orientation),
      //   new Pose(mainHand.aim.position, mainHand.aim.orientation),
      //   mainHand.IsStickClicked
      // );
      
      // oriel.Step();

      // cursor.Draw(Matrix.S(0.1f));
    }));
    SK.Shutdown();
  }
}

public class Lerper
{
  public float t = 0;
  public float spring = 1;
  public float dampen = 1;
  float vel;

  public void Step(float to = 1, bool bounce = false)
  {
    float dir = to - t;
    vel += dir * spring * Time.Elapsedf;

    if (Math.Sign(vel) != Math.Sign(dir))
    {
      vel *= 1 - (dampen * Time.Elapsedf);
    }
    else
    {
      vel *= 1 - (dampen * 0.33f * Time.Elapsedf);
    }

    float newt = t + vel * Time.Elapsedf;
    if (bounce && (newt < 0 || newt > 1))
    {
      vel *= -0.5f;
      newt = Math.Clamp(newt, 0, 1);
    }

    t = newt;
  }

  public void Reset()
  {
    t = vel = 0;
  }
}


public class Oriel {
  public Bounds bounds;

  // render
  // Model model = Model.FromFile("oriel.glb", Shader.FromFile("oriel.hlsl"));
  Material mat = new Material(Shader.FromFile("oriel.hlsl"));
  Mesh mesh = Default.MeshCube;
  Vec3 _dimensions;
  public void Start() {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));
    _dimensions = bounds.dimensions;
  }
  
  public void Step() {
    // circle around center
    // bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;


    // bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));
    
    mat.Transparency = Transparency.Blend;
    // mat.FaceCull = Cull.Front;
    mat.SetFloat("_height", bounds.dimensions.y);
    mat.SetFloat("_ypos", bounds.center.y);
    mesh.Draw(mat, Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
  }
}

public static class PullRequest {
  public static void BoundsDraw(Bounds b, float thickness, Color color) {
    Vec3 c = Vec3.One / 2;
    Vec3 ds = b.dimensions;
    for (int i = 0; i < 4; i++) {
      Quat q = Quat.FromAngles(i * 90, 0, 0);
      Lines.Add(q * (new Vec3(0, 0, 0) - c) *  ds, q * (new Vec3(0, 1, 0) - c) *  ds, color, color, thickness);
      Lines.Add(q * (new Vec3(0, 1, 0) - c) *  ds, q * (new Vec3(1, 1, 0) - c) *  ds, color, color, thickness);
      Lines.Add(q * (new Vec3(1, 1, 0) - c) *  ds, q * (new Vec3(1, 0, 0) - c) *  ds, color, color, thickness);

      // convert to linepoints
    }
  }

  // amplify quaternions (q * q * lerp(q.i, q, %))
}
