namespace Oriels;

class WaveCursor : dof {

	// data
  public Pose cursor = Pose.Identity;
  
  public void Init() {}

  public void Frame() {
    Hand hand = Input.Hand(handed);
    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			float fI = Flexion(hand, FingerId.Index);
			float fM = Flexion(hand, FingerId.Middle);
			float fR = Flexion(hand, FingerId.Ring);
			float fL = Flexion(hand, FingerId.Little);

			float stretch = (fI + fI + fM + fM + fM + fR + fR + fL) / 8f; // based on finger length

			Vec3 dir = PullRequest.Direction(
				hand.Get(FingerId.Index, JointId.Tip).position,
				hand.Get(FingerId.Index, JointId.KnuckleMajor).position
			);

			Vec3 rawPos = hand.Get(FingerId.Index, JointId.Tip).position + dir * stretch * strength * Mono.inst.stretchStr;
			cursor.position = Vec3.Lerp(cursor.position, rawPos, Time.Elapsedf * 6f);
			cursor.orientation = hand.palm.orientation;

    }

    // Demo();
  }

  public float deadzone = 0.03f;
  public float strength = 3f;
  public Handed handed  = Handed.Left;


  float Flexion(Hand hand, FingerId finger) {
    float flexion = (Vec3.Dot(
      PullRequest.Direction(
        hand.Get(finger, JointId.Tip).position,
        hand.Get(finger, JointId.KnuckleMinor).position
      ),
      PullRequest.Direction(
        hand.Get(finger, JointId.KnuckleMid).position,
        hand.Get(finger, JointId.KnuckleMajor).position
      )
    ) + 1f) / 2;

    return Math.Max(flexion - deadzone, 0f) / (1 - deadzone);
  }


  Vec3[] mm = new Vec3[64];
	
  Vec3[] xL = new Vec3[64];
  Vec3[] xR = new Vec3[64];
	Vec3[] yL = new Vec3[64];
	Vec3[] yR = new Vec3[64];
	Vec3[] zL = new Vec3[64];
	Vec3[] zR = new Vec3[64];
  public void Demo(Quat ori) {
		Trail(mm, cursor.position + ori * new Vec3(0, 0, 0.04f));

		// Trail(xL, smoothPos + cursor.orientation * new Vec3(-1, 0, 0) * 0.1f);
		// Trail(xR, smoothPos + cursor.orientation * new Vec3( 1, 0, 0) * 0.1f);
		// Trail(yL, smoothPos + cursor.orientation * new Vec3(0, -1, 0) * 0.1f);
		// Trail(yR, smoothPos + cursor.orientation * new Vec3(0,  1, 0) * 0.1f);
		// Trail(zL, smoothPos + cursor.orientation * new Vec3(0, 0, -1) * 0.1f);
		// Trail(zR, smoothPos + cursor.orientation * new Vec3(0, 0,  1) * 0.1f);
  }

	void Trail(Vec3[] points, Vec3 nextPos) {
		points[0] = nextPos;
    int len = (int)(points.Length * Mono.inst.trailLen);
    for (int i = 0; i < len; i++) {
      if (i > 0) {
        Vec3 dir = Vec3.Forward;
        if (points[i].v != points[i - 1].v) {
          dir = PullRequest.Direction(points[i], points[i - 1]);
        }
        points[i] = points[i - 1] + dir * 0.02f * Mono.inst.trailScl;
      }

      Vec3 from = i > 0 ? points[i - 1] : nextPos;
      Quat ori = Quat.LookDir(PullRequest.Direction(points[i], from));
      Mesh.Cube.Draw(
        Mono.inst.matHolo,
        Matrix.TRS(
					points[i] + ori * new Vec3(0, 0, 0.01f) * Mono.inst.trailScl,
					ori,
					new Vec3(0.01f, 0.01f, 0.02f) * Mono.inst.trailScl
				),
        Color.HSV(i / (float)len, 1, 1)
      );
    }
	}
}