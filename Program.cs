using System;
using StereoKit;

class Program {
	static void Main(string[] args) {
		SKSettings settings = new SKSettings
    {
      appName = "oriels",
      assetsFolder = "Assets",
    };
		if (!SK.Initialize(settings))
      Environment.Exit(1);

    Model cursor = Model.FromFile("cursor.glb");

		ColorCube cube = new ColorCube();
		OrbitalView.strength = 4;
		OrbitalView.distance = 0.4f;
		cube.thickness = 0.01f;

    ReachCursor reachCursor = new ReachCursor();

    Material addMat = new Material(Shader.FromFile("example.hlsl"));

		while(SK.Step(() => {
			// Matrix orbitMatrix = OrbitalView.transform;
			// cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
			// Default.MaterialHand["color"] = cube.color;

      reachCursor.Step();

			// cursor.Draw(Matrix.S(0.1f));
		}));
		SK.Shutdown();
	}
}
