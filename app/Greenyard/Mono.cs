using Oriels;

namespace Greenyard;
public class Mono {
  // Model greenyardModel = Model.FromFile("greenyard.glb");
  // Mesh[] greenyard;
  // Material greenyardMat = new Material(Shader.FromFile("/shaders/oriel.hlsl"));

  Vec3 offset = new Vec3(2, 0, -2);

  Thing[] thing;
  Model model = Model.FromFile("/backrooms/backrooms.glb");

  public Mono() {
    
  }

  public void Init() {
    // greenyard = new Mesh[12];
    // for (int i = 0; i < greenyard.Length; i++) {
    //   greenyard[i] = greenyardModel.GetMesh("Object_" + (i + 2));
    // }
    // greenyardMat.SetMat(101, Cull.None, true);
    // greenyardMat.SetTexture("diffuse", Tex.FromFile("greenyard.jpeg"));

    thing = new Thing[] {
      new Thing(
        model.GetMesh("Carpet"), 
        new Material(Shader.FromFile("/shaders/oriel.hlsl")),
        "backrooms/Carpet.png"
      ),
      new Thing(
        model.GetMesh("Walls"), 
        new Material(Shader.FromFile("/shaders/oriel.hlsl")),
        "backrooms/Walls.png"
      ),
      new Thing(
        model.GetMesh("Ceiling"),
        new Material(Shader.FromFile("/shaders/oriel.hlsl")),
        "backrooms/Ceiling.png"
      ),
      new Thing(
        model.GetMesh("Vents"),
        new Material(Shader.FromFile("/shaders/oriel.hlsl")),
        "backrooms/Vents.png"
      ),
      new Thing(
        model.GetMesh("Lights"),
        new Material(Shader.FromFile("/shaders/oriel.hlsl")),
        "backrooms/Lights.png"
      ),
    };
  }

  public void Frame() {
    Oriels.Rig rig = Oriels.Mono.inst.rig;
    Oriels.Oriel oriel = Oriels.Mono.inst.oriel;

    // Oriel
    float scale = oriel.scale * oriel.multiplier;
    if (oriel.scaleHeight) {
      scale *= oriel.bounds.dimensions.y;
    }

    Matrix simMatrix = Matrix.TRS(
      new Vec3(0, -oriel.bounds.dimensions.y / 2.01f, 0), 
      Quat.Identity,
      Vec3.One * scale
    );

    // Render
    // greenyardMat.SetVector("_center", oriel.bounds.center);
    // greenyardMat.SetVector("_dimensions", oriel.bounds.dimensions);
    // greenyardMat.SetVector("_light", oriel.ori * new Vec3(0.6f, -0.9f, 0.3f));
    // greenyardMat.SetFloat("_lit", 0);
    // greenyardMat["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrixInv);
    // for (int i = 0; i < greenyard.Length; i++) {
    //   greenyard[i].Draw(greenyardMat,
    //     Matrix.TRS(
    //       offset,
    //       Quat.Identity,
    //       new Vec3(1f, 1f, 1f)
    //     ) * simMatrix * oriel.matrix,
    //     Color.White
    //   );
    // }

    for (int i = 0; i < thing.Length; i++) {
      Thing t = thing[i];
      t.mat.SetVector("_center", oriel.bounds.center);
      t.mat.SetVector("_dimensions", oriel.bounds.dimensions);
      t.mat.SetVector("_light", oriel.ori * new Vec3(0.6f, -0.9f, 0.3f));
      t.mat.SetFloat("_lit", 0);
      t.mat["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrixInv);
      t.mesh.Draw(t.mat,
        Matrix.TRS(
          offset,
          Quat.Identity,
          new Vec3(1f, 1f, 1f)
        ) * simMatrix * oriel.matrix,
        Color.White
      );
    }
  }
}

[Serializable]
public class Thing {
  public Mesh mesh;
  public Material mat;

  public Thing(Mesh mesh, Material mat, string tex) {
    this.mesh = mesh;
    this.mat = mat;

    mat.SetMat(101, Cull.None, true);
    mat.SetTexture("diffuse", Tex.FromFile(tex));
  }
}