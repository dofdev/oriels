using Oriels;

namespace Greenyard;
public class Mono {
  Model greenyardModel = Model.FromFile("greenyard.glb");
  Mesh[] greenyard;
  Material greenyardMat = new Material(Shader.FromFile("/shaders/oriel.hlsl"));

  Vec3 offset = new Vec3(0, 0, 0);

  public Mono() {
    
  }

  public void Init() {
    greenyard = new Mesh[12];
    for (int i = 0; i < greenyard.Length; i++) {
      greenyard[i] = greenyardModel.GetMesh("Object_" + (i + 2));
    }
    greenyardMat.SetMat(101, Cull.None, true);
    greenyardMat.SetTexture("diffuse", Tex.FromFile("greenyard.jpeg"));
  }

  public void Frame() {
    Oriels.Rig rig = Oriels.Mono.inst.rig;
    Oriels.Oriel oriel = Oriels.Mono.inst.oriel;

    Matrix simMatrix = Matrix.TRS(
      new Vec3(0, -oriel.bounds.dimensions.y / 2.01f, 0), 
      Quat.Identity,
      Vec3.One * 0.1f * oriel.bounds.dimensions.y
    );

    // Render
    greenyardMat.SetVector("_center", oriel.bounds.center);
    greenyardMat.SetVector("_dimensions", oriel.bounds.dimensions);
    greenyardMat.SetVector("_light", oriel.ori * new Vec3(0.6f, -0.9f, 0.3f));
    greenyardMat.SetFloat("_lit", 0);
    greenyardMat["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrix);
    for (int i = 0; i < greenyard.Length; i++) {
      greenyard[i].Draw(greenyardMat,
        Matrix.TRS(
          offset,
          Quat.Identity,
          new Vec3(1f, 1f, 1f)
        ) * simMatrix * oriel.matrix.Inverse,
        Color.White
      );
    }
  }
}