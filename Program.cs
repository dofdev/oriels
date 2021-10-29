using StereoKit;

class Program {
	static void Main(string[] args) {
		// System.Console.WriteLine("Test");
		SKSettings settings = new SKSettings();
		settings.appName = "oriels";
		settings.assetsFolder = "Assets";
		// settings.displayPreference = DisplayMode.Flatscreen;
		SK.Initialize(settings);
		// Renderer.EnableSky = false;

		ColorCube cube = new ColorCube();
		OrbitalView.strength = 4;
		OrbitalView.distance = 0.4f;
		cube.thickness = 0.01f;

		while(SK.Step(() => {
			cube.Step(Matrix.S(Vec3.One * 0.2f) * OrbitalView.transform);
			Default.MaterialHand["color"] = cube.color;
		}));
		SK.Shutdown();
	}
}
