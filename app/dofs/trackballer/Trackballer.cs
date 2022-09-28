namespace Oriels;

class Trackballer : dof {
  public Pose p0, anchor = Pose.Identity;
  public Trackballer(Pose p0, Pose anchor) {
    this.p0 = p0;
    this.anchor = anchor;
  }


  Vec3 pos = new Vec3(0, 0, -1);
  Vec3 vel = new Vec3(0, 0, 0);


  public Quat ori = Quat.Identity;
  public void Init() {

  }

  // Vec3 pos, oldPos;
  Quat qDelta = Quat.Identity;
  public void Frame() {
    p0 = Mono.inst.rGlove.virtualGlove;

    // Vec3 vA = PullRequest.Direction(oldPos,      anchor.position);
    // Vec3 vB = PullRequest.Direction(p0.position, anchor.position);

    // if (Vec3.Distance(p0.position, oldPos) > 0.2f) { // (p0.position.v != oldPos.v) {
    //   // Vec3 delta = (p0.position - oldPos);
    //   float angle = Vec3.AngleBetween(vA, vB);
    //   Console.WriteLine("angle: " + angle);
    //   if (angle > 1f) {
    //     Vec3 v = PullRequest.Slerp(vA, vB, 1 / angle).Normalized;
    //     // Vec3 delta -> Quat delta
    //     Quat a = Quat.LookDir(vA).Normalized;
    //     Quat b = Quat.LookDir(v).Normalized;
    //     // when converting from vec to quat, the up axis can get flipped causing issues with reliably scaling the qDelta by vector angle


    //     qDelta = Quat.Difference(a, b).Normalized;

    //     // qDelta scaled to one degree
    //     // qDelta = Quat.Slerp(Quat.Identity, qDelta, 1 / angle).Normalized;

    //     // ori = Quat.Slerp(Quat.Identity, Quat.FromAngles(1, 0, 0), 6 * Time.Elapsedf) * ori;

    //     // and use the velocity magnitude
    //     float test = MathF.Tau * 0.05f;
    //   }

    //   oldPos = p0.position;
    // }
    // ori = Quat.Slerp(Quat.Identity, qDelta, 60 * Time.Elapsedf).Normalized * ori;
    // ori.Normalize();





    
    // Vec3 newPos = pos + vel * Time.Elapsedf;
    // if (newPos.v != pos) {
    //   newPos = PullRequest.Direction(newPos, anchor.position).Normalized * 1f;
    //   vel = PullRequest.Direction(newPos, pos) * vel.Length;
    //   pos = newPos;
    // }

    if (Input.Key(Key.Space).IsJustActive()) {
      fromMouse = Input.Mouse.pos;
    }

    if (Input.Key(Key.Space).IsJustInactive()) {
      Vec2 delta = (Input.Mouse.pos - fromMouse) / 32f;
      vel = new Vec3(delta.x, -delta.y, 0);

      pos = new Vec3(0, 0, 1);
      // ori = (Quat.LookDir(pos).Normalized * ori).Normalized;
    }

    // ori.LookDirection(pos);

    // ori = Quat.FromAngles(0, MathF.Sin(Time.Totalf) * 180, 0);
    // float angle; Vec3 axis;
    // ori.ToAxisAngle(out axis, out angle);
    // ori = PullRequest.FromAxisAngle(axis, angle);





    // pivot to just Quat.FromAngles(x, 0, 0) * Quat.FromAngles(0, y, 0) for the delta
    Quat qDelta = (
      Quat.FromAngles(-vel.y * 60 * Time.Elapsedf, 0, 0) 
      * Quat.FromAngles(0, vel.x * 60 * Time.Elapsedf, 0) 
      // * Quat.FromAngles(0, 0, -vel.z * 60 * Time.Elapsedf)
    );
    // apply the qDelta to the current orientation relative to the head orientation
    Quat headOri = Input.Head.orientation;
    ori = (headOri * qDelta * headOri.Inverse * ori).Normalized;
    // ori = qDelta * ori;

    
    


    // Lines.Add(anchor.position, pos, new Color(1, 0, 0), 0.01f);

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
    // Mesh.Cube.Draw(Material.Default, Matrix.TS(p0.position, new Vec3(0.04f, 0.01f, 0.04f)));
  }

  Vec2 fromMouse = new Vec2(0, 0);

  public float deadzone = 0.1f;
}
