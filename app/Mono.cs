namespace Oriels;

public class Mono {
	private static readonly Lazy<Oriels.Mono> lazy = new Lazy<Oriels.Mono>(() => new Oriels.Mono());
	public static Oriels.Mono inst { get { return lazy.Value; } }

	public PullRequest.Noise noise = new PullRequest.Noise(939949595);

	public Material matDev;
	public Material matHolo;

	public Rig rig = new Rig();
	public Space space = new Space();
	public Compositor compositor = new Compositor();

	// -------------------------------------------------

	public dof[] dofs;
	int dofIndex = 0;
	dof dof => dofs[dofIndex];

	public ColorCube colorCube = new ColorCube();

	public Glove rGlove = new Glove(true), lGlove = new Glove(false);
	public Glove Glove(bool chirality) { return chirality ? rGlove : lGlove; }

	public BlockCon rBlock = new BlockCon(true), lBlock = new BlockCon(false);
	public BlockCon BlockCon(bool chirality) { return chirality ? rBlock : lBlock; }
	public Block[] blocks = new Block[] {
		new Block(), new Block(), new Block(), new Block(), new Block(), new Block()
	};

	public CubicCon cubicCon = new CubicCon();
	public Cubic[] cubics = new Cubic[] {
		new Cubic(), new Cubic(), new Cubic(), new Cubic(), new Cubic(), new Cubic()
	};

	// -------------------------------------------------

	public MonoNet net = new MonoNet();

	public Mono() {
		Renderer.SetClip(0.02f, 1000f);

		dofs = new dof[] {
			// new StretchFinger(),
			new WaveCursor()  { handed = Handed.Left  }, 
			new WaveCursor()  { handed = Handed.Right },
			new Trackballer() { handed = Handed.Left  },
			new Trackballer() { handed = Handed.Right },
			// new StretchCursor() { },
			// new StretchCursor() { },
		};
	}

	public void Init() {
		compositor.Init();

		dofs[0].Init();
		dofs[1].Init();
		dofs[2].Init();
		dofs[3].Init();


		matDev = Material.Default.Copy();
		matDev.SetTexture("diffuse", Tex.DevTex);
		matHolo = Material.Default.Copy();
		matHolo.Transparency = Transparency.Add;
		matHolo.DepthWrite = false;
		matHolo.DepthTest = DepthTest.Always;
		matHolo.SetTexture("diffuse", Tex.DevTex);
	}

	public void Frame() {

		// Input.HandClearOverride(Handed.Left);
		Input.HandClearOverride(Handed.Right);
		// store hand pre override in rig
		rig.Step();

		// Hand hand = Input.Hand(Handed.Right);
    // Controller con = Input.Controller(Handed.Right);
		// Mesh.Cube.Draw(
		// 	Material.Default,
		// 	hand.IsJustPinched
		// )

    // Con -> Hand
    // lGlove.Step();
    // rGlove.Step();

    compositor.Frame();

		// -------------------------------------------------

		// // dof.Frame();    
		dofs[0].Frame();
		dofs[1].Frame();
		dofs[2].Frame();
		dofs[3].Frame();

		WaveCursor lwc = (WaveCursor)dofs[0];
		WaveCursor rwc = (WaveCursor)dofs[1];
		Trackballer ltb = (Trackballer)dofs[2];
		Trackballer rtb = (Trackballer)dofs[3];

		lwc.Demo(ltb.ori);
		rwc.Demo(rtb.ori);

		Mesh.Cube.Draw(Mono.inst.matHolo, 
			Matrix.TRS(
				lwc.cursor.position, 
				Quat.Identity, // ltb.ori, 
				0.015f
			), 
			new Color(0, 1, 1)
		);

		Mesh.Cube.Draw(Mono.inst.matHolo, 
			Matrix.TRS(
				rwc.cursor.position,
				Quat.Identity, // rtb.ori, 
				0.015f
			), 
			new Color(0, 1, 1)
		);

		// rBlock.Step(); lBlock.Step();

		// cubicCon.Step();

		// colorCube.Palm(lCon.device);

		// -------------------------------------------------

		net.me.Step();
		net.send = true;

		ShowWindowButton();
	}

	Pose windowPoseButton = new Pose(0, 0, 0.75f, Quat.Identity);
	void ShowWindowButton() {
		UI.WindowBegin("Window Button", ref windowPoseButton);

		// if (UI.Button("Draw Oriel Axis")) { oriel.drawAxis = !oriel.drawAxis; }

		// if (UI.Button("Reset Oriel Quat")) { oriel.ori = Quat.Identity; }
		// if (UI.Button("Scale w/Height")) { oriel.scaleWithHeight = !oriel.scaleWithHeight; }
		// UI.HSlider("Scale", ref oriel.scale, 0.1f, 1f, 0.1f);
		// UI.HSlider("Multiplier", ref oriel.multiplier, 0.1f, 1f, 0.1f);
		// // UI.Label("Player.y");
		// UI.HSlider("Player.y", ref greenyard.height, 1f, 6f, 0.2f);

		// UI.Label("pos.y");
		// UI.HSlider("pos.y", ref playerY, -1f, 1f, 0.1f);

		// UI.Label("trail.length");
		// UI.HSlider("trail.length", ref trailLen, 0.1f, 1f, 0.1f);

		// UI.Label("trail.scale");
		// UI.HSlider("trail.str", ref trailScl, 0.1f, 2f, 0.1f);

		// UI.Label("str");
		// UI.HSlider("str", ref stretchStr, 0.1f, 1f, 0.1f);



		// flipIndex
		// flipGrip



		UI.WindowEnd();
	}
	public float trailLen = 0.666f;
	public float trailScl = 0.2f;
	public float stretchStr = 0.5f;
	public float playerY = 0;

}


/* 
	COMMENTS
	
	debug bool
		rendering the raw output
		particularly for hand tracking dofs (so Moses can better test them!)
		raw = 0.333f alpha ~

*/