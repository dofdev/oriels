namespace Oriels;

class StretchCursor : dof {
	// input
  public Pose to, from;

	// data 
  public Pose cursor;

  public void Init() {}

  public void Frame() {
    Vec3   vec      = to.position - from.position;
    float  len      = vec.Length;
    float  stretch  = Math.Max(len - deadzone, 0f);
    Vec3   dir      = backhand ? vec / len : to.orientation * Vec3.Forward;

    cursor.position = to.position + dir * stretch * strength; // * Mono.inst.stretchStr;

    Mesh.Cube.Draw(Material.Default, Matrix.TS(cursor.position, 0.01f));
  }

	// design
  public bool  backhand = true;
  public float deadzone = 0.1f;
  public float strength = 3f;
}
