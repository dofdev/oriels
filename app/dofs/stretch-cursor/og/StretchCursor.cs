namespace Oriels;

class StretchCursor : dof {
	// input
	Vec3 vTo;
	Vec3 vFrom;

	// data
	float stretch;
	Vec3 cursor;

  public void Init() {}

	public void Frame() {
		float mag = (vTo - vFrom).Magnitude;
		stretch = MathF.Max(mag - deadzone, 0);

		Vec3 dir = PullRequest.Direction(vTo, vFrom);
		cursor = vTo + dir * stretch * strength;

		// draw
		Lines.Add(vFrom, vTo, new Color(1, 1, 1), 0.002f);
		Lines.Add(vTo, cursor, new Color(1, 0, 0), 0.002f);
		Mesh.Cube.Draw(Material.Default, Matrix.TRS(cursor, Quat.Identity, 0.04f));
	}

	// design
	float deadzone = 0.1f;
	float strength = 3f;
}
