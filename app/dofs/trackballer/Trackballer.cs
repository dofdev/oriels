namespace Oriels;

class Trackballer : dof {

  // data
  public Btn btnIn, btnOut;
  public Quat ori = Quat.Identity;
  Vec3 oldLocalPad;
  Quat delta = Quat.Identity;

  public void Init() {}

  public void Frame() {
    Hand hand = Input.Hand(handed);
    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			Vec3 anchor = hand.Get(FingerId.Index, JointId.KnuckleMajor).position;
			anchor = anchor + hand.palm.orientation * Vec3.Forward * 0.045f;
			Matrix mAnchor = Matrix.TR(anchor, hand.palm.orientation);
			Matrix mAnchorInv = mAnchor.Inverse;

			Vec3 pad = Vec3.Lerp(
				hand.Get(FingerId.Thumb, JointId.KnuckleMinor).position,
				hand.Get(FingerId.Thumb, JointId.Tip).position,
				0.5f
			);
			Vec3 localPad = mAnchorInv.Transform(pad);

			Color color = Color.White;
			if (btnIn.held) {
        btnIn.Step(localPad.Length < layer[1]);
			} else {
      	btnIn.Step(localPad.Length < layer[0]);
			}
			color = btnIn.held ? new Color(1, 0, 0) : Color.White;

			
      btnOut.Step(localPad.Length > layer[2]);
			if (localPad.Length > layer[2]) {
				color = new Color(0, 1, 1);
			}

			if (localPad.Length < layer[1]) {
				delta = PullRequest.Relative(
					hand.palm.orientation,
					PullRequest.Delta(localPad.Normalized, oldLocalPad.Normalized)
				).Normalized;
			}

    	oldLocalPad = localPad;

			// Draw
			Mesh.Sphere.Draw(Mono.inst.matDev, Matrix.TRS(anchor, ori, 0.045f), color);
    }

		Quat newOri = delta * ori;
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
  public float[] layer = new float[] { 0.015f, 0.03f, 0.055f };


	
  public float scale = 1;
}
