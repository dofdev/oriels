namespace Oriels;

class StretchCursor : dof {
	// input
  public Pose to, from;

	// data 
  public Vec3 cursor;

  public void Init() {
		cursor = to.position;
	}

  public void Frame() {
    Vec3   vec      = to.position - from.position;
    float  len      = vec.Length;
    float  stretch  = Math.Max(len - deadzone, 0f);
    Vec3   dir      = pointer ? vec / len : to.orientation * Vec3.Forward;

    cursor = to.position + dir * stretch * strength;

    Mesh.Cube.Draw(Material.Default, Matrix.TS(cursor, 0.01f));
  }

	// design
  public bool  pointer  = false;
  public float deadzone = 0.1f;
  public float strength = 3f;
}
