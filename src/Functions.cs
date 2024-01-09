namespace Oriels;
public static class Functions {
  
  // in SK >= 1.2.0 as Vec3.Direction()
  public static Vec3 dir(Vec3 to, Vec3 from) {
    return (to - from).Normalized;
  }

  // deadzone
  // magnitude
  // extension

  public static Vec3 reflect(Vec3 v, Vec3 n) {
    return v - 2 * Vec3.Dot(v, n) * n;
  }
  

  /* under construction
    claspΔ(aΔ, bΔ, t = 0.5) =>
      Δ = lerp(aΔ, bΔ, t) * sign(aΔ) == sign(bΔ) ? 1 : 0


    with your two index fingers on a larger object to really test this
  
  */
  // public static float claspΔ(float aΔ, float bΔ) {

  // }
}