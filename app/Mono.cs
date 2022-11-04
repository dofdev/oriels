namespace Oriels;

public class Mono {
	private static readonly Lazy<Oriels.Mono> lazy = new Lazy<Oriels.Mono>(() => new Oriels.Mono());
	public static Oriels.Mono inst { get { return lazy.Value; } }

	public PullRequest.Noise noise = new PullRequest.Noise(939949595);

	public Material matDev;
	public Material matHoloframe = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material matHoloframeUnder = new Material(Shader.FromFile("shaders/below.hlsl"));
	public Material matHolo = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material matHoloUnder = new Material(Shader.FromFile("shaders/below.hlsl"));

	public Rig rig = new Rig();
	public Space space = new Space();
	public Compositor compositor = new Compositor();

	// -------------------------------------------------

	public dof[] dofs;

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
			new Chiral() {
				dofs = new dof[] {
					new WaveCursor()  { handed = Handed.Left  },
					new WaveCursor()  { handed = Handed.Right }
				}
			},
			new Chiral() {
				dofs = new dof[] {
					new Trackballer() { handed = Handed.Left  },
					new Trackballer() { handed = Handed.Right }
				}
			},
		};
	}

	public void Init() {
		compositor.Init();

		for (int i = 0; i < dofs.Length; i++) {
			dofs[i].Init();
		}

		matDev = Material.Default.Copy();
		matDev.SetTexture("diffuse", Tex.DevTex);

		matHolo.SetColor("clearcolor", Renderer.ClearColor);
		matHoloUnder.SetColor("clearcolor", Renderer.ClearColor);
		matHoloUnder.FaceCull = Cull.None;
		matHolo.Chain = matHoloUnder;


		matHoloframe.SetColor("clearcolor", Color.Black);
		matHoloframe.Transparency = Transparency.Add;
		matHoloframe.DepthWrite = false;
		matHoloframe.FaceCull = Cull.None;
		matHoloframe.Wireframe = true;
		matHoloframeUnder.SetColor("clearcolor", Color.Black);
		matHoloframeUnder.Transparency = Transparency.Add;
		matHoloframeUnder.DepthWrite = false;
		matHoloframeUnder.FaceCull = Cull.None;
		matHoloframeUnder.Wireframe = true;
		matHoloframe.Chain = matHoloframeUnder;
	}

	public void Frame() {

		// Input.HandClearOverride(Handed.Left);
		// Input.HandClearOverride(Handed.Right);
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

		// Input.Subscribe(InputSource.Hand, BtnState.Any, Action<Hand, BtnState.Any, Pointer>);

		// -------------------------------------------------

		for (int i = 0; i < dofs.Length; i++) {
			if (dofs[i].Active) {
				dofs[i].Frame();
			}
		}  

		// <Heresy>
		WaveCursor lwc = (WaveCursor)((Chiral)dofs[0]).dofs[0];
		WaveCursor rwc = (WaveCursor)((Chiral)dofs[0]).dofs[1];
		Trackballer ltb = (Trackballer)((Chiral)dofs[1]).dofs[0];
		Trackballer rtb = (Trackballer)((Chiral)dofs[1]).dofs[1];

		if (lwc.Active) {
			lwc.Demo(ltb.ori);
		}
		if (rwc.Active) {
			rwc.Demo(rtb.ori);
		}

		if (rtb.Active) {
			rtb.Demo();
		}

		// </Heresy>

		// rBlock.Step(); lBlock.Step();

		// cubicCon.Step();

		// colorCube.Palm(lCon.device);

		// -------------------------------------------------

		net.me.Step();
		net.send = true;

		ShowWindowButton();
	}

	int dofIndex = 0;
	Pose windowPoseButton = new Pose(-0.5f, 1.3f, 0, Quat.FromAngles(0, -90f, 0));
	TextStyle style = Text.MakeStyle(Font.FromFile("add/fonts/DM-Mono.ttf"), 1f * U.cm, Color.White);
	TextStyle style2 = Text.MakeStyle(Font.FromFile("add/fonts/DM-Mono.ttf"), 1f * U.cm, new Color(0.5f, 0.5f, 0.5f));
	Vec2 fieldSize = new Vec2(6f * U.cm, 3f * U.cm);
	void ShowWindowButton() {
		UI.WindowBegin("design vars", ref windowPoseButton);
		UI.SetThemeColor(UIColor.Background, new Color(0f, 0f, 0f));
		UI.SetThemeColor(UIColor.Primary, new Color(0.5f, 0.5f, 0.5f));
		UI.PushTextStyle(style);

		// if (UI.Button("Draw Oriel Axis")) { oriel.drawAxis = !oriel.drawAxis; }
		// if (UI.Button("Reset Oriel Quat")) { oriel.ori = Quat.Identity; }
		// if (UI.Button("Scale w/Height")) { oriel.scaleWithHeight = !oriel.scaleWithHeight; }
		// UI.HSlider("Scale", ref oriel.scale, 0.1f, 1f, 0.1f);
		// UI.HSlider("Multiplier", ref oriel.multiplier, 0.1f, 1f, 0.1f);
		// // UI.Label("Player.y");
		// UI.HSlider("Player.y", ref greenyard.height, 1f, 6f, 0.2f);
		// UI.Label("pos.y");
		// UI.HSlider("pos.y", ref playerY, -1f, 1f, 0.1f);

		if (UI.Button("prev") && dofIndex > 0) {
			dofIndex--;
		}
		UI.SameLine();
		if (UI.Button("next") && dofIndex < dofs.Length - 1) {
			dofIndex++;
		}
		

		dof dof = dofs[dofIndex];
		Type type = dof.GetType();
		// active toggle
		Color tint = dof.Active ? new Color(0, 1, 0) : new Color(1, 0, 0);
		UI.PushTint(tint);
		if (UI.Button(dof.Active ? "on" : "off")) {
			dof.Active = !dof.Active;
		}
		UI.PopTint();
		if (type == typeof(Chiral)) {
			Chiral chiral = (Chiral)dof;

			System.Reflection.FieldInfo[] fields = typeof(Chiral).GetFields();
			foreach (System.Reflection.FieldInfo field in fields) {
				if (field.FieldType == typeof(Handed)) {
					Handed handed = (Handed)field.GetValue(chiral);
					if (UI.Button("<") && (int)handed > 0) {
						handed = (Handed)((int)handed - 1);
						field.SetValue(chiral, handed);
					}
					UI.SameLine();
					if (UI.Button(">") && (int)handed < 2) {
						handed = (Handed)((int)handed + 1);
						field.SetValue(chiral, handed);
					}
					UI.SameLine(); UI.Label(handed.ToString());
				}
			}

			RenderDof(chiral.dofs[0]);
		} else {
			RenderDof(dof);
		}

		UI.WindowEnd();
	}

	void RenderDof(dof dof) {
		Type type = dof.GetType();
		UI.Label("°" + type.Name);
		System.Reflection.FieldInfo[] fields = type.GetFields();
		for (int j = 0; j < fields.Length; j++) {
			System.Reflection.FieldInfo field = fields[j];
			if (field.FieldType == typeof(Design)) {
				Design design = (Design)field.GetValue(dof);
				UI.Input(field.Name, ref design.str, fieldSize, TextContext.Number);

				UI.SameLine(); 
				UI.PushTextStyle(style2); 
				UI.Label(design.term, new Vec2(4f * U.cm, 3f * U.cm));
				UI.PopTextStyle();

				UI.SameLine(); UI.Label(field.Name);
			}
		}
	}
}

// Chiral : handedness implies symmetry
public class Chiral : dof {
	private bool active;
	public bool Active {
		get { return this.active; }
		set { 
			this.active = value;
			for (int i = 0; i < this.dofs.Length; i++) {
				dof dof = this.dofs[i];
				if ((int)this.handed == 2 || i == (int)this.handed) {
					dof.Active = value;
				} else {
					dof.Active = false;
				}
			}
		} 
	}
	public dof[] dofs = new dof[2];
	// public Design handed = new Design { str = "2", min = 0, max = 2};
	public Handed handed = Handed.Max;

	public void Init() {
		dofs[0].Init();
		dofs[1].Init();
	}

	public void Frame() {
		// sync the left design variables to the right
		System.Reflection.FieldInfo[] fields = dofs[0].GetType().GetFields();
		foreach (System.Reflection.FieldInfo field in fields) {
			if (field.FieldType == typeof(Design)) {
				Design design = (Design)field.GetValue(dofs[0]); // define type?
				field.SetValue(dofs[1], design);
			}
		}

		for (int i = 0; i < dofs.Length; i++) {
			dof dof = dofs[i];
			if ((int)handed == 2 || i == (int)handed) {
				dof.Frame();
				dof.Active = true;
			}
			else {
				dof.Active = false;
			}
		}
	}
}

public class Design {
	public string str;
	public string term;
	public float min = float.NegativeInfinity;
	public float max = float.PositiveInfinity;
	public float unit = U.m;

	public float value {
		get {
			try {
				float value = PullRequest.Clamp(float.Parse(str), min, max);
				// if clamped, update string
				if (value != float.Parse(str)) {
					if (Input.Key(Key.Return).IsJustActive()) {
						str = value.ToString();
					}
				}
				return value * unit;
			} catch {
				return MathF.Max(0, min) * unit;
			}
		}
	}
	// public int integer {};
}


/* 
	COMMENTS

	ranges 
		0+1
		1-0
		1-0+1

		-0+

		0+&&-
		0+||-
	
	units 
		m
		cm
		mm
		t

	demo
		virtual shapes -> that can be slotted
		physics boxes

	mirror
		mirroring vectors(line segments) is really easy
		easier than rendering.. actually just render twice with the material chain
		stereonick mentioned
	
	debug bool
		rendering the raw output
		particularly for hand tracking dofs (so Moses can better test them!)
		raw = 0.333f alpha ~

*/