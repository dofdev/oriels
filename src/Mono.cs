namespace Oriels;

public class Mono {
	private static readonly Lazy<Mono> lazy = new Lazy<Mono>(() => new Mono());
	public static Mono inst { get { return lazy.Value; } }

	public PR.Noise noise = new(939949595);

	public Mat mat = new();

	public Rig rig = new();
	public Space space = new();
	public Compositor compositor = new();

	// -------------------------------------------------

	public Interaction[] interactions;

	public ColorCube colorCube = new();

	public Glove rGlove = new(true), lGlove = new(false);
	public Glove Glove(bool chirality) { return chirality ? rGlove : lGlove; }

	// -------------------------------------------------

	// public MonoNet net = new MonoNet();

	public Mono() {
		interactions = new Interaction[] {
			new Chiral(new Interaction[] {
				new WaveCursor()  { handed = Handed.Left  },
				new WaveCursor()  { handed = Handed.Right }
			}),
			new Chiral(new Interaction[] {
				new Trackballer() { handed = Handed.Left  },
				new Trackballer() { handed = Handed.Right }
			}),
			new Chiral(new Interaction[] {
				new RollsCursor() { handed = Handed.Left  },
				new RollsCursor() { handed = Handed.Right }
			}),
		};
	}

	public void Init() {
		compositor.Init();

		for (int i = 0; i < interactions.Length; i++) {
			interactions[i].Init();
		}

		mat.Init();
	}

	Pose shape = new Pose(new Vec3(0, 1f, -3f), Quat.FromAngles(45, 0, 45));
	bool shapeHeld = false;


	Pose testPose = new Pose(new Vec3(0, 1f, -1.5f), Quat.FromAngles(45, 0, 45));


	Spatial spatial = new (new Vec3(-1, 0.76f, 0.666f));
	Cursor cursor = new();
	Drawer drawerA = new (new Pose(new Vec3(-0.8f, 0.6f, 1.4f), Quat.FromAngles(0, 90f, 0)));
	Drawer drawerB = new (new Pose(new Vec3(-0.8f, 0.6f, 0.95f), Quat.FromAngles(0, 90f, 0)));
	Drawer drawerC = new (new Pose(new Vec3(-0.8f, 0.6f, 0.5f), Quat.FromAngles(0, 90f, 0)));

	public void Frame() {

		// Input.HandClearOverride(Handed.Left);
		// Input.HandClearOverride(Handed.Right);
		// store hand pre override in rig
		rig.Step();
		// Hand h = Input.Hand(Handed.Right);
		// if (h.pinch.IsActive()) {
		// 	Console.WriteLine($"{h.pinchPt}, {Input.Head.orientation * -Vec3.Forward}");
		// }

    // Con -> Hand
    // lGlove.Step();
    // rGlove.Step();

    compositor.Frame();
		
		spatial.Frame();

		// we need cursor abstraction
		// so we can plug in different cursor interactions into different elements!

		// pinch-cursor?
		{
			float deadzone = 0.01f;
			float strength = 6f;

			Hand hand = Input.Hand(Handed.Right);
			// left and right 'click' ~ curl
			Btn lBtn = new Btn(); // ~ trigger ~ left click
			float indexCurl = rig.FingerFlex(hand, FingerId.Index, 0.15f);
			lBtn.Frame(indexCurl < 0.5f);

			// stay in state until both ring and pinky finger 'agree' on curl or uncurl
			Btn rBtn = new Btn(); // ~ grip ~ right click
			float ringCurl = rig.FingerFlex(hand, FingerId.Ring, 0.15f);
			float pinkyCurl = rig.FingerFlex(hand, FingerId.Little, 0.15f);
			rBtn.Frame(ringCurl < 0.5f && pinkyCurl < 0.5f);

			// cursor
			Vec3 midTip   = hand.Get(FingerId.Middle, JointId.Tip).position;
			Vec3 thumbTip = hand.Get(FingerId.Thumb,  JointId.Tip).position;

			Vec3 delta  = midTip - thumbTip;
			float mag   = delta.Magnitude;
			float pinch = MathF.Max(mag - deadzone, 0);

			Vec3 dir = delta.Normalized;

			cursor.raw = midTip + dir * pinch * strength;

			Lines.Add(midTip, thumbTip, new Color(0, 0, 1), 0.002f);
			Mesh.Sphere.Draw(
				mat.holo,
				Matrix.TS(cursor.pos, 2*U.cm),
				new Color(
					lBtn.held ? 1.0f : 0.0f,
					0.5f,
					rBtn.held ? 1.0f : 0.0f
				)
			);

			if (rBtn.held) {
				testPose.position = cursor.smooth;
			}
			Mesh.Cube.Draw(
				mat.holoframe,
				testPose.ToMatrix(5*U.cm),
				rBtn.held ? new Color(0.5f, 0.55f, 0.75f) :
		                new Color(0.5f, 0.55f, 0.75f) * 0.3f
			);

			drawerA.Frame(cursor, pinch);
			drawerB.Frame(cursor, pinch);
			drawerC.Frame(cursor, pinch);
		}



		// Input.Subscribe(InputSource.Hand, BtnState.Any, Action<Hand, BtnState.Any, Pointer>);

		// -------------------------------------------------

		for (int i = 0; i < interactions.Length; i++) {
			if (interactions[i].Active) {
				interactions[i].Frame();
			}
		}

		// <Heresy>
		WaveCursor  lwc = (WaveCursor)((Chiral)interactions[0]).dofs[0];
		WaveCursor  rwc = (WaveCursor)((Chiral)interactions[0]).dofs[1];
		Trackballer ltb = (Trackballer)((Chiral)interactions[1]).dofs[0];
		Trackballer rtb = (Trackballer)((Chiral)interactions[1]).dofs[1];

		if (lwc.Active) {
			lwc.Demo(ltb.ori);
		}
		if (rwc.Active) {
			rwc.Demo(rtb.ori);
		}

		if (rtb.Active) {
			rtb.Demo();
		}

		if (!shapeHeld) {
			shapeHeld = rtb.btnPush.frameDown;
		}
		if (shapeHeld) {
			shape.position = rwc.cursor.smooth;
			shape.orientation = rtb.ori;
			shapeHeld = !rtb.btnPull.frameDown;
		}
		
		// I'd rather have it be pose.pos & pose.ori
		// as it's a bit of space hog

		Mesh.Cube.Draw(
			mat.holoframe,
			shape.ToMatrix(0.5f),
			new Color(0.5f, 0.55f, 0.75f) * 0.3f
		);


		// </Heresy>


		// rBlock.Step(); lBlock.Step();

		// cubicCon.Step();

		// colorCube.Palm(lCon.device);

		// -------------------------------------------------

		// net.me.Step();
		// net.send = true;

		window.Frame();
	}
	Window window = new Window();
}

/* 
	we have a whole inspector thing going on here

	but people are working on better alternatives:
		malek's foss social xr project

	which was part of the reason that i chopped off the networking parts of this project.
	as we have vrc for reaching larger audiences + malek's project to develop w/the fossxr community
	so running our own networking is needlessly redundant, and not my strong suit.

	refocusing this project on just prototyping and hosting our xr tools centrally
	  to then be ported to wherever they can best be applied ^-^


	the inspector is a crutch for the lack of a native spatial interface for prototyping
	as it's an incredibly limited and an awkward abstraction of what is happening spatially

	expose spatial functions and dofs
	allow the user to bind them to tracked inputs+
	*don't need it all to be fully featured and extensible out of the gate
		just need a better foundation than a paper paradigm inspector


	'world origin' needs to be adjustable~
		otherwise the visualizations will be difficult to decipher in different contexts

	no names! as everything remains in it's original context
		text for math symbols is fine~
			but don't use that as an excuse to abstract things back into text

	you can do vector math spatially
		by wrapping the living vectors with operators~
			i.e  av + bv  =  cv
					(-- + --) -> --
			it's hard to represent this with text :<
			but just think of different points/lines(vectors) being encapsulated by 
			underlying larger points/lines(vectors) with symbols or other identifiers
			with an output, managing to represent the underlying math within the spatial context


	side notes
		need to run it in a way where if it crashes, it doesn't take the whole app down (ask malek?)

		friction flip thumb swipe
		overcome with >x force impulse
		local to palm

		dofchan bows on the back of the ankles that double as trackers
*/