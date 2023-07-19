namespace Oriels;

public class Mono {
	private static readonly Lazy<Oriels.Mono> lazy = new Lazy<Oriels.Mono>(() => new Oriels.Mono());
	public static Oriels.Mono inst { get { return lazy.Value; } }

	public PR.Noise noise = new PR.Noise(939949595);

	public Material matDev;
	public Material matHoloframe = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material matHoloframeUnder   = new Material(Shader.FromFile("shaders/below.hlsl"));
	public Material matHoloclear = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material matHoloclearUnder   = new Material(Shader.FromFile("shaders/below.hlsl"));
	public Material matHolo      = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material matHoloUnder        = new Material(Shader.FromFile("shaders/below.hlsl"));

	public Rig rig = new Rig();
	public Space space = new Space();
	public Compositor compositor = new Compositor();

	// -------------------------------------------------

	public Interaction[] interactions;

	public ColorCube colorCube = new ColorCube();

	public Glove rGlove = new Glove(true), lGlove = new Glove(false);
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

		matDev = Material.Default.Copy();
		matDev.SetTexture("diffuse", Tex.DevTex);

		matHolo.SetColor("clearcolor", Renderer.ClearColor);
		matHoloUnder.SetColor("clearcolor", Renderer.ClearColor);
		matHoloUnder.FaceCull = Cull.None;
		matHolo.Chain = matHoloUnder;

		matHoloclear.SetColor("clearcolor", Color.Black);
		matHoloclear.Transparency = Transparency.Add;
		matHoloclear.DepthWrite = false;
		matHoloclear.FaceCull = Cull.None;
		matHoloclearUnder.SetColor("clearcolor", Color.Black);
		matHoloclearUnder.Transparency = Transparency.Add;
		matHoloclearUnder.DepthWrite = false;
		matHoloclearUnder.FaceCull = Cull.None;
		matHoloclear.Chain = matHoloclearUnder;

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

	Pose shape = new Pose(new Vec3(0, 1f, -3f), Quat.FromAngles(45, 0, 45));
	bool shapeHeld = false;




	Spatial spatial = new Spatial(new Vec3(-1, 0.76f, 0.666f));
	Cursor cursor = new Cursor();
	Drawer drawerA = new Drawer(new Pose(new Vec3(-0.8f, 0.6f, 1.4f), Quat.FromAngles(0, 90f, 0)));
	Drawer drawerB = new Drawer(new Pose(new Vec3(-0.8f, 0.6f, 0.95f), Quat.FromAngles(0, 90f, 0)));
	Drawer drawerC = new Drawer(new Pose(new Vec3(-0.8f, 0.6f, 0.5f), Quat.FromAngles(0, 90f, 0)));

	public void Frame() {

		// Input.HandClearOverride(Handed.Left);
		// Input.HandClearOverride(Handed.Right);
		// store hand pre override in rig
		rig.Step();
		// Hand h = Input.Hand(Handed.Right);
		// if (h.pinch.IsActive()) {
		// 	Console.WriteLine($"{h.pinchPt}, {Input.Head.orientation * -Vec3.Forward}");
		// }

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
		
		spatial.Frame();

		// pinch-cursor?
		// {
		// 	float deadzone = 0.01f;
		// 	float strength = 6f;

		// 	Hand hand = Input.Hand(Handed.Right);
		// 	Vec3 indexTip = hand.Get(FingerId.Index, JointId.Tip).position;
		// 	Vec3 thumbTip = hand.Get(FingerId.Thumb, JointId.Tip).position;

		// 	Vec3 delta    = indexTip - thumbTip;
		// 	float mag     = delta.Magnitude;
		// 	float pinch   = MathF.Max(mag - deadzone, 0);

		// 	Vec3 dir = delta.Normalized;

		// 	cursor.raw = indexTip + dir * pinch * strength;

		// 	Lines.Add(indexTip, thumbTip, new Color(0, 0, 1), 0.002f);
		// 	Mesh.Sphere.Draw(matHolo, Matrix.TS(cursor.pos, 0.01f), new Color(0.5f, 0.5f, 0.5f));
		// 	// V.XYZ(0, 0, );

		// 	drawerA.Frame(cursor, pinch);
		// 	drawerB.Frame(cursor, pinch);
		// 	drawerC.Frame(cursor, pinch);
		// }



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
			Mono.inst.matHoloframe,
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

		ShowWindowButton();
	}

	int dofIndex = 0;
	Pose windowPose = new Pose(-0.333f, 1.2f, -0.5f, Quat.FromAngles(0, 180, 0));
	Material windowMat = new Material(Shader.FromFile("shaders/window.hlsl"));
	TextStyle style  = Text.MakeStyle(Font.FromFile("add/fonts/DM-Mono.ttf"), 1f * U.cm, Color.Black);
	TextStyle style2 = Text.MakeStyle(Font.FromFile("add/fonts/DM-Mono.ttf"), 1f * U.cm, new Color(0.5f, 0.5f, 0.5f));
	Vec2 fieldSize = new Vec2(6f * U.cm, 3f * U.cm);
	void ShowWindowButton() {
		windowMat.Transparency = Transparency.Add;
		windowMat.FaceCull = Cull.None;
		windowMat.DepthWrite = false;
		UI.SetElementVisual(UIVisual.WindowBody, Mesh.Quad, windowMat, Vec2.One);
		UI.SetElementVisual(UIVisual.WindowHead, Mesh.Quad, windowMat, Vec2.One);
		UI.WindowBegin("design vars", ref windowPose);
		// UI.SetThemeColor(UIColor.Primary, new Color(1f, 1f, 1f));
		// UI.HandleBegin("design", ref windowPose, new Bounds(V.XYZ(0.02f, 0.02f, 0.02f)), true, UIMove.FaceUser);
		UI.SetThemeColor(UIColor.Background, new Color(0.2f, 0.2f, 0.3f));
		UI.SetThemeColor(UIColor.Primary,    new Color(0.4f, 0.4f, 0.6f));
		UI.SetThemeColor(UIColor.Common,     new Color(1.0f, 1.0f, 1.0f));
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
		if (UI.Button("next") && dofIndex < interactions.Length - 1) {
			dofIndex++;
		}
		

		Interaction dof = interactions[dofIndex];
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
		// UI.HandleEnd();
	}

	void RenderDof(Interaction dof) {
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
public class Chiral : Interaction {
	public Chiral(Interaction[] dofs) => this.dofs = dofs;
	private bool active;
	public bool Active {
		get { return this.active; }
		set { 
			this.active = value;
			for (int i = 0; i < this.dofs.Length; i++) {
				Interaction dof = this.dofs[i];
				if ((int)this.handed == 2 || i == (int)this.handed) {
					dof.Active = value;
				} else {
					dof.Active = false;
				}
			}
		} 
	}
	public Interaction[] dofs = new Interaction[2];
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
			Interaction dof = dofs[i];
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
				float value = PR.Clamp(float.Parse(str), min, max);
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

public class Cursor {
	PR.OneEuroFilter xF = new PR.OneEuroFilter(0.001f, 0.1f);
	PR.OneEuroFilter yF = new PR.OneEuroFilter(0.001f, 0.1f);
	PR.OneEuroFilter zF = new PR.OneEuroFilter(0.001f, 0.1f);
	Vec3 _raw;
	public Vec3 raw {
		get => _raw;
		set {
			_raw = value;
			pos = new Vec3(
				(float)xF.Filter(raw.x, (double)Time.Stepf),
				(float)yF.Filter(raw.y, (double)Time.Stepf),
				(float)zF.Filter(raw.z, (double)Time.Stepf)
			);
			smooth = Vec3.Lerp(smooth, pos, Time.Stepf * 6f);
		}
	}
	public Vec3 pos { get; private set; }
	public Vec3 smooth { get; private set; }
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
		seperate the demos from the dofs, and make them rebindable (assigning input using reflection?)
		virtual shapes(scalable) -> that can be slotted
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


public class Spatial {
	// example, to build out from!
	// rn it's just adding two vectors
	// building towards great interactivity and visual feedback
	public Spatial(Vec3 origin) {
		this.origin = origin;
	}
	public Vec3 origin;
	float scale = 0.2f;
	float thickness => 0.02f * scale;


	float t = 1.0f;
	Vec3 aFrom, aTo;
	Vec3 a => Vec3.Lerp(aFrom, aTo, MathF.Min(t, 1f));
	Vec3 bFrom, bTo;
	Vec3 b => Vec3.Lerp(bFrom, bTo, MathF.Min(t, 1f));
	Vec3 c => a + b;

	public void Frame() {
		// origin axis
		Lines.Add(origin, World(new Vec3(1, 0, 0)), new Color(1, 0, 0), thickness * 0.333f);
		Lines.Add(origin, World(new Vec3(0, 1, 0)), new Color(0, 1, 0), thickness * 0.333f);
		Lines.Add(origin, World(new Vec3(0, 0, 1)), new Color(0, 0, 1), thickness * 0.333f);
		Mesh.Sphere.Draw(Material.Unlit, Matrix.TS(origin, thickness), new Color(0.5f, 0.5f, 0.5f));

		Random rand = Random.Shared;
		if (t >= 1.3f) {
			aFrom = aTo;
			bFrom = bTo;

			if (rand.NextSingle() < 0.5f) {
				aTo = new Vec3(rand.NextSingle(), rand.NextSingle(), rand.NextSingle()) * 0.5f;
			} else {
				bTo = new Vec3(rand.NextSingle(), rand.NextSingle(), rand.NextSingle()) * 0.5f;
			}

			t = 0.0f;
		}
		t += Time.Stepf / 2f;

		// Lines.Add(origin, World(a), new Color(1, 1, 1, 0.5f), thickness * 2); // they clip with no material way to fix it?
		Lines.Add(origin, World(a), new Color(1, 1, 0), thickness);
		Mesh.Sphere.Draw(Material.Unlit, Matrix.TS(World(a), thickness), new Color(1, 1, 0));
		// Lines.Add(origin, World(b), new Color(1, 1, 1, 0.5f), thickness * 2);
		Lines.Add(origin, World(b), new Color(0, 1, 1), thickness);
		Mesh.Sphere.Draw(Material.Unlit, Matrix.TS(World(b), thickness), new Color(0, 1, 1));

		Lines.Add(World(a), World(c), new Color(0, 1, 1), thickness);
		Lines.Add(World(b), World(c), new Color(1, 1, 0), thickness);
		// color between yellow and cyan using HSV
		Mesh.Sphere.Draw(Material.Unlit, Matrix.TS(World(c), thickness), new Color(0.5f, 1, 1));
	}

	Vec3 World(Vec3 local) {
		return origin + local * scale;
	}

	// 
}

// Volumetric Lines
public class Vine {

}