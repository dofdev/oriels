namespace Oriels;

public class Compositor {

	// these are in a weird place, 
	// as a process would not be hosted by a compositor...
	Backrooms.Mono backrooms = new Backrooms.Mono();
	Greenyard.Mono greenyard = new Greenyard.Mono();
	// bool other = false;

	Tex tex_near, depth_near;
	Tex tex_far,  depth_far;
	
	Material mat_near, mat_far;
	
	Material mat = new Material(Shader.FromFile("shaders/compositor.hlsl"));

	public void Init() {
		tex_near = new Tex(TexType.Rendertarget);
		tex_near.SetSize(512, 512);
		depth_near = tex_near.AddZBuffer(TexFormat.Depth32); // DepthStencil

		tex_far = new Tex(TexType.Rendertarget);
		tex_far.SetSize(512, 512);
		depth_far  = tex_far.AddZBuffer(TexFormat.Depth32); // DepthStencil

		mat_near = Material.Unlit.Copy();
		mat_near.FaceCull = Cull.Back;
		mat_far = Material.Unlit.Copy();
		mat_far.FaceCull = Cull.Front;
	
		mat["near"] = depth_near;
		mat["far"] = depth_far;
		mat.FaceCull = Cull.None;

		// Renderer.Blit(tex, newMat)

		backrooms.Init();
		greenyard.Init();
	}

	public void Frame() {
		Mono mono = Mono.inst;
		Rig rig = mono.rig;

		if (false) {
			Pose oriel = new(
				V.XYZ(0, 1.6f, -0.4f),
				Quat.Identity
			);
			Vec3 oriel_scale = new(
				0.2f, 0.1f, 0.2f
			);

			// oriel_n
			Mesh.Cube.Draw(
				mat_near,
				oriel.ToMatrix(1),
				Color.White,
				RenderLayer.Layer1
			);
			Renderer.RenderTo(tex_near,
				Renderer.CameraRoot, // will need separate matrices+ for each eye
				Matrix.Perspective(90, 1, 1*U.cm, 100),
				RenderLayer.Layer1 // RenderLayer.All & ~RenderLayer.Layer1
			);

			// oriel_f
			Mesh.Cube.Draw(
				mat_far,
				oriel.ToMatrix(1),
				Color.White,
				RenderLayer.Layer2
			);
			Renderer.RenderTo(tex_far,
				Renderer.CameraRoot, // will need separate matrices+ for each eye
				Matrix.Perspective(90, 1, 1*U.cm, 100),
				RenderLayer.Layer2 // RenderLayer.All & ~RenderLayer.Layer2
			);

			// oriel_content debug
			Mesh.Quad.Draw(mat,
				Matrix.TRS(
					V.XYZ(0, 0, 0 * U.cm),
					Quat.FromAngles(0, 180, 0),
					1
				) * oriel.ToMatrix(1)
			);
		}




	// optimize by making it more of a composition texture rather than depth textures



																															/*
																															SK
																																Plane.Plane(Vec3 pointOnPlane1, Vec3 pointOnPlane2, Vec3 pointOnPlane3)
																																pointOnPlane3: Third point on the plane.


																																Creates a plane from 3 points that are directly on that plane.

																																use this for that hand tracking mouse cursor 
																																and then make the delta of the hand motion relative to the hand rotation
																																just like a mouse ^-^
																																
																																painfully easy, so focus on which joint points are being tracked well 
																																to use for the 'sensor position' + offset*
																															*/ 


    backrooms.oriel.Frame();
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