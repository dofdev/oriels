using Oriels;

namespace Greenyard;
public class Mono {
  public Oriel oriel = new Oriel(
    new Vec3(0.0f, -0.5f, 0.5f),
    Quat.FromAngles(0, 90, 0),
    new Vec3(0.8f, 0.5f, 0.5f)
  ) { color = new Color(78 / 256f, 142 / 256f, 191 / 256f) * 0.333f };

	Model greenyardModel = Model.FromFile("greenyard.glb");
	Mesh[] greenyard;
	Material greenyardMat = new Material(Shader.FromFile("shaders/oriel.hlsl"));

	Matrix matrix = Matrix.Identity;
	Vec3 offset = new Vec3(2, 1, -12);
	public float height = 6f;
	Vec3 angle = new Vec3(0, 180, 0);

	public void Init() {
		greenyard = new Mesh[12];
		for (int i = 0; i < greenyard.Length; i++) {
			greenyard[i] = greenyardModel.GetMesh("Object_" + (i + 2));
		}
		greenyardMat.SetMat(101, Cull.None, true);
    // greenyardMat.Transparency = Transparency.Add;
    // greenyardMat.DepthTest    = DepthTest.Always;
		greenyardMat.SetTexture("diffuse", Tex.FromFile("greenyard.jpeg"));
	}

	public void Frame() {
		Oriels.Rig rig = Oriels.Mono.inst.rig;

		// angle.x -= rig.rCon.device.stick.y * 90f * Time.Stepf;
		// angle.x = PR.Clamp(angle.x, -89, 89);
		angle.y -= rig.rCon.device.stick.x * 90f * Time.Stepf;

		Vec3 input = new Vec3(
			rig.lCon.device.stick.x,
			0,
			rig.lCon.device.stick.y
		);
		if (input.MagnitudeSq > 0.01f) {
			input = (
				// Quat.FromAngles(angle.x, 0, 0).Inverse *
				Quat.FromAngles(0, angle.y, 0).Inverse *
				rig.lCon.ori *
				oriel.ori.Inverse
			).Normalized * input;
			
			input.y = 0;
			offset += input * Time.Stepf;
		}
		offset.y = -height;



    // Oriel
    float scale = 0.1f; // oriel.scale * oriel.multiplier;
		// scale w/height?
		// scale *= oriel.bounds.dimensions.y; 

		matrix = Matrix.TRS(
			Vec3.Zero, // -oriel.bounds.dimensions.y / 2.01f
			Quat.FromAngles(angle.x, 0, 0) *
			Quat.FromAngles(0, angle.y, 0),
			Vec3.One * scale
		);
	}

	public void Render() {
		greenyardMat.SetVector("_center", oriel.bounds.center);
		greenyardMat.SetVector("_dimensions", oriel.bounds.dimensions);
		greenyardMat.SetVector("_light", oriel.ori * new Vec3(0.6f, -0.9f, 0.3f));
		greenyardMat.SetFloat("_lit", 0);
		greenyardMat["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrixInv);
		for (int i = 0; i < greenyard.Length; i++) {
			greenyard[i].Draw(greenyardMat,
				Matrix.TRS(
					offset,
					Quat.Identity,
					new Vec3(1f, 1f, 1f)
				) * matrix * oriel.matrix,
				Color.White
			);
		}
	}
}