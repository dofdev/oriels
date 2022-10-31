namespace Oriels;

class WaveCursor : dof {
	public bool Active { get; set; }

	// input
	public Handed handed = Handed.Left;

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

			// Biased by finger length
			float stretch = (fI + fI + fM + fM + fM + fR + fR + fL) / 8f; 

			Vec3 dir = PullRequest.Direction(
				hand.Get(FingerId.Index, JointId.Tip).position,
				hand.Get(FingerId.Index, JointId.KnuckleMajor).position
			);

			cursor.raw = hand.Get(FingerId.Index, JointId.Tip).position + dir * stretch * reach.value;
			cursor.pos.x = (float)xF.Filter(cursor.raw.x, (double)Time.Elapsedf);
			cursor.pos.y = (float)yF.Filter(cursor.raw.y, (double)Time.Elapsedf);
			cursor.pos.z = (float)zF.Filter(cursor.raw.z, (double)Time.Elapsedf);
			cursor.smooth = Vec3.Lerp(cursor.smooth, cursor.pos, Time.Elapsedf * 6f);

			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(cursor.raw, Quat.Identity, 0.01f), new Color(1, 0, 0));
			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(cursor.pos, Quat.Identity, 0.01f), new Color(0, 1, 0));
			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(cursor.smooth, Quat.Identity, 0.01f), new Color(0, 0, 1));


			// pinch is more than just the thumb and index finger

			handBtn.Frame(
				PinchStep(hand, FingerId.Little, littleBtn) ||
				PinchStep(hand, FingerId.Ring, ringBtn) ||
				PinchStep(hand, FingerId.Middle, middleBtn) ||
				PinchStep(hand, FingerId.Index, indexBtn)
			);
			if (handBtn.held) {
				shapePos = cursor.pos;
			}

			// Mesh.Cube.Draw(
			// 	Mono.inst.matHolo,
			// 	Matrix.TS(shapePos, 10 * U.cm),
			// 	new Color(0.5f, 0.55f, 0.75f)
			// );
    }
  }
	Btn littleBtn = new Btn();
	Btn ringBtn = new Btn();
	Btn middleBtn = new Btn();
	Btn indexBtn = new Btn();
	Btn handBtn = new Btn();
	Vec3 shapePos = new Vec3(0, 1.3f, -0.5f);


	// design
	public Design deadzone = new Design { str="0.3", term="0+1t", min=0, max=1 };
	public Design reach = new Design { str="1.0", term="0+m", min=0 };

	// demo
	public Design snakeLength = new Design { str="0.5", term="0+1t", min=0, max=1 };
	public Design snakeScale  = new Design { str="0.333", term=">0", min=0.01f };
	public Design snakeRadius = new Design { str="4", term="0+cm", unit=U.cm, min=0 };


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

	bool PinchStep(Hand hand, FingerId finger, Btn btn) {
		HandJoint thumb = hand.Get(FingerId.Thumb, JointId.Tip);
		HandJoint fingy = hand.Get(finger, JointId.Tip);
		float dist = Vec3.Distance(thumb.position, fingy.position);
		btn.Frame(dist < 1 * U.cm, dist < 2 * U.cm);

		return btn.held;
	}


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