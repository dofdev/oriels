namespace Oriels;
public static class Functions {
  
  // in SK >= 1.2.0 as Vec3.Direction()
  public static Vec3 Dir(Vec3 to, Vec3 from) {
    return (to - from).Normalized;
  }

  // deadzone
  // magnitude
  // extension
}