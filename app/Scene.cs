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
  Monolith mono;
  
  MaterialBuffer<BufferData> buffer;
  BufferData data = new BufferData();

  Material matFloor = new Material(Shader.Default);
  Model room = Model.FromFile("room/room.glb", Shader.FromFile("room.hlsl"));


  Solid floor;
  public Scene(Monolith mono) {
    this.mono = mono;

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
    data.matrix = (Matrix)System.Numerics.Matrix4x4.Transpose(mono.oriel.matrix);
    data.dimensions = mono.oriel.bounds.dimensions;
    
    buffer.Set(data);

    // PullRequest.BlockOut(floor.GetPose().ToMatrix(floorScale), Color.White * 0.333f, matFloor);
    foreach (ModelNode node in room.Visuals) {

      // Console.WriteLine(i + " - " + node.Name);

      // node.Material.SetVector("_center", mono.oriel.bounds.center);
      // node.Material.SetVector("_dimensions", mono.oriel.bounds.dimensions);
      // node.Material["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(mono.oriel.matrix);

      // Console.WriteLine("Shader: " + node.Material.Shader.Name);

      // node.Mesh.Draw(matRoom, Matrix.TRS(new Vec3(0, World.BoundsPose.position.y, -1), Quat.Identity, Vec3.One));
      // Console.WriteLine(matRoom.ParamCount + " test " + node.Material.ParamCount);
    }
    // room.RootNode.Material.SetVector("_center", mono.oriel.bounds.center);
    // room.RootNode.Material.SetVector("_dimensions", mono.oriel.bounds.dimensions);
    // room.RootNode.Material["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(mono.oriel.matrix);

    // Shader.

    room.Draw(Matrix.TRS(new Vec3(0, World.BoundsPose.position.y, -1), Quat.Identity, Vec3.One));
  }
}