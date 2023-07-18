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
  Model shed = Model.FromFile("shed/shed.glb", Shader.FromFile("shaders/room.hlsl"));
  Mesh cube = Mesh.Cube;

	Solid floor;
  public Space() {
    buffer = new MaterialBuffer<BufferData>(3); // index

		// recenter the nodes in the leek model
		// so that the leek is centered at the origin
		// and the scale is 1
		// Vec3 center = new Vec3(0, 0, 0);
		// foreach (ModelNode node in leek.Nodes) {
		// 	if (node.Mesh != null) {
		// 		// average the vertices to find the center
		// 		foreach (Vertex vertex in node.Mesh.GetVerts()) {
		// 			center += vertex.pos;
		// 		}
		// 		center /= node.Mesh.VertCount;
		// 	}
		// 	node.LocalTransform = Matrix.TS(
		// 		Vec3.Zero,
		// 		1f
		// 	);
		// 	// node.ModelTransform = Matrix.TS(
		// 	// 	new Vec3(0, 0, 0),
		// 	// 	1f
		// 	// );
		// }
		// leek.RootNode.LocalTransform = Matrix.TS(
		// 	-center,
		// 	1f
		// );


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

		meshBeam = Mesh.GeneratePlane(new Vec2(0.1f, 1));
		Vertex[] verts = meshBeam.GetVerts();
		verts[0].col   = new Color(1f, 0.5f, 0.5f);
		meshBeam.SetVerts(verts);
	}
	Mesh meshBeam;

  public float scale;
  public Vec3 floorScale;


	public void Frame() {
		// Oriel oriel = Mono.inst.oriel;
		// data.matrix = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrixInv);
		// data.dimensions = oriel.bounds.dimensions;

		data.matrix = (Matrix)System.Numerics.Matrix4x4.Transpose(Matrix.T(Vec3.Up));
		data.dimensions = new Vec3(0.1f, 0.1f, 0.1f);
		buffer.Set(data);





		// PR.BlockOut(floor.GetPose().ToMatrix(floorScale), Color.White * 0.333f, matFloor);
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

		// leek.Draw(Matrix.TRS(new Vec3(2.5f, 0, -2.5f) * 1.2f, Quat.FromAngles(180f, 30f, 0f), 1.2f));

		
		float radius = 9;
		Vec3 pillarsOffset = new Vec3(0, 0, -10);
		for (float x = -radius; x < radius; x++) {
			for (float z = -radius; z < radius; z++) {

				float d2 = Mono.inst.noise.D2((int)x, (int)z);
				float xpos = pillarsOffset.x + x;
				float zpos = pillarsOffset.z - z;
				
				if (Vec3.Distance(new Vec3(xpos, 0, zpos), Vec3.Zero) < radius / 2) {
					Mesh.Cube.Draw(
						Mono.inst.matHolo,
						Matrix.TS(
							new Vec3(
								xpos + x, 
								radius / 2,
								zpos + z / 2
							) + Quat.FromAngles(d2 * 90, d2 * 360, 0) * Vec3.Forward * 2f,
							new Vec3(2, 1, 2) * (0.5f + d2)
						),
						Color.White
					);
					continue;
				}

				float height = 1 + d2;
				float angle = d2 * 360f;
				Vec3 offset = Quat.FromAngles(0, angle, 0) * Vec3.Forward * d2 * 0.5f;
				xpos += offset.x;
				zpos += offset.z;
				Mesh.Cube.Draw(
					Mono.inst.matHolo,
					Matrix.TRS(
						new Vec3(xpos, (height * 0.5f), zpos),
						Quat.FromAngles(0, angle, 0),
						new Vec3(0.1f, height, 0.1f)
					),
					new Color(1f, 1f, 1f, 1f)
				);

				Mesh.Cube.Draw(
					Mono.inst.matHolo,
					Matrix.TRS(
						new Vec3(xpos, height, zpos),
						Quat.FromAngles(0, angle, 0),
						new Vec3(0.95f, height, 0.95f)
					),
					new Color(0.3f, 0.7f + (d2 * 0.3f), 0.2f, 1f)
				);
			}
		}

		for (int i = 0; i < drops.Length; i++) {
			if (drops[i] == null) drops[i] = new Drop();
			
			drops[i].Frame(i);
		}
	}
	Drop[] drops = new Drop[128];
	class Drop {
		public Vec3 pos;
		float rippleT = 1f;
		bool falling = false;
		Mesh mesh_ripple = Model.FromFile("ripple.glb").FindNode("ripple").Mesh;

		public void Frame(int id) {
			if (!falling) {
				PR.Noise noise = Mono.inst.noise;		
				rippleT += Time.Stepf / 0.5f;
				if (rippleT >= 1.0f + (1.0f + noise.D1(id))) {
					pos = new Vec3(
						noise.value  *  10f,
						10, 
						-0.5f + noise.uvalue * -10f
					);
					falling = true;
				}

				float t = 1 - MathF.Min(rippleT, 1f);
				t *= t;
				t = 1 - t;
				mesh_ripple.Draw(
					Mono.inst.matHoloclear,
					Matrix.TRS(pos, Quat.Identity, new Vec3(0.333f * t, 0.0133f, 0.333f * t)),
					new Color(1f, 1f, 1f, 1f) * (1f - t)
				); 
			}

			if (falling) {
				// rain's terminal velocity is 9.8 m/s
				pos.y -= 9.8f * Time.Stepf;
				if (pos.y <= 0.0f) {
					pos.y = 0.0f;
					rippleT = 0f;
					falling = false;
				}

				Mesh.Cube.Draw(
					Mono.inst.matHoloclear,
					Matrix.TRS(pos, Quat.Identity, new Vec3(0.002f, 0.98f, 0.002f)),
					new Color(0.8f, 0.8f, 1f) * 0.1333f
				); 
			}
		}
	}


	// Tree tree = new Tree();
	// Tree[] trees = new Tree[128];

	// class Tree {
	// 	float r; // damage
	// 	float g; // resources
	// 	float b; // peak
	// 	Vec3  pos;
	// 	float angle;
	// 	// color(r, max(g, b), b)
	// 	// height = b

	// 	public void Frame() {

	// 	}
	// }
}


/* 
	COMMENTS

	sapling
		e 

		e = lerp(e, g * lft, lft / x)
		r += e
		g -= e
		b -= min(g, 0)
		reliant on ideal conditions being there to ease off of the seed dependency
			(scrappy can outlast the g)

	tree 
		r += min(g, 0) + background radiation * lft
		g += b * lft
		b += 
		r 0->b
		g = clamp(g, 0, 1)
		b = clamp(b, 0, 1)

		e = (r / neighbors) * lft
		g -= e

		if g > 1 - r 
			seed(g, pos + Quat.FromAngles(0, noise.value * 360f, 0) * b)
			g = 0

		tilt towards best spot
			* rand.dir * r * smoothstart (r * r * r * r * r) ?
			
		if r > b
			b += lft

		if b > 1
			poof

*/