namespace Oriels;

class StretchCursor : dof {
	// input
  public Vec3 vTo, vFrom;

	// data
  public Vec3 cursor;

  public void Init() {}

  public void Frame() {
		float mag 		= (vTo - vFrom).Magnitude;
    float stretch = Math.Max(mag - deadzone, 0f);

    Vec3 dir = PullRequest.Direction(vTo, vFrom);
    cursor 	 = vTo + dir * stretch * strength;

    Mesh.Cube.Draw(Material.Default, Matrix.TS(cursor, 0.01f));
  }

	// design
  public float deadzone = 0.1f;
  public float strength = 3f;
}
