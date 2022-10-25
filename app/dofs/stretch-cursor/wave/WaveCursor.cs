namespace Oriels;

class WaveCursor : dof {

	public class Cursor
	{
		public Vec3 raw;
		public Vec3 pos;
		public Vec3 smooth;
	}

	// data
  public Cursor cursor = new Cursor();
	PullRequest.OneEuroFilter xF = new PullRequest.OneEuroFilter(0.001f, 0.1f);
	PullRequest.OneEuroFilter yF = new PullRequest.OneEuroFilter(0.001f, 0.1f);
	PullRequest.OneEuroFilter zF = new PullRequest.OneEuroFilter(0.001f, 0.1f);

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

			cursor.raw = hand.Get(FingerId.Index, JointId.Tip).position + dir * stretch * strength;
			cursor.pos.x = (float)xF.Filter(cursor.raw.x, (double)Time.Elapsedf);
			cursor.pos.y = (float)yF.Filter(cursor.raw.y, (double)Time.Elapsedf);
			cursor.pos.z = (float)zF.Filter(cursor.raw.z, (double)Time.Elapsedf);
			cursor.smooth = Vec3.Lerp(cursor.smooth, cursor.pos, Time.Elapsedf * 6f);

			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(cursor.raw, Quat.Identity, 0.01f), new Color(1, 0, 0));
			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(cursor.pos, Quat.Identity, 0.01f), new Color(0, 1, 0));
			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(cursor.smooth, Quat.Identity, 0.01f), new Color(0, 0, 1));
    }
  }

  public float deadzone = 0.3f;
  public float strength { 
		get { return PullRequest.ToFloat(ref Mono.inst.wcReach, 0); } // 3f
	}
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


  Vec3[] mm = new Vec3[81];
	
  Vec3[] xL = new Vec3[81];
  Vec3[] xR = new Vec3[81];
	Vec3[] yL = new Vec3[81];
	Vec3[] yR = new Vec3[81];
	Vec3[] zL = new Vec3[81];
	Vec3[] zR = new Vec3[81];
  public void Demo(Quat ori) {
		Trail(mm, cursor.smooth + ori * new Vec3(0, 0, 0.08f));

		// Trail(xL, smoothPos + cursor.orientation * new Vec3(-1, 0, 0) * 0.1f);
		// Trail(xR, smoothPos + cursor.orientation * new Vec3( 1, 0, 0) * 0.1f);
		// Trail(yL, smoothPos + cursor.orientation * new Vec3(0, -1, 0) * 0.1f);
		// Trail(yR, smoothPos + cursor.orientation * new Vec3(0,  1, 0) * 0.1f);
		// Trail(zL, smoothPos + cursor.orientation * new Vec3(0, 0, -1) * 0.1f);
		// Trail(zR, smoothPos + cursor.orientation * new Vec3(0, 0,  1) * 0.1f);
	}

	void Trail(Vec3[] points, Vec3 nextPos) {
		float scale = PullRequest.ToFloat(ref Mono.inst.wcScale, 0.001f);
		while (Vec3.Distance(points[0], nextPos) > 0.03f * scale) {
			for (int i = points.Length - 1; i > 0; i--) {
				points[i] = points[i - 1];
			}
			points[0] += Vec3.Direction(nextPos, points[0]) * 0.02f * scale;
		}


		// points[0] = nextPos;
    int len = (int)(points.Length * PullRequest.ToFloat(ref Mono.inst.wcLength, 0f, 1f));
    for (int i = 0; i < len; i++) {
      // if (i > 0) {
      //   Vec3 dir = Vec3.Forward;
      //   if (points[i].v != points[i - 1].v) {
      //     dir = PullRequest.Direction(points[i], points[i - 1]);
      //   }
      //   // points[i] = points[i - 1] + dir * 0.02f * scale;
      // }

      Vec3 from = i > 0 ? points[i - 1] : nextPos;
      Quat ori = Quat.LookDir(Vec3.Direction(points[i], from));
      Mesh.Cube.Draw(
        Mono.inst.matHolo,
        Matrix.TRS(
					points[i] + ori * new Vec3(0, 0, 0.01f) * scale,
					ori,
					new Vec3(0.01f, 0.01f, 0.02f) * scale
				),
        Color.HSV(i / (float)len, 1, 1)
      );
    }
	}
}


/* 
	COMMENTS

*/