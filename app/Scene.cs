using System;
using System.Runtime.InteropServices;
using StereoKit;

[StructLayout(LayoutKind.Sequential)]
struct BufferData {
  public Matrix matrix;
  public Vec3 dimensions;
  public float time;
}

public class Scene {
  MaterialBuffer<BufferData> buffer;
  BufferData data = new BufferData();

  Material matFloor = new Material(Shader.Default);
  // Model room = Model.FromFile("room/room.glb", Shader.FromFile("room.hlsl"));
  Model shed = Model.FromFile("shed/shed.glb", Shader.FromFile("room.hlsl"));
  // Model skatepark = Model.FromFile("skatepark/scene.gltf", Shader.FromFile("room.hlsl"));


  Solid floor;
  public Scene() {
    // Shader.FromFile("room.hlsl")
    buffer = new MaterialBuffer<BufferData>(3); // index


    floor = new Solid(World.BoundsPose.position, Quat.Identity, SolidType.Immovable);
    scale = 64f;
    floorScale = new Vec3(scale, 0.1f, scale);
    floor.AddBox(floorScale);
    // box on each side
    floor.AddBox(new Vec3(scale, scale / 2, 0.1f), 1, new Vec3(0, scale / 4, -scale / 2));
    floor.AddBox(new Vec3(scale, scale / 2, 0.1f), 1, new Vec3(0, scale / 4, scale / 2));
    floor.AddBox(new Vec3(0.1f, scale / 2, scale), 1, new Vec3(-scale / 2, scale / 4, 0));
    floor.AddBox(new Vec3(0.1f, scale / 2, scale), 1, new Vec3(scale / 2, scale / 4, 0));
    // and ceiling
    floor.AddBox(new Vec3(scale, 0.1f, scale), 1, new Vec3(0, scale / 2, 0));
    matFloor.SetTexture("diffuse", Tex.FromFile("floor.png"));
    matFloor.SetFloat("tex_scale", 32);
  }

  public float scale;
  public Vec3 floorScale;


  public void Step() {
    Oriel oriel = Mono.inst.oriel;
    data.matrix = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrix);
    data.dimensions = oriel.bounds.dimensions;








    // data.dimensions = Vec3.Zero;










    
    buffer.Set(data);

    // PullRequest.BlockOut(floor.GetPose().ToMatrix(floorScale), Color.White * 0.333f, matFloor);
    // foreach (ModelNode node in shed.Visuals) {

      // Console.WriteLine(i + " - " + node.Name);

      // node.Material.SetVector("_center", oriel.bounds.center);
      // node.Material.SetVector("_dimensions", oriel.bounds.dimensions);
      // node.Material["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrix);

      // Console.WriteLine("Shader: " + node.Material.Shader.Name);

      // node.Mesh.Draw(matRoom, Matrix.TRS(new Vec3(0, World.BoundsPose.position.y, -1), Quat.Identity, Vec3.One));
      // Console.WriteLine(matRoom.ParamCount + " test " + node.Material.ParamCount);
    // }
    // room.RootNode.Material.SetVector("_center", oriel.bounds.center);
    // room.RootNode.Material.SetVector("_dimensions", oriel.bounds.dimensions);
    // room.RootNode.Material["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrix);

    // Shader.
    // World.BoundsPose.position.y
    shed.Draw(Matrix.TRS(new Vec3(0, -1.6f, 0), Quat.Identity, Vec3.One));
    // skatepark.Draw(Matrix.TRS(new Vec3(0, -5.6f, 0), Quat.Identity, Vec3.One));
  }
}