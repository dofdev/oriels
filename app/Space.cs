using System.Runtime.InteropServices;

namespace Oriels;
[StructLayout(LayoutKind.Sequential)]
struct BufferData {
  public Matrix matrix;
  public Vec3 dimensions;
  public float time;
}

public class Space {
  MaterialBuffer<BufferData> buffer;
  BufferData data = new BufferData();

  Material matFloor = new Material(Shader.Default);
  Model shed = Model.FromFile("shed/shed.glb", Shader.FromFile("/shaders/room.hlsl"));
  Model leek = Model.FromFile("houseleek_plant.glb", Shader.FromFile("/shaders/room.hlsl"));
  Mesh cube = Mesh.Cube;

	Solid floor;
  public Space() {
    buffer = new MaterialBuffer<BufferData>(3); // index

		// recenter the nodes in the leek model
		// so that the leek is centered at the origin
		// and the scale is 1
		Vec3 center = new Vec3(0, 0, 0);
		foreach (ModelNode node in leek.Nodes) {
			if (node.Mesh != null) {
				// average the vertices to find the center
				foreach (Vertex vertex in node.Mesh.GetVerts()) {
					center += vertex.pos;
				}
				center /= node.Mesh.VertCount;
			}
			node.LocalTransform = Matrix.TS(
				Vec3.Zero,
				1f
			);
			// node.ModelTransform = Matrix.TS(
			// 	new Vec3(0, 0, 0),
			// 	1f
			// );
		}
		leek.RootNode.LocalTransform = Matrix.TS(
			-center,
			1f
		);


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


	public void Frame() {
		// Oriel oriel = Mono.inst.oriel;
		// data.matrix = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrixInv);
		// data.dimensions = oriel.bounds.dimensions;

		data.matrix = (Matrix)System.Numerics.Matrix4x4.Transpose(Matrix.T(Vec3.Up));
		data.dimensions = new Vec3(0.1f, 0.1f, 0.1f);
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


		shed.Draw(Matrix.Identity);

		leek.Draw(Matrix.TRS(new Vec3(2.5f, 0, -2.5f) * 1.2f, Quat.FromAngles(180f, 30f, 0f), 1.2f));
	}
}