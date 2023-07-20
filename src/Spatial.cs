namespace Oriels;

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