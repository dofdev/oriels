using System;
using StereoKit;
using AutoUpdaterDotNET;

class Program {
	static void Main(string[] args) {
    // AutoUpdater.InstalledVersion = new Version("0.0.0.2");
    // AutoUpdater.Start("https://github.com/dofdev/oriels/blob/main/oriels.xml");

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
    ColorCube cube = new ColorCube();
    OrbitalView.strength = 4;
    OrbitalView.distance = 0.4f;
    cube.thickness = 0.01f;

    ReachCursor reachCursor = new ReachCursor();
    SupineCursor supineCursor = new SupineCursor();
    BallsCursor ballsCursor = new BallsCursor();

    Oriel oriel = new Oriel();

    Pose p = new Pose(Vec3.One, Quat.Identity); // ACTUALLY COOL

    oriel.Start();

    while (SK.Step(() => {
      offHand = Input.Controller(Handed.Left);
      mainHand = Input.Controller(Handed.Right);

      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      // reachCursor.Step();
      // supineCursor.Step(
      //   offHand.aim.orientation,
      //   mainHand.aim.position,
      //   mainHand.aim.orientation,
      //   Mono.mainHand.IsStickClicked
      // );

      oriel.Step();

      // cursor.Draw(Matrix.S(0.1f));
    })) ;
    SK.Shutdown();
  }
}

public class Oriel {
  public Bounds bounds;

  // render
  Material mat = new Material(Shader.FromFile("oriel.hlsl"));
  Mesh mesh = Mesh.GenerateCube(new Vec3(1, 1, 1));
  Model model = Model.FromFile("oriel.glb", Default.ShaderUnlit);

  public void Start() {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));

    // Vertex[] verts = mesh.GetVerts();
    // for (int i = 0; i < verts.Length; i++) {
    //   verts[i].norm *= -1f;
    // }
    // mesh.SetVerts(verts); 
  }
  
  public void Step() {
    model.Draw(Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
    // mesh.Draw(mat, Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
  }
}

public static class PullRequest {
  public static Vec3 VecMulti(Vec3 a, Vec3 b) { return new Vec3(a.x * b.x, a.y * b.y, a.z * b.z); }

  public static void BoundsDraw(Bounds b, Color color) {
    Vec3 c = Vec3.One / 2;
    Vec3 ds = b.dimensions;
    for (int i = 0; i < 4; i++) {
      Quat q = Quat.FromAngles(i * 90, 0, 0);
      Lines.Add(q * VecMulti(new Vec3(0, 0, 0) - c, ds), q * VecMulti(new Vec3(0, 1, 0) - c, ds), color, color, 0.01f);
      Lines.Add(q * VecMulti(new Vec3(0, 1, 0) - c, ds), q * VecMulti(new Vec3(1, 1, 0) - c, ds), color, color, 0.01f);
      Lines.Add(q * VecMulti(new Vec3(1, 1, 0) - c, ds), q * VecMulti(new Vec3(1, 0, 0) - c, ds), color, color, 0.01f);
    }
  }
}
