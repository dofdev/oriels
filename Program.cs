using StereoKit;

class Program {
	static void Main(string[] args) {
		// System.Console.WriteLine("Test");
		SKSettings settings = new SKSettings();
		settings.appName = "oriels";
		settings.assetsFolder = "Assets";
		SK.Initialize(settings);

		Model cursor = Model.FromFile("cursor.glb");

		ColorCube cube = new ColorCube();
		OrbitalView.strength = 4;
		OrbitalView.distance = 0.4f;
		cube.thickness = 0.01f;

    ReachCursor reachCursor = new ReachCursor();

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
