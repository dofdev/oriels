namespace Oriels;

class OrbitalView {
  public static float strength = 0.5f;
  public static float distance = 0.25f;
  public static Matrix transform {
    get {
      Vec3 pos = Input.Head.position + (Input.Head.Forward * distance);

      Vec2 headDirection2D = Input.Head.Forward.XZ.Normalized;
      float angle = -Vec2.AngleBetween(prevDir, headDirection2D);
      Quat rot = prevRot * Quat.Slerp(Quat.Identity, Quat.FromAngles(Vec3.Up * angle), strength);

      prevDir = headDirection2D;
      prevRot = rot;

      return Matrix.TR(pos, rot);
    }
  }

  private static Vec2 prevDir = Vec3.Forward.XZ;
  private static Quat prevRot = Quat.Identity;
}