namespace Oriels;

class WaveCursor : dof {
	public bool Active { get; set; }

	// input
	public Handed handed = Handed.Left;

	// data
  public Cursor cursor = new Cursor();

	public void Init() {}

  public void Frame() {
    Hand hand = Input.Hand(handed);
    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			float fI = Flexion(hand, FingerId.Index);
			float fM = Flexion(hand, FingerId.Middle);
			float fR = Flexion(hand, FingerId.Ring);
			float fL = Flexion(hand, FingerId.Little);

			// Biased by finger length
			float stretch = (fI + fI + fM + fM + fM + fR + fR + fL) / 8f; 

			Vec3 dir = PullRequest.Direction(
				hand.Get(FingerId.Index, JointId.Tip).position,
				hand.Get(FingerId.Index, JointId.KnuckleMajor).position
			);

			cursor.raw = hand.Get(FingerId.Index, JointId.Tip).position + dir * stretch * reach.value;

			Mesh.Sphere.Draw(Mono.inst.matHoloframe, Matrix.TRS(cursor.raw, Quat.Identity, 0.01f), new Color(1, 0, 0));
			Mesh.Sphere.Draw(Mono.inst.matHoloframe, Matrix.TRS(cursor.pos, Quat.Identity, 0.01f), new Color(0, 1, 0));
			Mesh.Sphere.Draw(Mono.inst.matHoloframe, Matrix.TRS(cursor.smooth, Quat.Identity, 0.01f), new Color(0, 0, 1));
    }
  }

	// design
	public Design deadzone = new Design { str="0.3", term="0+1t", min=0, max=1 };
	public Design reach = new Design { str="1.0", term="0+m", min=0 };

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

    return Math.Max(flexion - deadzone.value, 0f) / (1 - deadzone.value);
  }

	// demo
	public Design snakeLength = new Design { str="0.5", term="0+1t", min=0, max=1 };
	public Design snakeScale  = new Design { str="0.333", term=">0", min=0.01f };
	public Design snakeRadius = new Design { str="4", term="0+cm", unit=U.cm, min=0 };

  Vec3[] mm = new Vec3[128];
  public void Demo(Quat ori) {
		Vec3 tPos = cursor.smooth + ori * Vec3.Forward * snakeRadius.value;
		Lines.Add(cursor.smooth, tPos, Color.White, 0.001f);
		Trail(mm, tPos);
	}

	void Trail(Vec3[] points, Vec3 nextPos) {
		float scale = snakeScale.value;
		while (Vec3.Distance(points[0], nextPos) > 0.03f * scale) {
			for (int i = points.Length - 1; i > 0; i--) {
				points[i] = points[i - 1];
			}
			points[0] += Vec3.Direction(nextPos, points[0]) * 0.02f * scale;
		}


		// points[0] = nextPos;
    int len = (int)(points.Length * snakeLength.value);
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
        Mono.inst.matHoloframe,
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