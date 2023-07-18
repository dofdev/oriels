namespace Oriels;

public class ColorCube {
  static Mesh cube = Default.MeshCube;
  static Material mat = new Material(Shader.FromFile("shaders/colorcube.hlsl"));
  static Material unlit = Default.MaterialUnlit;
  public float thicc = 0.0025f;
  public float ogSize = 0.05f;
  public float size = 0.05f;
  public Vec3 center = Vec3.Zero;
  public Vec3 cursor = Vec3.Zero;

  public Color color = Color.White * 0.5f;

  public ColorCube() {
    // SetColor(Vec3.Zero);
  }

  public void Step() {
    mat.SetVector("_pos", center);
    mat.SetFloat("_size", size);
    Vec3 c = Vec3.One / 2;
    float offset = (size / 2) - (thicc / 2);
    for (int i = 0; i < 4; i++) {
      Quat q = Quat.FromAngles(i * 90, 0, 0);
      cube.Draw(mat, Matrix.TS(center + q * new Vec3(0, offset, offset), new Vec3(size, thicc, thicc)));
      for (int j = -1; j <= 1; j += 2) {
        Vec3 scale = q * new Vec3(thicc, size, thicc);
        scale = new Vec3(Math.Abs(scale.x), Math.Abs(scale.y), Math.Abs(scale.z));
        cube.Draw(mat, Matrix.TS(center + q * new Vec3(offset * j, 0, offset), scale));
      }
    }

    float thinn = thicc / 3;
    // Vec3 p0s = pos + new Vec3((float)Math.Sin(Time.Totalf) * offset, (float)Math.Sin(Time.Totalf* 2) * offset, (float)Math.Sin(Time.Totalf * 4) * offset);
    Vec3 raw = center + (cursor * offset);
    Vec3 p00 = cursor;
    p00.x = Math.Clamp(p00.x, -1, 1);
    p00.y = Math.Clamp(p00.y, -1, 1);
    p00.z = Math.Clamp(p00.z, -1, 1);
    Vec3 p0s = center + (p00 * offset);
    cube.Draw(mat, Matrix.TS(new Vec3(center.x, p0s.y, p0s.z), new Vec3(size, thinn, thinn)));
    cube.Draw(mat, Matrix.TS(new Vec3(p0s.x, center.y, p0s.z), new Vec3(thinn, size, thinn)));
    cube.Draw(mat, Matrix.TS(new Vec3(p0s.x, p0s.y, center.z), new Vec3(thinn, thinn, size)));

    Vec3 col = (p00 + Vec3.One) / 2;
    color = new Color(col.x, col.y, col.z);
    cube.Draw(unlit, Matrix.TS(p0s, Vec3.One * thicc * 2), color);
    cube.Draw(unlit, Matrix.TS(raw, Vec3.One * thicc), Color.White);
  }

  public void Palm(Controller con) {
    // reveal when palm up
    float reveal = con.pose.Right.y * 1.666f;
    float look = 1 - Math.Clamp((1 - Math.Clamp(Vec3.Dot((con.pose.position - Input.Head.position).Normalized, Input.Head.Forward), 0f, 1f)) * 5f, 0f, 1f);
    reveal *= look;
    size = ogSize * Math.Clamp(reveal, 0, 1);
    center = con.pose.position + con.pose.Right * 0.0666f;
    // move with grip
    if (reveal > thicc) { // !leftPlanted
      if (reveal > 1f && con.trigger > 0.5f) {
        cursor -= (con.pose.position - oldConPos) / ogSize * 2;
      } else {
        // clamp 0 - 1
        cursor.x = Math.Clamp(cursor.x, -1, 1);
        cursor.y = Math.Clamp(cursor.y, -1, 1);
        cursor.z = Math.Clamp(cursor.z, -1, 1);
      }


      Step();
    }
    oldConPos = con.pose.position;
  }
  Vec3 oldConPos = Vec3.Zero;
}


// everyone get their own color cube? * held in off hand *
// or just one?
// or context sensitive?
// can it be networked effectively? well its just a bounds with a shader and cursor, should be easy enough :)

// set your color and when you move blocks around, it changes the color of the blocks * you leave your mark *