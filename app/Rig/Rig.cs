using System;
using StereoKit;

public class Rig {
  public Mic mic;

  public Rig() {
    mic = new Mic();
  }

  public Con rCon = new Con(), lCon = new Con();
  public Con Con(bool chirality) { return chirality ? rCon : lCon; }
  bool handleChirality = false;
  public Con HandleCon() {
    return Con(handleChirality);
  }

  public Pose Head() {
    Pose pose = Input.Head;
    pose.position += pose.orientation * Vec3.Forward * -0.1f;
    return pose;
  }

  public Pose rShoulder, lShoulder;
  public Pose Shoulder(bool chirality) { return chirality ? rShoulder : lShoulder; }

  public Pose rWrist, lWrist;
  public Pose Wrist(bool chirality) { return chirality ? rWrist : lWrist; }

  public void Step() {
    // Controllers
    rCon.Step(true);
    lCon.Step(false);

    if (rCon.gripBtn.frameDown) { handleChirality = true; }
    if (lCon.gripBtn.frameDown) { handleChirality = false; }

    // Shoulders
    Vec3 headPos = Input.Head.position + Input.Head.Forward * -0.15f; // Input.Head -> Head() ?
    Vec3 shoulderDir = (
      (lCon.pos.X0Z - headPos.X0Z).Normalized +
      (rCon.pos.X0Z - headPos.X0Z).Normalized
    ).Normalized;

    if (Vec3.Dot(shoulderDir, Input.Head.Forward) < 0) { shoulderDir = -shoulderDir; }
    rShoulder = new Pose(headPos + Quat.LookDir(shoulderDir) * new Vec3(0.2f, -0.2f, 0), Quat.LookDir(shoulderDir));
    lShoulder = new Pose(headPos + Quat.LookDir(shoulderDir) * new Vec3(-0.2f, -0.2f, 0), Quat.LookDir(shoulderDir));

    // Wrists
    rWrist = new Pose(rCon.pos + rCon.ori * new Vec3(0, 0, 0.052f), rCon.ori);
    lWrist = new Pose(lCon.pos + lCon.ori * new Vec3(0, 0, 0.052f), lCon.ori);
  }

  public Vec3 Fullstick(bool chirality) {
    Controller con = Con(chirality).device;
    Quat rot = Quat.FromAngles(con.stick.y * -90, 0, con.stick.x * 90);
    Vec3 dir = Vec3.Up * (con.IsStickClicked ? -1 : 1);
    return con.aim.orientation * rot * dir;
  }
}

public class Con {
  public Controller device;
  public Vec3 pos;
  public Quat ori;
  public Btn gripBtn;
  public Btn triggerBtn;

  public void Step(bool chirality) {
    device = Input.Controller(chirality ? Handed.Right : Handed.Left);
    pos = device.pose.position;
    ori = device.aim.orientation;
    gripBtn.Step(device.grip > 0.5f);
    triggerBtn.Step(device.trigger > 0.5f);
  }

  public Pose Pose() {
    return new Pose(pos, ori);
  }
}

public struct Btn {
  public bool frameDown, held, frameUp;

  public void Step(bool down) {
    frameDown = down && !held;
    frameUp = !down && held;
    held = down;
  }
}