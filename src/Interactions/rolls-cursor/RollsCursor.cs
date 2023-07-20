namespace Oriels;

class RollsCursor : Interaction {
	public bool Active { get; set; }

	// input
	public Handed handed = Handed.Left;

	// data
	public Cursor cursor = new Cursor();
	PR.Delta fIdelta = new PR.Delta();
	PR.Delta fMdelta = new PR.Delta();
	PR.Delta fRdelta = new PR.Delta();

	public void Init() { }

	public void Frame() {
		Mono mono = Mono.inst;

		Hand hand = Input.Hand(handed);
		if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			float fI = mono.rig.Flexion(hand, FingerId.Index);
			float fM = mono.rig.Flexion(hand, FingerId.Middle);
			float fR = mono.rig.Flexion(hand, FingerId.Ring);
			float fL = mono.rig.Flexion(hand, FingerId.Little);

			float stretch = (fI + fM + fR + fL) / 4f;

			// Vec3 to   = Roll(hand, JointId.KnuckleMid,   fI, fM, fR, fL);
			// Vec3 from = Roll(hand, JointId.KnuckleMajor, fI, fM, fR, fL);

			// Vec3 dir = Vec3.Direction(to, from);

			// cursor.raw = to + dir * stretch * reach.value;

			if (fL == 0.0f) {
				cursor.raw = hand.Get(FingerId.Index,  JointId.Tip).position;
			}

			fIdelta.Update(hand.Get(FingerId.Index,  JointId.Tip).position);
			fMdelta.Update(hand.Get(FingerId.Middle, JointId.Tip).position);
			fRdelta.Update(hand.Get(FingerId.Ring,   JointId.Tip).position);

			Vec3 delta = (fIdelta.value + fMdelta.value + fRdelta.value) / 3f;
			cursor.raw += delta * reach.value;

			Mesh.Sphere.Draw(mono.mat.holoframe, Matrix.TRS(cursor.raw,    Quat.Identity, 0.01f), new Color(1, 0, 0));
			Mesh.Sphere.Draw(mono.mat.holoframe, Matrix.TRS(cursor.pos,    Quat.Identity, 0.01f), new Color(0, 1, 0));
			Mesh.Sphere.Draw(mono.mat.holoframe, Matrix.TRS(cursor.smooth, Quat.Identity, 0.01f), new Color(0, 0, 1));
		}
	}

	// design
	public Design reach = new Design { str = "3.0", term = "0+m",  min = 0 };

	public Vec3 Roll(Hand hand, JointId jointId, float fI, float fM, float fR, float fL) {
		Vec3 i = hand.Get(FingerId.Index,  jointId).position;
		Vec3 m = hand.Get(FingerId.Middle, jointId).position;
		Vec3 r = hand.Get(FingerId.Ring,   jointId).position;
		Vec3 l = hand.Get(FingerId.Little, jointId).position;

		fI = PR.Clamp(fI, 0.0001f, 1f);
		fM = PR.Clamp(fM, 0.0001f, 1f);
		fR = PR.Clamp(fR, 0.0001f, 1f);
		fL = PR.Clamp(fL, 0.0001f, 1f);

		Vec3 im   = Vec3.Lerp(i  , m  , fM / (fM + fI));
		Vec3  mr  = Vec3.Lerp( m ,  r , fR / (fR + fM));
		Vec3   rl = Vec3.Lerp(  r,   l, fL / (fL + fR));

		Vec3 imr  = Vec3.Lerp(im , mr , fR / (fR + fM + fI));
		Vec3  mrl = Vec3.Lerp( mr,  rl, fL / (fL + fR + fM));

		Vec3 imrl = Vec3.Lerp(imr, mrl, fL / (fL + fR + fM + fI));

		return imrl;
	}
}



/* 
	COMMENTS

	reach out to nova for any help or perspectives on the problem

	un stagger the roll
	bias direction on index and pinky
*/