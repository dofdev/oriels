using System;
using StereoKit;

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
    ColorCube cube = new ColorCube();
    OrbitalView.strength = 4;
    OrbitalView.distance = 0.4f;
    cube.thickness = 0.01f;


    ReachCursor reachCursor = new ReachCursor();
    SupineCursor supineCursor = new SupineCursor();

    Oriel oriel = new Oriel();

    oriel.Start();

    while (SK.Step(() => {
      offHand = Input.Controller(Handed.Left);
      mainHand = Input.Controller(Handed.Right);

      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      // reachCursor.Step();
      supineCursor.Step();
      // oriel.Step();

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

  public void Start() {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));

    // Vertex[] verts = mesh.GetVerts();
    // for (int i = 0; i < verts.Length; i++) {
    //   verts[i].norm *= -1f;
    // }
    // mesh.SetVerts(verts); 
  }
  
  public void Step() {
    mesh.Draw(mat, Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
  }
}
