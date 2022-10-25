namespace Oriels;

class Trackballer : dof {

  // data
  public Btn btnIn, btnOut;
	bool onTheBall;
  public Quat ori = Quat.Identity;

  Quat momentum = Quat.Identity;
  Quat delta    = Quat.Identity;
	Matrix pad    = Matrix.Identity;
	Matrix oldPad = Matrix.Identity;
	int lastClosestIndex;

	PullRequest.Vec3PID compliance = new PullRequest.Vec3PID();

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
			UpdateMomentum(hand);
    }

		Quat newOri = (momentum * ori).Normalized;
		if (new Vec3(newOri.x, newOri.y, newOri.z).LengthSq > 0) {
			ori = newOri;
		}
  }

	public void UpdateMomentum(Hand hand) {
		// Thumb pad
		HandJoint thumbJoint = hand.Get(FingerId.Thumb, JointId.Tip);
		oldPad = pad;
		pad = Matrix.TRS(
			thumbJoint.position,
			thumbJoint.orientation,
			new Vec3(handed == Handed.Left ? -1f : 1f, 1f, 1f) * 0.1666f
		);
		mesh.Draw(Mono.inst.matHolo, pad, new Color(0, 1, 1));

		// Ball anchor
		HandJoint ballJoint = hand.Get(FingerId.Index, JointId.KnuckleMajor);
		Vec3 anchorPos = ballJoint.position + hand.palm.orientation * new Vec3(
			Mono.inst.tbX.value * U.cm * (handed == Handed.Left ? -1 : 1),
			Mono.inst.tbY.value * U.cm,
			Mono.inst.tbZ.value * U.cm
		);
		anchorPos += compliance.Update(
			Vec3.Zero, 
			onTheBall ? 1f   : 10f, 
			onTheBall ? 0.1f : 1f // 10x less integral when on the ball?
		);
		// compliance;
		// compliance = Vec3.Lerp(compliance, Vec3.Zero, Time.Elapsedf * 10f);
		Matrix anchor = Matrix.TR(anchorPos, hand.palm.orientation);
		Matrix anchorInv = anchor.Inverse;

		// Traction delta mesh matrix
		Vertex[] verts = mesh.GetVerts();
		float oldClosest = (
			pad.Transform(verts[lastClosestIndex].pos) - anchorPos
		).LengthSq;
		float closest = 100000f;
		int closestIndex = lastClosestIndex;
		for (int i = 0; i < verts.Length; i++) {
			Vec3 v = pad.Transform(verts[i].pos);
			float d = (v - anchorPos).LengthSq;
			if (d < closest && d < oldClosest - 0.00002f) {
				closest = d;
				closestIndex = i;
			}
		}
		lastClosestIndex = closestIndex;

		Vec3 point = anchorInv.Transform(
			pad.Transform(verts[closestIndex].pos)
		);
		Vec3 oldPoint = anchorInv.Transform(
			oldPad.Transform(verts[closestIndex].pos)
		);

		// ?
		// localPad.x = (float)xF.Filter(localPad.x, (double)Time.Elapsedf);
		// localPad.y = (float)yF.Filter(localPad.y, (double)Time.Elapsedf);
		// localPad.z = (float)zF.Filter(localPad.z, (double)Time.Elapsedf);

		// Lines.Add(thumbTip, thumbKnuckle, Color.White, 0.002f);
		Mesh.Sphere.Draw(
			Mono.inst.matHolo,
			Matrix.TRS(anchor.Transform(point), hand.palm.orientation, 0.002f),
			new Color(0, 1, 1)
		);

		float dist = point.Length;
		// if (btnIn.held)  { btnIn.Step(dist < layer[1]); } 
		// else             { btnIn.Step(dist < layer[0]); }
		float inT = btnIn.held ? 1 : 0.333f;

		if (btnOut.held) { btnOut.Step(dist > layer[1]); } 
		else             { btnOut.Step(dist > layer[2]); }
		float outT = btnOut.held ? 1 : 0.333f;

		if (btnIn.held) {
			delta = momentum = Quat.Identity;
		} else {
			onTheBall = dist < layer[1];
			if (onTheBall) {
				delta = Quat.Delta(
					oldPoint.Normalized,
					point.Normalized
				).Relative(hand.palm.orientation);

				momentum = Quat.Slerp(momentum, delta, Time.Elapsedf * 10f);

				Vec3 contact = point.Normalized * layer[1];
				Vec3 offset  = point - contact;

				// no z axis
				// offset.z = 0; 

				offset = hand.palm.orientation * offset;
				compliance.value += offset * Mono.inst.tbCompliance.value;
				compliance.integral = Vec3.Zero;
			}
		}

		// Draw
		Mesh.Sphere.Draw(
			Mono.inst.matHolo,
			Matrix.TRS(anchorPos, ori, layer[1] * 2),
			new Color(inT, 0, 0)
		);
	}

	// design
	public Handed handed = Handed.Left;
  public float[] layer = new float[] { 0.00333f, 0.02f, 0.0666f };
	

	Vec3 cursorPos = new Vec3(0f, 0f, 0f);
	public void Demo() {
		Matrix panel = Matrix.TR(
			new Vec3(
				1.47f, 
				1.145f, //  - World.BoundsPose.position.y, 
				1.08f),
			Quat.FromAngles(-3.2f, 90f, 0)
		);

		float width  = 52 * U.cm;
		float height = 29f * U.cm;
		Mesh.Quad.Draw(
			Mono.inst.matHolo,
			Matrix.S(new Vec3(width, height, 1)) * panel,
			new Color(1, 1, 1)
		);


		cursorPos.x = PullRequest.Clamp(
			cursorPos.x + (momentum * Vec3.Right).z * 0.1f,
			width / -2f, 
			width / 2f
		);
		cursorPos.y = PullRequest.Clamp(
			cursorPos.y + (momentum * Vec3.Right).y * -0.1f,
			height / -2f, 
			height / 2f
		);

		Mesh.Quad.Draw(
			Material.Unlit,
			Matrix.TS(cursorPos, 1 * U.cm) * panel,
			new Color(1, 1, 1)
		);
	}
}


/*
	COMMENTS

	distinct interactions to account for (relative to palm orientation)
		y swipe
		z swipe
		x spin

	how reliable is the provided palm orientation?
	
	more boolean visual and audio feeback
	
*/