namespace Oriels;

public class Compositor {

	// these are in a weird place, 
	// as a process would not be hosted by a compositor...
	Backrooms.Mono backrooms = new Backrooms.Mono();
	Greenyard.Mono greenyard = new Greenyard.Mono();
	// bool other = false;

	Tex tex;
	Material mat = new Material(Shader.FromFile("compositor.hlsl"));

	public void Init() {
		tex = new Tex(TexType.Rendertarget);
		tex.SetSize(512, 512);
		tex.AddZBuffer(TexFormat.Depth16); // DepthStencil
		mat[MatParamName.DiffuseTex] = tex;
		mat.FaceCull = Cull.Front;

		// Renderer.Blit(tex, newMat)


		backrooms.Init();
		greenyard.Init();
	}

	public void Frame() {
		Mono mono = Mono.inst;

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

		Renderer.RenderTo(tex, 
			Matrix.TR(V.XYZ(0, 1, 0), Quat.FromAngles(0, 180, 0)),
			Matrix.Perspective(60, 1, 0.1f, 100), 
			RenderLayer.All // & ~RenderLayer.Layer1
		);

		Default.MeshQuad.Draw(mat, 
			Matrix.TR(V.XYZ(0, 1, 0), Quat.FromAngles(0, 0, 0))
		);
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