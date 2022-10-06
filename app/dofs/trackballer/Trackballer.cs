namespace Oriels;

class Trackballer : dof {

  // data
  public Btn btnIn, btnOut;
  public Quat ori = Quat.Identity;
  Quat momentum = Quat.Identity;
  Quat delta = Quat.Identity;
  Vec3 oldLocalPad;

  public void Init() {}

  public void Frame() {
    Hand hand = Input.Hand(handed);
    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			Vec3 anchor = hand.Get(FingerId.Index, JointId.KnuckleMajor).position;
			anchor = anchor + hand.palm.orientation * new Vec3(-0.015f, 0, -0.045f);
			Matrix mAnchor = Matrix.TR(anchor, hand.palm.orientation);
			Matrix mAnchorInv = mAnchor.Inverse;

      Vec3 thumbTip = hand.Get(FingerId.Thumb, JointId.Tip).position;
			Vec3 thumbKnuckle = hand.Get(FingerId.Thumb, JointId.KnuckleMinor).position;
      Vec3 pad = anchor.SnapToLine(
				thumbKnuckle, thumbTip, 
				true,
				out float t, 0, 0.9f
			);
      // t = 1 - t;
      t = t * t;
			t = 1 - t;
      pad += hand.Get(FingerId.Thumb, JointId.Tip).orientation * -Vec3.Up * 0.00666f * t;
      Vec3 localPad = mAnchorInv.Transform(pad);

			Lines.Add(thumbTip, thumbKnuckle, Color.White, 0.002f);
      Mesh.Sphere.Draw(Mono.inst.matDev, Matrix.TRS(pad, hand.palm.orientation, 0.004f), new Color(0, 1, 0));

      Color color = Color.White;
			if (btnIn.held) {
        btnIn.Step(localPad.Length < layer[1]);
			} else {
      	btnIn.Step(localPad.Length < layer[0]);
			}
			color = btnIn.held ? new Color(1, 0, 0) : color;

      if (btnOut.held) {
        btnOut.Step(localPad.Length > layer[1]);
      } else {
				btnOut.Step(localPad.Length > layer[2]);
			}
			color = btnOut.held ? new Color(0, 1, 0) : color;

			if (btnIn.held) {
				delta = momentum = Quat.Identity;
			} else {
				if (localPad.Length < layer[1]) {
					delta = PullRequest.Relative(
						hand.palm.orientation,
						PullRequest.Delta(localPad.Normalized, oldLocalPad.Normalized)
					).Normalized;

					momentum = Quat.Slerp(momentum, delta, Time.Elapsedf * 10f);
				}
			}

    	oldLocalPad = localPad;

			// Draw
			Mesh.Cube.Draw(Mono.inst.matDev, Matrix.TRS(anchor, ori, 0.04f), color);
    }



		// pad momentum! 
		// like we did w/ vader life alyx immortal
		// all the difference in the world!
		// and makes for the third iteration of the trackballer



		Quat newOri = momentum * ori;
		if (new Vec3(newOri.x, newOri.y, newOri.z).LengthSq > 0) {
			ori = newOri;
		}
    
    // show that you are about to boolean in and out

    // trackballer demo
    // fly around a "ship" with the cursor
    // and turn it with the thumb trackballer
  }

	// design
  public Handed handed = Handed.Left;
  public float[] layer = new float[] { 0.00333f, 0.02f, 0.0666f };


	
  public float scale = 1;
}
