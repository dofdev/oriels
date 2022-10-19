namespace Oriels;

class Trackballer : dof {

  // data
  public Btn btnIn, btnOut;
  public Quat ori = Quat.Identity;
  Quat momentum = Quat.Identity;
  Quat delta = Quat.Identity;
	Matrix oldMeshMatrix = Matrix.Identity;

	PullRequest.OneEuroFilter xF = new PullRequest.OneEuroFilter(0.0001f, 0.1f);
	PullRequest.OneEuroFilter yF = new PullRequest.OneEuroFilter(0.0001f, 0.1f);
	PullRequest.OneEuroFilter zF = new PullRequest.OneEuroFilter(0.0001f, 0.1f);

	Model model = Model.FromFile("thumb_pad.glb");
	Mesh mesh;

	public void Init() {
		mesh = model.GetMesh("Pad");
	}

  public void Frame() {
    Hand hand = Input.Hand(handed);
    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			Vec3 anchor = hand.Get(FingerId.Index, JointId.KnuckleMajor).position;
			anchor = anchor + hand.palm.orientation * new Vec3(handed == Handed.Left ? -0.006f : 0.006f, 0.01f, -0.04f);
			Matrix mAnchor = Matrix.TR(anchor, hand.palm.orientation);
			Matrix mAnchorInv = mAnchor.Inverse;

      Vec3 thumbTip = hand.Get(FingerId.Thumb, JointId.Tip).position;
			// Vec3 tipDelta = mAnchorInv.Transform(thumbTip) - mAnchorInv.Transform(oldTip);
			// oldTip = thumbTip;
			Vec3 thumbKnuckle = hand.Get(FingerId.Thumb, JointId.KnuckleMinor).position;

			Quat thumbRot = hand.Get(FingerId.Thumb, JointId.Tip).orientation;
			Matrix mMesh = Matrix.TRS(
				thumbTip,
				thumbRot,
				new Vec3(handed == Handed.Left ? -1f : 1f, 1f, 1f) * 0.1666f
			);
			mesh.Draw(Mono.inst.matHolo, mMesh, new Color(0, 0, 1));

			// closest to anchor
			float closest = 100000f;
			int closestIndex = -1;
			Vertex[] verts = mesh.GetVerts();
			for (int i = 0; i < verts.Length; i++) {
				Vec3 v = mMesh.Transform(verts[i].pos);
				float d = (v - anchor).LengthSq;
				if (d < closest) {
					closest = d;
					closestIndex = i;
				}
			}

      Vec3 localPad = mAnchorInv.Transform(mMesh.Transform(verts[closestIndex].pos));
			Vec3 oldPad   = mAnchorInv.Transform(oldMeshMatrix.Transform(verts[closestIndex].pos));

			oldMeshMatrix = mMesh;

			// 
			// Vec3 pad = anchor.SnapToLine(
			// 	thumbKnuckle, thumbTip,
			// 	true,
			// 	out float t, 0f, 1f
			// );
			// // t = 1 - t;
			// // scale to 0.666f - 1f
			// // t = (t - 0.666f) / 0.334f;
			// t = t * t;
			// t = 1 - t;
      // pad += hand.Get(FingerId.Thumb, JointId.Tip).orientation * -Vec3.Up * 0.00666f * t;


      // Vec3 localPad = mAnchorInv.Transform(pad);
      // Vec3 localPad = mAnchorInv.Transform(thumbTip);

			// ?
			// localPad.x = (float)xF.Filter(localPad.x, (double)Time.Elapsedf);
			// localPad.y = (float)yF.Filter(localPad.y, (double)Time.Elapsedf);
			// localPad.z = (float)zF.Filter(localPad.z, (double)Time.Elapsedf);

			

			// Lines.Add(thumbTip, thumbKnuckle, Color.White, 0.002f);
      Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(mAnchor.Transform(localPad), hand.palm.orientation, 0.004f), new Color(0, 1, 0));

      
      // if (btnIn.held) {
      //   btnIn.Step(localPad.Length < layer[1]);
			// } else {
      // 	btnIn.Step(localPad.Length < layer[0]);
			// }
			float inT = btnIn.held ? 1 : 0.333f;

      if (btnOut.held) {
        btnOut.Step(localPad.Length > layer[1]);
      } else {
				btnOut.Step(localPad.Length > layer[2]);
			}
      float outT = btnOut.held ? 1 : 0.333f;

      if (btnIn.held) {
				delta = momentum = Quat.Identity;
			} else {
				if (localPad.Length < layer[1]) {
					delta = PullRequest.Relative(
						hand.palm.orientation,
						Quat.Delta(
							oldPad.Normalized,
							localPad.Normalized
						)
					).Normalized;

					momentum = Quat.Slerp(momentum, delta, Time.Elapsedf * 10f);
				}
			}

			// Draw
			Mesh.Sphere.Draw(Mono.inst.matHolo, Matrix.TRS(anchor, ori, 0.04f), new Color(inT, 0, 0));
			Mesh.Cube.Draw(Mono.inst.matHolo, Matrix.TRS(anchor, ori, 0.04f), new Color(0, outT * 0.2f, 0));
    }

		Quat newOri = momentum * ori;
		if (new Vec3(newOri.x, newOri.y, newOri.z).LengthSq > 0) {
			ori = newOri;
		}
  }

	// design
  public Handed handed = Handed.Left;
  public float[] layer = new float[] { 0.00333f, 0.02f, 0.0666f };
	
}


/*
	COMMENTS

	distinct interactions to account for (relative to palm orientation)
		w/rating assuming perfect tracking
		y swipe (10/10) 
		z swipe (05/10)
		x spin  (02/10)

	how reliable is the provided palm orientation?
	
	show when you are about to boolean
	
	2d pad?
*/