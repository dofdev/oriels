namespace Oriels;

public class Window {
  int dofIndex = 0;
	Pose windowPose = new Pose(-0.333f, 1.2f, -0.5f, Quat.FromAngles(0, 180, 0));
	Material windowMat = new Material(Shader.FromFile("shaders/window.hlsl"));
	TextStyle style  = Text.MakeStyle(Font.FromFile("add/fonts/DM-Mono.ttf"), 1f * U.cm, Color.Black);
	TextStyle style2 = Text.MakeStyle(Font.FromFile("add/fonts/DM-Mono.ttf"), 1f * U.cm, new Color(0.5f, 0.5f, 0.5f));
	Vec2 fieldSize = new Vec2(6f * U.cm, 3f * U.cm);
	public void Frame() {
    Mono mono = Mono.inst;
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
		if (UI.Button("next") && dofIndex < mono.interactions.Length - 1) {
			dofIndex++;
		}
		

		Interaction dof = mono.interactions[dofIndex];
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