using System;
using StereoKit;

class ColorCube {
	static Material orbMat = Default.MaterialUnlit.Copy();
	static Model orb = new Model(Default.MeshSphere, orbMat);
	static Model colorCube = Model.FromFile("colorcube.glb", Shader.UIBox);
	Bounds bounds = new Bounds(Vec3.Zero, Vec3.One * 1.25f);
	
	public bool picker = true;
	public Color color = Color.White * 0.5f;
	public float thickness {
		set {
			_thickness = value;
			colorCube.RootNode.Material["border_size"] = value;
		}
	}
	float _thickness = 0.01f;

	public ColorCube() {
		SetColor(Vec3.Zero);
	}

	void SetColor(Vec3 axes) {
		Color col = Vec2Col(axes);
		orbMat["color"] = col;
		color = col;
	}
	Color Vec2Col(Vec3 vec) {
		Vec3 normalVec = vec;
		normalVec += Vec3.One * 0.5f;
		return new Color(normalVec.x, normalVec.y, normalVec.z);
	}
	Vec3 Col2Vec(Color color) {
		Vec3 colVec = new Vec3(color.r, color.g, color.b);
		colVec -= (Vec3.One * 0.5f);
		return colVec;
	}

	public void Step(Matrix matrix) {
		colorCube.Draw(matrix);

		if(!picker)
			return;

		for (int h = 0; h < (int)Handed.Max; h++) {
			// Get the pose for the index fingertip
			Hand hand      = Input.Hand((Handed)h);
			Pose fingertip = hand[FingerId.Index, JointId.Tip].Pose;

			Vec3 localFingerPos = matrix.Inverse * fingertip.position;
			if(hand.IsPinched && bounds.Contains(localFingerPos)) {
				localFingerPos.x = Math.Clamp(localFingerPos.x, -0.5f, 0.5f);
				localFingerPos.y = Math.Clamp(localFingerPos.y, -0.5f, 0.5f);
				localFingerPos.z = Math.Clamp(localFingerPos.z, -0.5f, 0.5f);
				SetColor(localFingerPos);
			}
		}

		Vec3 orbPos = Col2Vec(color);

		Lines.Add(matrix * new Vec3(-0.5f, orbPos.y, orbPos.z), matrix * new Vec3(0.5f, orbPos.y, orbPos.z), new Color(0, color.g, color.b), new Color(1, color.g, color.b), _thickness);
		Lines.Add(matrix * new Vec3(orbPos.x, -0.5f, orbPos.z), matrix * new Vec3(orbPos.x, 0.5f, orbPos.z), new Color(color.r, 0, color.b), new Color(color.r, 1, color.b), _thickness);
		Lines.Add(matrix * new Vec3(orbPos.x, orbPos.y, -0.5f), matrix * new Vec3(orbPos.x, orbPos.y, 0.5f), new Color(color.r, color.g, 0), new Color(color.r, color.g, 1), _thickness);

		orb.Draw(Matrix.TS(matrix * orbPos, _thickness * 2));
	}
}
