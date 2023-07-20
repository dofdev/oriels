namespace Oriels;

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

/* 
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