namespace Oriels;
public class Drawer {
  public Pose pose;
  public float open; // 0 - 1

  public Drawer(Pose pose) {
    this.pose = pose;

    mat.FaceCull = Cull.None;
  }

  public void Frame(Cursor cursor, float pinch) {
    float width = 0.4f;
    float height = 0.15f;

    Matrix matrix = pose.ToMatrix();
    Vec3 localCursor = matrix.Inverse.Transform(cursor.pos);

    bool inBounds = localCursor.x > width  / -2f && localCursor.x < width  / 2f &&
                    localCursor.y > height / -2f && localCursor.y < height / 2f;

    if (!opening) {
      if (open > 0) {
        float delta = localCursor.z - oldZ;

        if (inBounds && localCursor.z < open && delta < -0.5f * Time.Stepf)
          open = 0;
      }

      if (open == 0 && inBounds && localCursor.z > 0 && oldZ <= 0) {
        opening = true;
      }
    }

    if (opening) {
      open = MathF.Max(localCursor.z, 0);

      if (pinch == 0 || open > 0.4f) { // !inBounds || 
        opening = false;
      }
    }

    openSmooth.Update(open);
    model.FindNode("Cube").Mesh.Draw(mat, 
      Matrix.T(V.XYZ(0, 0, 0.5f)) * 
      Matrix.S(V.XYZ(width, height, MathF.Max(openSmooth.value, 0.01f))) * 
      pose.ToMatrix(),
      new Color(0.8f, 0.8f, 0.8f, 0.5f)
    );

    oldZ = localCursor.z;
  }
  float oldZ = 0;
  bool opening = false;

  PR.PID openSmooth = new PR.PID(10f, 0.01f);

  Model model  = Model.FromFile("drawer.glb", Shader.Default);
  Material mat = Material.Default.Copy();
}