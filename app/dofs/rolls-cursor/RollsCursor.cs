namespace Oriels;

class RollsCursor : dof {
	public bool Active { get; set; }

	// input
	public Handed handed = Handed.Left;

	// data
	public Cursor cursor = new Cursor();

	public void Init() { }

	public void Frame() {
		Rig rig = Mono.inst.rig;
		Hand hand = Input.Hand(handed);
		if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
			float fI = rig.Flexion(hand, FingerId.Index);
			float fM = rig.Flexion(hand, FingerId.Middle);
			float fR = rig.Flexion(hand, FingerId.Ring);
			float fL = rig.Flexion(hand, FingerId.Little);

			// Biased by finger length
			float stretch = (fI + fI + fM + fM + fM + fR + fR + fL) / 8f;

			Vec3 to   = Roll(hand, JointId.KnuckleMid,   fI, fM, fR, fL);
			Vec3 from = Roll(hand, JointId.KnuckleMajor, fI, fM, fR, fL);

			Vec3 dir = PullRequest.Direction(to, from);

			cursor.raw = to + dir * stretch * reach.value;

			Mesh.Sphere.Draw(Mono.inst.matHoloframe, Matrix.TRS(cursor.raw,    Quat.Identity, 0.01f), new Color(1, 0, 0));
			Mesh.Sphere.Draw(Mono.inst.matHoloframe, Matrix.TRS(cursor.pos,    Quat.Identity, 0.01f), new Color(0, 1, 0));
			Mesh.Sphere.Draw(Mono.inst.matHoloframe, Matrix.TRS(cursor.smooth, Quat.Identity, 0.01f), new Color(0, 0, 1));
		}
	}

	// design
	public Design reach = new Design { str = "1.0", term = "0+m",  min = 0 };

	public Vec3 Roll(Hand hand, JointId jointId, float fI, float fM, float fR, float fL) {
		Vec3 i = hand.Get(FingerId.Index,  jointId).position;
		Vec3 m = hand.Get(FingerId.Middle, jointId).position;
		Vec3 r = hand.Get(FingerId.Ring,   jointId).position;
		Vec3 l = hand.Get(FingerId.Little, jointId).position;

		fI = PullRequest.Clamp(fI, 0.0001f, 1f);
		fM = PullRequest.Clamp(fM, 0.0001f, 1f);
		fR = PullRequest.Clamp(fR, 0.0001f, 1f);
		fL = PullRequest.Clamp(fL, 0.0001f, 1f);

		Vec3 im   = Vec3.Lerp(i  , m  , fM / (fM + fI));
		Vec3 mr   = Vec3.Lerp( m ,  r , fR / (fR + fM));
		Vec3 rl   = Vec3.Lerp(  r,   l, fL / (fL + fR));

		Vec3 imr  = Vec3.Lerp(im , mr , fR / (fR + fM + fI));
		Vec3 mrl  = Vec3.Lerp( mr,  rl, fL / (fL + fR + fM));

		Vec3 imrl = Vec3.Lerp(imr, mrl, fL / (fL + fR + fM + fI));

		return imrl;
	}
}



/* 
	COMMENTS

*/