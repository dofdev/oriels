namespace Oriels;

class WaveCursor : dof {
  public WaveCursor() {

  }

  Vec3 oldLocalPad;
  Quat delta;
  public Pose cursor = Pose.Identity;
  Material mat;
  public void Init() {
		mat = Material.Default.Copy();
		mat.SetTexture("diffuse", Tex.DevTex);
  }

  bool isTracking = false;
  public void Frame() {
    Hand hand = Input.Hand(handId);

    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
      // p0.position = hand.Get(FingerId.Index, JointId.KnuckleMajor).position;
      // p1.position = hand.Get(FingerId.Index, JointId.Tip).position;

      isTracking = true;
    }

    float fI = Flexion(hand, FingerId.Index);
    float fM = Flexion(hand, FingerId.Middle);
    float fR = Flexion(hand, FingerId.Ring);
    float fL = Flexion(hand, FingerId.Little);

    float stretch = (fI + fI + fM + fM + fM + fR + fR + fL) / 8f; // based on finger length

    Vec3 dir = PullRequest.Direction(
      hand.Get(FingerId.Index, JointId.Tip).position,
      hand.Get(FingerId.Index, JointId.KnuckleMajor).position
    );

    cursor.position = hand.Get(FingerId.Index, JointId.Tip).position + dir * stretch * strength * Mono.inst.stretchStr;


    // thumb trackballer
    float d = Vec3.Distance(
      hand.Get(FingerId.Index, JointId.KnuckleMid).position,
      hand.Get(FingerId.Index, JointId.KnuckleMajor).position
    );
    Vec3 anchor = hand.Get(FingerId.Index, JointId.KnuckleMajor).position;
    anchor = anchor + hand.palm.orientation * Vec3.Forward * d;
    Matrix mAnchor = Matrix.TR(anchor, hand.palm.orientation);
    Matrix mAnchorInv = mAnchor.Inverse;


    Vec3 pad = Vec3.Lerp(
      hand.Get(FingerId.Thumb, JointId.KnuckleMinor).position,
      hand.Get(FingerId.Thumb, JointId.Tip).position,
      0.5f
    );
    Vec3 localPad = mAnchorInv.Transform(pad);

    Color color = Color.White;
    if (localPad.Length < 0.015f) {
      color = new Color(1, 0, 0);
    }
    if (localPad.Length > 0.055f) {
      color = new Color(0, 1, 1);
    }

    if (localPad.Length < 0.03f && isTracking) {
			delta = PullRequest.Relative(
        hand.palm.orientation,
        PullRequest.Delta(localPad.Normalized, oldLocalPad.Normalized)
      ).Normalized;
    }

		if (isTracking) {
			Quat newOri = delta * cursor.orientation;
			if (new Vec3(newOri.x, newOri.y, newOri.z).LengthSq > 0) {
				cursor.orientation = newOri;
			}
		}

    // Lines.Add(
    //   mAnchor.Transform(padIn),
    //   mAnchor.Transform(padOut), 
    //   new Color(1, 1, 1), 0.004f
    // );

		// show that you are about to boolean in and out

		// trackballer demo
		// fly around a "ship" with the cursor
		// and turn it with the thumb trackballer


    oldLocalPad = localPad;



    Mesh.Sphere.Draw(mat, Matrix.TRS(anchor, cursor.orientation, 0.045f), color);
    // Mesh.Cube.Draw(Material.Default, Matrix.TRS(anchor, cursor.orientation, 0.02f));


    if (isTracking) { Demo(); }
  }

  public bool backhand = true;
  public float deadzone = 0.1f;
  public float strength = 3f;
  public int handId = 0;


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


  Vec3 smoothPos;
  Vec3[] points = new Vec3[64];
  void Demo() {
    points[0] = smoothPos = Vec3.Lerp(smoothPos, cursor.position, Time.Elapsedf * 6f);

    int len = (int)(points.Length * Mono.inst.trailLen);
    for (int i = 0; i < len; i++) {
      if (i > 0) {
        Vec3 dir = Vec3.Forward;
        if (points[i].v != points[i - 1].v) {
          dir = PullRequest.Direction(points[i], points[i - 1]);
        }
        points[i] = points[i - 1] + dir * 0.02f * Mono.inst.trailScl;
      }

      Vec3 from = i > 0 ? points[i - 1] : smoothPos;

      Mesh.Cube.Draw(
        Material.Default,
        Matrix.TRS(points[i], Quat.LookDir(PullRequest.Direction(points[i], from)), 0.01f * Mono.inst.trailScl),
        Color.HSV(i / (float)len, 1, 1)
      );
    }
  }
}