namespace Oriels;
public class Fullstick {
  public Vec3 Direction(bool chirality) {
    Controller con = Mono.inst.rig.Con(chirality).device;
    Quat rot = Quat.FromAngles(con.stick.y * -90, 0, con.stick.x * 90);
    Vec3 dir = Vec3.Up * (con.IsStickClicked ? -1 : 1);
    return con.aim.orientation * rot * dir;
  }
}