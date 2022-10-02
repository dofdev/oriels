namespace Oriels;

class Trackballer : dof {
  Pose anchor = Pose.Identity;
  Quat ori = Quat.Identity;
  Quat qDelta = Quat.Identity;
  Vec2 oldMouse;

  public void Init() {

  }

  public void Frame() {
    // apply the qDelta to the current orientation relative to the head orientation
    // Quat headOri = Input.Head.orientation;
    // ori = (headOri * qDelta * headOri.Inverse * ori).Normalized;
    // PullRequest.Relative(headOri, qDelta) * ori;

    Vec2 mouse = Input.Mouse.pos;
    mouse = new Vec2(
      (mouse.x / 1280 * 2) - 1f,
      (mouse.y / 720 * 2) - 1f
    ) * 4f;

    ori = PullRequest.Delta(
      new Vec3(mouse.x, mouse.y, 1).Normalized,
      new Vec3(oldMouse.x, oldMouse.y, 1).Normalized
    ) * ori;

    oldMouse = mouse;


    Lines.Add(
      anchor.position - ori * new Vec3(-1, 0, 0) * 0.1f,
      anchor.position - ori * new Vec3( 1, 0, 0) * 0.1f,
      new Color(1, 0, 0), 0.002f
    );
    Lines.Add(
      anchor.position - ori * new Vec3( 0,-1, 0) * 0.1f,
      anchor.position - ori * new Vec3( 0, 1, 0) * 0.1f,
      new Color(0, 1, 0), 0.002f
    );
    Lines.Add(
      anchor.position - ori * new Vec3( 0, 0,-1) * 0.1f,
      anchor.position - ori * new Vec3( 0, 0, 1) * 0.1f,
      new Color(0, 0, 1), 0.002f
    );
    Mesh.Cube.Draw(Material.Default, Matrix.TRS(anchor.position, ori, 0.04f));
  }

  Vec2 fromMouse = new Vec2(0, 0);

  public float deadzone = 0.1f;
}
