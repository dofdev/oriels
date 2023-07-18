namespace Oriels;

public class Compositor {

	// these are in a weird place, 
	// as a process would not be hosted by a compositor...
	Backrooms.Mono backrooms = new Backrooms.Mono();
	Greenyard.Mono greenyard = new Greenyard.Mono();
	// bool other = false;

	Tex tex, depth;
	Material mat = new Material(Shader.FromFile("shaders/compositor.hlsl"));

	public void Init() {
		tex = new Tex(TexType.Rendertarget);
		tex.SetSize(512, 512);
		depth = tex.AddZBuffer(TexFormat.Depth32); // DepthStencil
		mat[MatParamName.DiffuseTex] = depth;
		mat.FaceCull = Cull.None;

		// Renderer.Blit(tex, newMat)


		backrooms.Init();
		greenyard.Init();
	}

	public void Frame() {
		Mono mono = Mono.inst;

		if (Input.Key(Key.Space).IsJustActive()) {
			// add the depth tex color.r's up and see if they are > 0
			float r = 0;
			Color32[] cols = depth.GetColors();
			for (int i = 0; i < cols.Length; i++) {
				r += cols[i].r;
			}
			Console.WriteLine($"r: {r}");
		}

		Default.MeshQuad.Draw(mat,
			Matrix.TRS(V.XYZ(-0.90f, 1.16f, 1.44f), Quat.LookDir(0.63f, 0.78f, 0.02f), 0.5f)
		);

		Renderer.RenderTo(tex,
			Matrix.TR(V.XYZ(-0.90f, 1.16f, 1.44f), Quat.LookDir(0.63f, 0.78f, 0.02f)),
			Matrix.Perspective(60, 1, 0.1f, 100),
			RenderLayer.All, // & ~RenderLayer.Layer1
			RenderClear.All,
			default(Rect)
		);


    // backrooms.oriel.Frame();
    // greenyard.oriel.Frame();

    // // mono.space.Frame();
    // Glove rGlove = Mono.inst.rGlove;
    // Vec3 cursor = rGlove.virtualGlove.position;

		// if (other) {
		// 	greenyard.Frame();

    // 	Vec3 localCursor = backrooms.oriel.matrixInv.Transform(cursor);
		// 	if (backrooms.oriel.bounds.Contains(localCursor + backrooms.oriel.bounds.center)) {
		// 		Console.WriteLine("in backrooms");
		// 		other = false;
		// 	} 
		// } else {
		// 	backrooms.Frame();

    // 	Vec3 localCursor = greenyard.oriel.matrixInv.Transform(cursor);
		// 	if (greenyard.oriel.bounds.Contains(localCursor + greenyard.oriel.bounds.center)) {
		// 		Console.WriteLine("in greenyard");
		// 		other = true;
		// 	}
		// }

		// render buffers ? or just use the depth buffer?

		mono.space.Frame();
		// backrooms.oriel.Render(); // -> Frame() by moving Input specific parts to the compositor?
		// backrooms.Render();
		// greenyard.oriel.Render();
		// greenyard.Render();

    // active oriel
		// how to show this?
		// well popping up a wireframe would get in the way and look bad
		// and a glow would be hard to keep consistent across backgrounds
		// am I bringing the crown back?

    // matFrame.Wireframe = true;
    // matFrame.DepthTest = DepthTest.Always;
    // matFrame.SetVector("_cursor", cursor);
    // matFrame.SetFloat("_time", Time.Totalf);
    // Mesh.Cube.Draw(matFrame,
    //   Matrix.TRS(bounds.center, ori, bounds.dimensions),
    //   new Color(0.1f, 0.1f, 0.1f)
    // );
    // Model model = Model.FromFile("oriel.glb");
    // ~ Mesh mesh = model.GetMesh("oriel");
  }

	void Place() {

	}
}


/* 
	COMMENTS

	mono (kernel)
		compositor
			oriel (client)
				app

	Frame and Render is not a useful distinction
	cycle or loop or step instead of frame?
	draw instead of render?

	we keep combining the two, and it's not a good idea
	as input can be polled at a higher frequency than rendering can be output?

	Hertz & Frame

	does a process ever not have a GUI? *can be hidden &| locked
	we are trying to skip the tty!

	would be nice to have a dedicated thread(s)
	for essential GUI
		
*/