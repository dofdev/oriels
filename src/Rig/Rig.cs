namespace Oriels;

public class Rig {
  public Mic mic = new Mic();
  public Vec3 pos = new Vec3(0, 0, 0);
  public Quat ori = Quat.Identity;

  public Rig() {}

  // public Vec3 center;
  // public void Recenter() {
  //   center = World.BoundsPose.position.X0Z - Head.position.X0Z;
  // }

  public Vec3 FloorCenter {
    get {
      if (World.HasBounds) { return World.BoundsPose.position; }
      return new Vec3(0, 0, 0);
    }
  }

  public Pose Head {
    get {
      Pose pose = Input.Head; // between eyes pose
      pose.position += pose.orientation * Vec3.Forward * -0.1f;
      return pose;
    }
  }

  public Pose rShoulder, lShoulder;
  public Pose Shoulder(bool chirality) { return chirality ? rShoulder : lShoulder; }

  public Pose rWrist, lWrist;
  public Pose Wrist(bool chirality) { return chirality ? rWrist : lWrist; }

  public Con rCon = new Con(), lCon = new Con();
  public Con Con(bool chirality) { return chirality ? rCon : lCon; }


  public Vec3 LocalPos(Vec3 p) {
    return ori.Inverse * (p - (pos));
  }

	bool gotBounds = false;
	Matrix bounds = Matrix.T(0, -1.3f, 0);
	public void Step() {
		if (!gotBounds && World.HasBounds) {
			gotBounds = true;
			bounds = World.BoundsPose.ToMatrix();
		}
		Vec2 stickL = Input.Controller(Handed.Left).stick;
		Vec2 stickR = Input.Controller(Handed.Right).stick;
		Quat delta = Quat.FromAngles(
			Vec3.Up * stickR.x * 120f * Time.Stepf
		) * ori;
    Vec3 headPos = Input.Head.position + Input.Head.Forward * -0.15f; // Input.Head -> Head() ?
		pos -= headPos;
		pos = delta * pos;
		pos += headPos;
		ori = delta * ori;

		Vec3 move = -stickL.X0Y * Time.Stepf * 0.5f;
		pos += (Input.Head.orientation * move).X0Z;

		Renderer.CameraRoot = Matrix.TR(pos, ori) * bounds.Inverse;

    // Controllers
    rCon.Step(true);
    lCon.Step(false);

    // Shoulders
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

	public float Flexion(Hand hand, FingerId id) {
		float fingerFlex = (Vec3.Dot(
			Vec3.Direction(
				hand.Get(id, JointId.Tip).position,
				hand.Get(id, JointId.KnuckleMinor).position
			),
			Vec3.Direction(
				hand.Get(id, JointId.KnuckleMid).position,
				hand.Get(id, JointId.KnuckleMajor).position
			)
		) + 1f) / 2;

    float fingerTrim = 0.15f;
		fingerFlex = Math.Max(fingerFlex - fingerTrim, 0f) / (1 - fingerTrim);

    float knuckleFlex = (Vec3.Dot(
      Vec3.Direction(
        hand.Get(id, JointId.KnuckleMid).position,
        hand.Get(id, JointId.KnuckleMajor).position
      ),
      Vec3.Direction(
        hand.Get(id, JointId.KnuckleMajor).position,
        hand.Get(id, JointId.Root).position
      )
    ) + 1f) / 2;

    float knuckleTrim = 0.666f;
    knuckleFlex = Math.Max(knuckleFlex - knuckleTrim, 0f) / (1 - knuckleTrim);

    float flexion = knuckleFlex + fingerFlex;
		return flexion * flexion;
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
  public Quat ori = Quat.Identity;
  public Pose pose;
  public Vec3 backhandDir;
  public Btn gripBtn;
  public Btn triggerBtn;

  public void Step(bool chirality) {
    device = Input.Controller(chirality ? Handed.Right : Handed.Left);
    pose.position = pos = device.pose.position;
    pose.orientation = ori = Quat.Identity; // device.pose.orientation;
    backhandDir = ori * (chirality ? Vec3.Right : -Vec3.Right);
    gripBtn.Frame(device.grip > 0.5f);
    triggerBtn.Frame(device.trigger > 0.5f);
  }
}

public struct Btn {
  public bool frameDown, held, frameUp;

  public void Frame(bool down, bool? sticky = null) {
		if (sticky != null && held) {
			down = (bool)sticky;
		}

    frameDown = down && !held;
    frameUp = !down && held;
    held = down;
  }
}