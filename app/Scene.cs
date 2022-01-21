using System;
using StereoKit;

public class Scene {
  Monolith mono;
  Material matFloor = new Material(Shader.Default);
  Solid floor;
  public Scene(Monolith mono) {
    this.mono = mono;

    floor = new Solid(Vec3.Up * -1.5f, Quat.Identity, SolidType.Immovable);
    scale = 64f;
    floorScale = new Vec3(scale, 0.1f, scale);
    floor.AddBox(floorScale);
    // box on each side
    floor.AddBox(new Vec3(scale, scale / 2, 0.1f), 1, new Vec3(0, scale / 4, -scale / 2));
    floor.AddBox(new Vec3(scale, scale / 2, 0.1f), 1, new Vec3(0, scale / 4, scale / 2));
    floor.AddBox(new Vec3(0.1f, scale / 2, scale), 1, new Vec3(-scale / 2, scale / 4, 0));
    floor.AddBox(new Vec3(0.1f, scale / 2, scale), 1, new Vec3(scale / 2, scale / 4, 0));
    // and ceiling
    floor.AddBox(new Vec3(scale, 0.1f, scale), 1, new Vec3(0, scale / 2, 0));
    matFloor.SetTexture("diffuse", Tex.FromFile("floor.png"));
    matFloor.SetFloat("tex_scale", 32);
  }

  public float scale;
  public Vec3 floorScale;


  public void Step() {
    PullRequest.BlockOut(floor.GetPose().ToMatrix(floorScale), Color.White * 0.666f, matFloor);
  }
}