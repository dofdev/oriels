namespace Oriels;

class StretchCursor : dof {
	// input
  public Pose to, from;

	// data 
  public Vec3 cursor;

  public void Init() {}

  public void Frame() {
    Vec3  vec = to.position - from.position;
    float len = vec.Length;
    Vec3 	dir = vec / len;

    float stretch = Math.Max(len - deadzone, 0f);
    cursor = to.position + dir * stretch * strength;

    Mesh.Cube.Draw(Material.Default, Matrix.TS(cursor, 0.01f));
  }

	// design
  public float deadzone = 0.1f;
  public float strength = 3f;
}
