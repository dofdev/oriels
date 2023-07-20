namespace Oriels;

public class Mat {
  public Material dev;
	public Material holoframe = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material holoframeUnder   = new Material(Shader.FromFile("shaders/below.hlsl"));
	public Material holoclear = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material holoclearUnder   = new Material(Shader.FromFile("shaders/below.hlsl"));
	public Material holo      = new Material(Shader.FromFile("shaders/above.hlsl"));
	Material holoUnder        = new Material(Shader.FromFile("shaders/below.hlsl"));

  public void Init() {
    dev = Material.Default.Copy();
		dev.SetTexture("diffuse", Tex.DevTex);

		holo.SetColor("clearcolor", Renderer.ClearColor);
		holoUnder.SetColor("clearcolor", Renderer.ClearColor);
		holoUnder.FaceCull = Cull.None;
		holo.Chain = holoUnder;

		holoclear.SetColor("clearcolor", Color.Black);
		holoclear.Transparency = Transparency.Add;
		holoclear.DepthWrite = false;
		holoclear.FaceCull = Cull.None;
		holoclearUnder.SetColor("clearcolor", Color.Black);
		holoclearUnder.Transparency = Transparency.Add;
		holoclearUnder.DepthWrite = false;
		holoclearUnder.FaceCull = Cull.None;
		holoclear.Chain = holoclearUnder;

		holoframe.SetColor("clearcolor", Color.Black);
		holoframe.Transparency = Transparency.Add;
		holoframe.DepthWrite = false;
		holoframe.FaceCull = Cull.None;
		holoframe.Wireframe = true;
		holoframeUnder.SetColor("clearcolor", Color.Black);
		holoframeUnder.Transparency = Transparency.Add;
		holoframeUnder.DepthWrite = false;
		holoframeUnder.FaceCull = Cull.None;
		holoframeUnder.Wireframe = true;
		holoframe.Chain = holoframeUnder;
  }
}