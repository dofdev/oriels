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
    MonoNet net = new MonoNet();
    net.Start();

    // ColorCube cube = new ColorCube();
    // OrbitalView.strength = 4;
    // OrbitalView.distance = 0.4f;
    // cube.thickness = 0.01f;

    StretchCursor stretchCursor = new StretchCursor();
    // ReachCursor reachCursor = new ReachCursor();
    // SupineCursor supineCursor = new SupineCursor();
    // ClawCursor clawCursor = new ClawCursor();

    // Oriel oriel = new Oriel();
    // oriel.Start();

    // Lerper lerper = new Lerper();

    while (SK.Step(() => {
      offHand = Input.Controller(Handed.Left);
      mainHand = Input.Controller(Handed.Right);

      // mainHand.aim = Input.Hand(Handed.Right).palm;


      stretchCursor.Step(offHand.aim, mainHand.aim);
      net.me.cursor = stretchCursor.pos;
      // net.me.cursor = Vec3.Up * (float)Math.Sin(Time.Total);
      net.me.headset = Input.Head;
      net.me.offHand = offHand.aim;
      net.me.mainHand = mainHand.aim;
      for (int i = 0; i < net.peers.Length; i++) {
        MonoNet.Peer peer = net.peers[i];
        if (peer != null) {
          net.Cubee(Matrix.TRS(peer.cursor, Quat.Identity, Vec3.One * 0.05f));
          net.Cubee(peer.headset.ToMatrix(Vec3.One * 0.3f));
          net.Cubee(peer.offHand.ToMatrix(Vec3.One * 0.1f));
          net.Cubee(peer.mainHand.ToMatrix(Vec3.One * 0.1f));
        }
      } 

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

public class DrawKey {
  public int x, y;
  public Key key;
  public DrawKey(int x, int y, Key key) {
    this.x = x;
    this.y = y;
    this.key = key;
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

public class Bitting {

  Tex tex = new Tex(TexType.Image, TexFormat.Rgba32);  
  Material material = Default.Material;
  Mesh quad = Default.MeshQuad;
  int [,] bitchar = new int[,] {
    {0, 0, 0, 0, 0, 0, 0},
    {0, 0, 0, 0, 0, 0, 0},
    {0, 0, 0, 0, 0, 0, 0},
  };
  DrawKey[] drawKeys = new DrawKey[] {
    new DrawKey(0, 0, Key.F), new DrawKey(0, 2, Key.D), new DrawKey(0, 4, Key.S), new DrawKey(0, 6, Key.A),
    new DrawKey(2, 0, Key.J), new DrawKey(2, 2, Key.K), new DrawKey(2, 4, Key.L), new DrawKey(2, 6, Key.Semicolon),
  }; DrawKey lastKey = null;

  public void Start() {
    tex.SetSize(128, 128);
    tex.SampleMode = TexSample.Point;
    material.SetTexture("diffuse", tex);
  }

  public void Step() {
    // clear
    if (Input.Key(Key.Space).IsJustActive()) {
      for (int i = 0; i < bitchar.GetLength(0); i++) {
        for (int j = 0; j < bitchar.GetLength(1); j++) {
          bitchar[i, j] = 0;
        }
      }
      lastKey = null;
    }

    for (int i = 0; i < drawKeys.Length; i++){
      DrawKey drawKey = drawKeys[i];
      if (Input.Key(drawKey.key).IsJustActive()) {
        bitchar[drawKey.x, drawKey.y] = 1;
        if (lastKey != null) {
          // draw line between last and current
          int x1 = lastKey.x;
          int y1 = lastKey.y;
          int x2 = drawKey.x;
          int y2 = drawKey.y;
          int dx = Math.Abs(x2 - x1);
          int dy = Math.Abs(y2 - y1);
          int sx = x1 < x2 ? 1 : -1;
          int sy = y1 < y2 ? 1 : -1;
          int err = dx - dy;
          while (true) {
            bitchar[x1, y1] = 1;
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x1 += sx; }
            if (e2 < dx) { err += dx; y1 += sy; }
          }
        }
        lastKey = drawKey;
        break;
      }
    }

    Color32[] pixels = new Color32[tex.Width * tex.Height];
    tex.GetColors(ref pixels);
    for (int i = 0; i < pixels.Length; i++) {
      pixels[i] = new Color32(0, 0, 0, 0);
      int x = i % tex.Width;
      int y = i / tex.Width;
      if (x < 3 && y < 7 && bitchar[x, y] == 1) {
        pixels[i] = new Color32(0, 255, 255, 0);
      }
    }
    tex.SetColors(tex.Width, tex.Height, pixels);

    quad.Draw(material, Matrix.TR(Vec3.Zero, Quat.FromAngles(0, 180, 0)));
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
