namespace Oriels;

class Trackballer : Interaction {
	public bool Active { get; set; }

	// input
	public Handed handed = Handed.Left;

	// data
	public Btn btnPull = new Btn();
	public Btn btnPush = new Btn();
	bool onTheBall;
  public Quat ori = Quat.Identity;

  Quat momentum = Quat.Identity;
  Quat delta    = Quat.Identity;
	Matrix pad    = Matrix.Identity;
	Matrix oldPad = Matrix.Identity;
	int lastClosestIndex;

	PR.Vec3PID compliance = new PR.Vec3PID();

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
		mesh.Draw(Mono.inst.matHoloframe, pad, new Color(0, 1, 1));

		// Ball anchor
		HandJoint ballJoint = hand.Get(FingerId.Index, JointId.KnuckleMajor);
		Vec3 anchorOrigin = ballJoint.position + hand.palm.orientation * new Vec3(
			aX.value * (handed == Handed.Left ? -1 : 1),
			aY.value,
			aZ.value
		);
		Vec3 anchorPos = anchorOrigin + compliance.Update(
			Vec3.Zero, 
			onTheBall ? 1f   : 10f, 
			onTheBall ? 0.5f : 5f // 10x less integral when on the ball?
		);
		// compliance;
		// compliance = Vec3.Lerp(compliance, Vec3.Zero, Time.Stepf * 10f);
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


		// Pull
		float pull = point.Length / pullClick.value;
		btnPull.Frame(pull > 1f, pull > 0.333f); // magic sticky var

		float pullScalar = btnPull.held ? MathF.Max((pull - 0.333f) / 0.666f, 0) : MathF.Max(1 - pull, 0);
		Mesh.Sphere.Draw(Mono.inst.matHoloframe,
			Matrix.TRS(anchorPos, thumbJoint.orientation, pullScalar * radius.value),
			new Color(0, 1, 1) * (btnPull.held ? 1f : 0.0666f)
		);
		Lines.Add(
			anchor.Transform(point), anchorPos,
			new Color(0, 1, 1), 1f * U.mm
		);
		Mesh.Sphere.Draw(Mono.inst.matHoloframe,
			Matrix.TRS(anchor.Transform(point), thumbJoint.orientation, 2f * U.mm),
			new Color(0, 1, 1)
		);


		// Push
		float push = compliance.value.Length / pushClick.value;
		btnPush.Frame(push > 1f, push > 0.333f); // magic sticky var

		float pushScalar = btnPush.held ? MathF.Max((MathF.Min(push, 1f) - 0.333f) / 0.666f, 0) : MathF.Max(1 - push, 0);
		Mesh.Sphere.Draw(Mono.inst.matHoloframe,
			Matrix.TRS(anchorPos, ori, (radius.value * 2) * pushScalar),
			new Color(1, 0, 0) * (btnPush.held ? 1f : 0.2f)
		);


		onTheBall = point.Length < radius.value;
		if (onTheBall) {
			delta = Quat.Delta(
				oldPoint.Normalized,
				point.Normalized
			).Relative(hand.palm.orientation);

			momentum = Quat.Slerp(momentum, delta, Time.Stepf * 10f);

			Vec3 contact = point.Normalized * radius.value;
			Vec3 offset  = point - contact;

			// no z axis
			// offset.z = 0; 
			offset = hand.palm.orientation * offset;
			compliance.value += offset * compliant.value;
			compliance.integral = Vec3.Zero;
		} else {
			PR.ToAxisAngle(momentum, out Vec3 axis, out float angle);
			if (angle < stop.value) {
				momentum = Quat.Slerp(momentum, Quat.Identity, Time.Stepf * 10f);
			}
		}

		// Draw ball result
		Mesh.Sphere.Draw(
			Mono.inst.matHoloframe,
			Matrix.TRS(anchorPos, ori, radius.value * 2),
			new Color(0.8f, 0, 0)
		);
	}

	// design
	public Design radius    = new Design { str="2", term=">0cm", unit=U.cm, min=0.5f };
	public Design pullClick = new Design { str="6.66", term=">0cm", unit=U.cm, min=0.1f };
	public Design pushClick = new Design { str="1.5", term=">0cm", unit=U.cm, min=0.1f };
	public Design aX = new Design { str=" 1.0", term="-0+cm", unit=U.cm, min=-10f, max=10f };
	public Design aY = new Design { str=" 2.0", term="-0+cm", unit=U.cm, min=-10f, max=10f };
	public Design aZ = new Design { str="-4.0", term="-0+cm", unit=U.cm, min=-10f, max=10f };
	public Design compliant = new Design { str="0.2", term="0+1t", min=0, max=1 };
	public Design stop = new Design { str="0.05", term="0+", min=0 };
	

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
			Mono.inst.matHoloframe,
			Matrix.S(new Vec3(width, height, 1)) * panel,
			new Color(1, 1, 1)
		);


		cursorPos.x = PR.Clamp(
			cursorPos.x + (momentum * Vec3.Right).z * 0.1f,
			width / -2f, 
			width / 2f
		);
		cursorPos.y = PR.Clamp(
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

	press in is not reliable

	full thumb mesh and contact point lerp

	pullbtn mouse click scale anim

	thumb inside instead
	so the natural thumb position doesn't affect the trackballer

	sens for cursor

	be able to stop it easily without jittering
		use another fingers flexion to be able to brake the trackballer

	finger gun interaction (on the other hand) to move the trackballer

	distinct interactions to account for (relative to palm orientation)
		y swipe (move x)
		z swipe (move y)
		x spin  (scroll)

	how reliable is the provided palm orientation?
	
	more boolean visual and audio feeback
	
*/